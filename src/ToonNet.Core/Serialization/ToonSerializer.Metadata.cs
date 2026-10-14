using System.Diagnostics.CodeAnalysis;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.Core.Serialization;

public static partial class ToonSerializer
{
    /// <summary>
    /// Cached reflection data for a serializable property.
    /// </summary>
    private sealed class PropertyMetadata
    {
        public required PropertyInfo Property { get; init; }
        public required Func<object, object?> Getter { get; init; }
        public Action<object, object?>? Setter { get; init; }
        public string? AttributeName { get; init; }
        public IToonConverter? Converter { get; init; }
        public bool HasSetter => Setter != null;

        /// <summary>Typed access for primitive properties of classes, used when no converter applies; else null.</summary>
        public PrimitiveAccessor? Primitive { get; init; }

        /// <summary>Serialized names per <see cref="PropertyNamingPolicy"/> value, computed on first use.</summary>
        public string?[] Names { get; } = new string?[NamingPolicyCount];
    }

    /// <summary>
    /// Cached reflection data for a type: its properties (in serialization order) and how to construct it.
    /// </summary>
    /// <remarks>
    /// Instances are built once and treated as immutable thereafter.
    /// </remarks>
    private sealed class TypeMetadata
    {
        public required PropertyMetadata[] Properties { get; init; }

        /// <summary>The constructor used for deserialization, or null when the type has none usable.</summary>
        public ConstructorInfo? Constructor { get; init; }

        /// <summary>The parameters of <see cref="Constructor"/>; empty for a parameterless constructor.</summary>
        public ParameterInfo[] ConstructorParameters { get; init; } = [];

        /// <summary>A converter from <see cref="ToonConverterAttribute"/> on the type, if any.</summary>
        public IToonConverter? TypeConverter { get; init; }

        /// <summary>
        ///     The <see cref="ToonSerializableAttribute.NamingPolicy"/> set on the type, which replaces the policy of the
        ///     options (as in generated code); null when it is not set.
        /// </summary>
        public PropertyNamingPolicy? FixedNamingPolicy { get; init; }

        /// <summary>The type the metadata describes.</summary>
        public required Type Type { get; init; }

        /// <summary>
        ///     Property names per naming policy for direct writing (see <see cref="GetDistinctPropertyNames"/>); an empty
        ///     array marks names that are not distinct.
        /// </summary>
        public string[]?[] DistinctNames { get; } = new string[]?[NamingPolicyCount];

        /// <summary>Key lookups for direct reading per naming policy (see <see cref="GetReadLookup"/>).</summary>
        public ReadLookup?[] ReadLookups { get; } = new ReadLookup?[NamingPolicyCount];

        /// <summary>Per naming policy: the names no other name of the type equals ignoring case (case-insensitive reading).</summary>
        public HashSet<string>?[] CaseInsensitiveNames { get; } = new HashSet<string>?[NamingPolicyCount];

        private ObjectActivator? _activator;

        /// <summary>
        ///     How to create an instance when deserializing; built on first use, as most types are only serialized.
        /// </summary>
        public ObjectActivator Activator => _activator ??= ObjectActivator.For(this);
    }

    /// <summary>
    ///     A constructor parameter and what is bound to it: the property with the same name (case-insensitive), if any,
    ///     and the value used when the document has no entry for it.
    /// </summary>
    private sealed record ConstructorArgument(ParameterInfo Parameter, PropertyMetadata? Property, object? MissingValue);

    /// <summary>
    ///     Creates instances of a type for deserialization through a compiled delegate instead of
    ///     <see cref="ConstructorInfo.Invoke(object[])"/>.
    /// </summary>
    private sealed class ObjectActivator
    {
        /// <summary>Calls the selected constructor (or creates a default struct) with the given arguments.</summary>
        public required Func<object?[], object> New { get; init; }

        /// <summary>The constructor arguments, in parameter order; empty for a parameterless constructor.</summary>
        public required ConstructorArgument[] Arguments { get; init; }

        /// <summary>Per entry of <see cref="TypeMetadata.Properties"/>, whether it is set through the constructor.</summary>
        public required bool[] BoundToConstructor { get; init; }

        [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = ToonSerializer.ReflectionJustification)]
        public static ObjectActivator For(TypeMetadata metadata)
        {
            var arguments = metadata.ConstructorParameters
                                    .Select(parameter => new ConstructorArgument(
                                                parameter,
                                                metadata.Properties.FirstOrDefault(p => string.Equals(p.Property.Name, parameter.Name,
                                                                                                      StringComparison.OrdinalIgnoreCase)),
                                                parameter.HasDefaultValue ? parameter.DefaultValue
                                                : parameter.ParameterType.IsValueType ? System.Activator.CreateInstance(parameter.ParameterType)
                                                : null))
                                    .ToArray();

            return new ObjectActivator
            {
                New = CompileFactory(metadata.Type, metadata.Constructor),
                Arguments = arguments,
                BoundToConstructor = [.. metadata.Properties.Select(p => arguments.Any(a => a.Property == p))]
            };
        }

        /// <summary>
        ///     Compiles <c>args =&gt; (object)new T((P0)args[0], ...)</c>, or <c>new T()</c> for a struct without a
        ///     selected constructor. Abstract types and classes without a constructor get a delegate that is never called.
        /// </summary>
        /// <remarks>
        ///     A null argument for a non-nullable value type parameter is passed as its default value, as
        ///     <see cref="ConstructorInfo.Invoke(object[])"/> does.
        /// </remarks>
        [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = ToonSerializer.ReflectionJustification)]
        private static Func<object?[], object> CompileFactory(Type type, ConstructorInfo? constructor)
        {
            if (type.IsAbstract || type.IsInterface || (constructor == null && !type.IsValueType))
            {
                return _ => throw new InvalidOperationException($"{type.Name} cannot be created");
            }

            if (constructor != null && constructor.GetParameters().Any(p => p.ParameterType.IsByRef))
            {
                return args => constructor.Invoke(args); // in/ref parameters cannot be passed from an expression tree
            }

            var args = Expression.Parameter(typeof(object?[]), "args");
            Expression creation = constructor == null
                ? Expression.New(type)
                : Expression.New(constructor, constructor.GetParameters().Select((p, i) => Argument(args, i, p.ParameterType)));

            return Expression.Lambda<Func<object?[], object>>(Expression.Convert(creation, typeof(object)), args).Compile();

            static Expression Argument(ParameterExpression args, int index, Type parameterType)
            {
                var item = Expression.ArrayIndex(args, Expression.Constant(index));

                return parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) == null
                    ? Expression.Condition(Expression.Equal(item, Expression.Constant(null)), Expression.Default(parameterType),
                                           Expression.Convert(item, parameterType))
                    : Expression.Convert(item, parameterType);
            }
        }
    }

    /// <summary>
    /// Thread-safe cache for type metadata.
    /// </summary>
    /// <remarks>
    /// Entries are created on demand and retained for the lifetime of the process.
    /// </remarks>
    private static readonly ConcurrentDictionary<Type, TypeMetadata> TypeMetadataCache = new();

    /// <summary>
    /// Converter instances created for <see cref="ToonConverterAttribute"/>, one per converter type.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, IToonConverter> AttributeConverterCache = new();

    /// <summary>
    /// Retrieves cached type metadata for a specified type, or generates and stores it if not already cached.
    /// </summary>
    /// <remarks>
    /// Includes public instance properties with a public getter that are not indexers and not marked
    /// with <see cref="ToonIgnoreAttribute"/>, ordered by <see cref="ToonPropertyOrderAttribute"/> (default 0), then base-class
    /// properties before derived ones, then declaration order.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = ToonSerializer.ReflectionJustification)]
    private static TypeMetadata GetTypeMetadata(Type type)
    {
        return TypeMetadataCache.GetOrAdd(type, static t =>
        {
            var properties = new List<(PropertyMetadata Metadata, int Order, int Index)>();
            var index = 0;

            foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetIndexParameters().Length > 0 || prop.GetGetMethod() == null ||
                    prop.GetCustomAttribute<ToonIgnoreAttribute>() != null)
                {
                    continue;
                }

                var metadata = new PropertyMetadata
                {
                    Property = prop,
                    Getter = CompileGetter(prop),
                    Setter = prop.CanWrite ? CompileSetter(prop) : null,
                    AttributeName = prop.GetCustomAttribute<ToonPropertyAttribute>()?.Name,
                    Converter = CreateAttributeConverter(prop.GetCustomAttribute<ToonConverterAttribute>()),
                    Primitive = PrimitiveAccessor.Create(prop)
                };

                var order = prop.GetCustomAttribute<ToonPropertyOrderAttribute>()?.Order ?? 0;
                properties.Add((metadata, order, InheritanceDepth(prop.DeclaringType!) * 100_000 + index++));
            }

            var (constructor, parameters) = SelectConstructor(t);

            return new TypeMetadata
            {
                Type = t,
                Properties = [.. properties.OrderBy(p => p.Order).ThenBy(p => p.Index).Select(p => p.Metadata)],
                Constructor = constructor,
                ConstructorParameters = parameters,
                TypeConverter = CreateAttributeConverter(t.GetCustomAttribute<ToonConverterAttribute>()),
                FixedNamingPolicy = GetFixedNamingPolicy(t)
            };
        });
    }

    /// <summary>
    /// The naming policy set explicitly with <c>[ToonSerializable(NamingPolicy = ...)]</c> on the type or, for a type
    /// without the attribute, on its nearest base type that has it. Like the source generator, an attribute without
    /// the named argument leaves the policy of the options in effect.
    /// </summary>
    private static PropertyNamingPolicy? GetFixedNamingPolicy(Type type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            var attribute = current.GetCustomAttributesData().FirstOrDefault(a => a.AttributeType == typeof(ToonSerializableAttribute));

            if (attribute == null)
            {
                continue;
            }

            foreach (var argument in attribute.NamedArguments)
            {
                if (argument.MemberName == nameof(ToonSerializableAttribute.NamingPolicy))
                {
                    return (PropertyNamingPolicy)(int)argument.TypedValue.Value!;
                }
            }

            return null;
        }

        return null;
    }

    /// <summary>
    /// The number of base types of <paramref name="type"/>, so base-class properties are written before derived ones.
    /// </summary>
    private static int InheritanceDepth(Type type)
    {
        var depth = 0;

        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            depth++;
        }

        return depth;
    }

    /// <summary>
    /// Selects the constructor used for deserialization: one marked with <see cref="ToonConstructorAttribute"/>,
    /// otherwise the public parameterless constructor, otherwise the public constructor with the most parameters
    /// (e.g. the primary constructor of a positional record).
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = ToonSerializer.ReflectionJustification)]
    private static (ConstructorInfo? Constructor, ParameterInfo[] Parameters) SelectConstructor(Type type)
    {
        if (type.IsAbstract || type.IsInterface)
        {
            return (null, []);
        }

        // [ToonConstructor] may mark a constructor of any accessibility; otherwise only public constructors are used
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                              .FirstOrDefault(c => c.GetCustomAttribute<ToonConstructorAttribute>() != null)
                          ?? constructors.FirstOrDefault(c => c.GetParameters().Length == 0)
                          ?? constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

        return (constructor, constructor?.GetParameters() ?? []);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = ToonSerializer.ReflectionJustification)]
    private static IToonConverter? CreateAttributeConverter(ToonConverterAttribute? attribute)
    {
        if (attribute == null)
        {
            return null;
        }

        return AttributeConverterCache.GetOrAdd(attribute.ConverterType, static converterType =>
        {
            if (!typeof(IToonConverter).IsAssignableFrom(converterType))
            {
                throw new InvalidOperationException($"{converterType.FullName} used in [ToonConverter] does not implement {nameof(IToonConverter)}");
            }

            return (IToonConverter)Activator.CreateInstance(converterType)!;
        });
    }

    /// <summary>
    ///     Compiles an expression tree for a fast property getter.
    /// </summary>
    private static Func<object, object?> CompileGetter(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var castInstance = Expression.Convert(instance, property.DeclaringType!);
        var propertyAccess = Expression.Property(castInstance, property);
        var castResult = Expression.Convert(propertyAccess, typeof(object));

        return Expression.Lambda<Func<object, object?>>(castResult, instance).Compile();
    }

    /// <summary>
    ///     Compiles an expression tree for a fast property setter.
    /// </summary>
    /// <remarks>
    ///     For structs the setter writes into the boxed instance (via unbox), so the assignment is not lost on a copy.
    /// </remarks>
    private static Action<object, object?> CompileSetter(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Parameter(typeof(object), "value");
        var declaringType = property.DeclaringType!;

        Expression target = declaringType.IsValueType
            ? Expression.Unbox(instance, declaringType)
            : Expression.Convert(instance, declaringType);

        var assign = Expression.Assign(Expression.Property(target, property), Expression.Convert(value, property.PropertyType));

        return Expression.Lambda<Action<object, object?>>(assign, instance, value).Compile();
    }

    /// <summary>
    ///     Gets the serialized name for a property: <see cref="ToonPropertyAttribute"/> first, then the naming policy of
    ///     the type's <see cref="ToonSerializableAttribute"/>, then the one of the options.
    /// </summary>
    private static string GetPropertyName(PropertyMetadata property, TypeMetadata type, ToonSerializerOptions options)
    {
        if (property.AttributeName != null)
        {
            return property.AttributeName;
        }

        var policy = type.FixedNamingPolicy ?? options.PropertyNamingPolicy;

        // Racing threads compute the same name, so a plain array is enough; undefined policy values are not cached
        return (uint)policy < NamingPolicyCount
            ? property.Names[(int)policy] ??= ApplyNamingPolicy(policy, property.Property.Name)
            : ApplyNamingPolicy(policy, property.Property.Name);

        static string ApplyNamingPolicy(PropertyNamingPolicy policy, string name) => policy switch
        {
            PropertyNamingPolicy.CamelCase => Shared.ToonNaming.ToCamelCase(name),
            PropertyNamingPolicy.SnakeCase => Shared.ToonNaming.ToSnakeCase(name),
            PropertyNamingPolicy.LowerCase => name.ToLowerInvariant(),
            _                              => name
        };
    }

    /// <summary>The number of <see cref="PropertyNamingPolicy"/> values.</summary>
    private const int NamingPolicyCount = (int)PropertyNamingPolicy.LowerCase + 1;

    /// <summary>
    /// Tracks the property path for error messages and the objects being serialized for cycle detection.
    /// </summary>
    /// <remarks>
    /// The path is kept as segments (a name, or an array index) and only turned into text for an error, so walking
    /// a collection does not format a string per item. The lists are created on first use.
    /// </remarks>
    private sealed class SerializerState
    {
        /// <summary>A state for reads that never push a segment or track objects (primitive reads from generated code).</summary>
        public static readonly SerializerState Root = new();

        private List<(string? Name, int Index)>? _path;
        private HashSet<object>? _visiting;

        public HashSet<object> Visiting => _visiting ??= new HashSet<object>(ReferenceEqualityComparer.Instance);

        /// <summary>
        ///     Set when generated code hands a value to this serializer; the first value is then not given back to
        ///     generated code, which would hand it over again.
        /// </summary>
        public bool FromGeneratedCode { get; set; }

        public void Push(string name) => (_path ??= []).Add((name, 0));

        public void Push(int index) => (_path ??= []).Add((null, index));

        public void Pop() => _path!.RemoveAt(_path.Count - 1);

        public string Path
        {
            get
            {
                var sb = new StringBuilder("$");

                foreach (var (name, index) in _path ?? [])
                {
                    if (name == null)
                    {
                        sb.Append('[').Append(index.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(']');
                    }
                    else
                    {
                        if (!name.StartsWith('['))
                        {
                            sb.Append('.');
                        }

                        sb.Append(name);
                    }
                }

                return sb.ToString();
            }
        }
    }
}
