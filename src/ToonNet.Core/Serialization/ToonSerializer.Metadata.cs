using System.Runtime.CompilerServices;
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

        /// <summary>Whether the property is declared with the C# <c>required</c> modifier.</summary>
        public bool IsRequired { get; init; }

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
        ///     The indexes of the <c>required</c> properties; empty when the constructor used for reading sets them
        ///     (<see cref="System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute"/>).
        /// </summary>
        public int[] RequiredProperties { get; init; } = [];

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
    ///     and the value used when the document has no entry for it. A parameter whose member the type keeps out of
    ///     documents (<see cref="ToonIgnoreAttribute"/>, a non-public property or a field) is not
    ///     <see cref="FromDocument"/>: it always gets <see cref="MissingValue"/>, and its key is not mapped.
    /// </summary>
    private sealed record ConstructorArgument(ParameterInfo Parameter, PropertyMetadata? Property, object? MissingValue, bool FromDocument = true);

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

        /// <exception cref="InvalidOperationException">
        ///     Thrown when a parameter has no member of its name and the constructor is not marked
        ///     <see cref="ToonConstructorAttribute"/>, as System.Text.Json does: a parameter of its own would let documents
        ///     pass values the type never shows (a path, a mode) to the constructor of any type a property declares.
        /// </exception>
        [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = ToonSerializer.ReflectionJustification)]
        public static ObjectActivator For(TypeMetadata metadata)
        {
            var arguments = metadata.ConstructorParameters
                                    .Select(parameter =>
                                    {
                                        var property = metadata.Properties.FirstOrDefault(p => string.Equals(p.Property.Name, parameter.Name,
                                                                                                             StringComparison.OrdinalIgnoreCase));
                                        var missing = parameter.HasDefaultValue ? DefaultOf(parameter)
                                            : ValueTypeOf(parameter).IsValueType ? System.Activator.CreateInstance(ValueTypeOf(parameter))
                                            : null;

                                        if (property != null)
                                        {
                                            return new ConstructorArgument(parameter, property, missing);
                                        }

                                        if (HasHiddenMember(metadata.Type, parameter.Name!))
                                        {
                                            return new ConstructorArgument(parameter, null, missing, FromDocument: false);
                                        }

                                        if (metadata.Constructor?.IsDefined(typeof(ToonConstructorAttribute), inherit: false) != true)
                                        {
                                            throw new InvalidOperationException(
                                                $"The parameter '{parameter.Name}' of the constructor of {metadata.Type.Name} matches no public property. " +
                                                $"Add a property with its name, or mark the constructor [ToonConstructor] to read the parameter from the key '{parameter.Name}'.");
                                        }

                                        return new ConstructorArgument(parameter, null, missing);
                                    })
                                    .ToArray();

            return new ObjectActivator
            {
                New = CompileFactory(metadata.Type, metadata.Constructor),
                Arguments = arguments,
                BoundToConstructor = [.. metadata.Properties.Select(p => arguments.Any(a => a.Property == p))]
            };
        }

        /// <summary>
        ///     Whether the type or a base type has a property or field of the name (ignoring case) that is not written or
        ///     read: marked <see cref="ToonIgnoreAttribute"/>, without a public getter, or a field.
        /// </summary>
        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = ToonSerializer.ReflectionJustification)]
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = ToonSerializer.ReflectionJustification)]
        private static bool HasHiddenMember(Type type, string name)
        {
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                if (current.GetProperties(all).Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) ||
                    current.GetFields(all).Any(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The type of a parameter's values: the element type of an <c>in</c> parameter (<c>Int32&amp;</c>).</summary>
        internal static Type ValueTypeOf(ParameterInfo parameter) =>
            parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;

        /// <summary>
        ///     The default value of an optional parameter as the parameter's type: reflection gives an enum default of a
        ///     nullable enum parameter (<c>Perm? p = Perm.Write</c>) as its underlying number.
        /// </summary>
        private static object? DefaultOf(ParameterInfo parameter)
        {
            var value = parameter.DefaultValue;
            var target = Nullable.GetUnderlyingType(ValueTypeOf(parameter)) ?? ValueTypeOf(parameter);

            return value != null && target.IsEnum && value.GetType() != target ? Enum.ToObject(target, value) : value;
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
    ///     The setter reading may use: a public one (<c>set</c>, <c>init</c>), or a non-public one when the property is
    ///     marked <see cref="ToonIncludeAttribute"/>; null otherwise.
    /// </summary>
    internal static MethodInfo? SettableFromDocuments(PropertyInfo property) =>
        FindSetter(property) is { } setter && (setter.IsPublic || Attribute.IsDefined(property, typeof(ToonIncludeAttribute), inherit: true)) ? setter : null;

    /// <summary>
    ///     The setter of a property, as the generator finds it: the property's own (a private one of a base class
    ///     included, which a <see cref="PropertyInfo"/> of the derived type does not show), or that of the property it
    ///     overrides when the override only redefines the getter.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = ToonSerializer.ReflectionJustification)]
    private static MethodInfo? FindSetter(PropertyInfo property)
    {
        const BindingFlags declared = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        for (var current = property; current != null;)
        {
            var own = current.DeclaringType!.GetProperties(declared).FirstOrDefault(p => p.Name == current.Name) ?? current;

            if (own.GetSetMethod(nonPublic: true) is { } setter)
            {
                return setter;
            }

            // An override that only redefines the getter: the setter of the overridden property
            var getter = own.GetGetMethod(nonPublic: true);
            var baseGetter = getter?.GetBaseDefinition();
            current = baseGetter != null && baseGetter != getter
                ? baseGetter.DeclaringType!.GetProperties(declared).FirstOrDefault(p => p.Name == current.Name)
                : null;
        }

        return null;
    }

    /// <summary>
    /// Retrieves cached type metadata for a specified type, or generates and stores it if not already cached.
    /// </summary>
    /// <remarks>
    /// Includes public instance properties with a public getter that are not indexers and not marked
    /// with <see cref="ToonIgnoreAttribute"/>, ordered by <see cref="ToonPropertyOrderAttribute"/> (default 0), then base-class
    /// properties before derived ones, then declaration order.
    /// </remarks>
    private static readonly ConcurrentDictionary<(Type Interface, Type Runtime), TypeMetadata> InterfaceViews = new();

    /// <summary>
    ///     The metadata an object is written with: that of <paramref name="writtenType"/>, without the properties of an
    ///     interface that the object's type marks <see cref="ToonIgnoreAttribute"/> where it implements them (a value held
    ///     as <c>IAccount</c> whose class ignores <c>PasswordHash</c>).
    /// </summary>
    private static TypeMetadata GetWrittenMetadata(Type writtenType, Type runtimeType)
    {
        var metadata = GetTypeMetadata(writtenType);

        if (!writtenType.IsInterface || runtimeType == writtenType || runtimeType.IsInterface || metadata.Properties.Length == 0)
        {
            return metadata;
        }

        return InterfaceViews.GetOrAdd((writtenType, runtimeType), static (key, metadata) =>
        {
            var kept = metadata.Properties.Where(p => !IsIgnoredByImplementation(p.Property, key.Runtime)).ToArray();

            return kept.Length == metadata.Properties.Length
                ? metadata
                : new TypeMetadata
                {
                    Properties = kept,
                    Type = metadata.Type,
                    TypeConverter = metadata.TypeConverter,
                    FixedNamingPolicy = metadata.FixedNamingPolicy
                };
        }, metadata);
    }

    /// <summary>Whether the member of <paramref name="type"/> that implements an interface property is marked <see cref="ToonIgnoreAttribute"/>.</summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = ToonSerializer.ReflectionJustification)]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = ToonSerializer.ReflectionJustification)]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = ToonSerializer.ReflectionJustification)]
    private static bool IsIgnoredByImplementation(PropertyInfo interfaceProperty, Type type)
    {
        if (interfaceProperty.GetGetMethod() is not { } getter || !interfaceProperty.DeclaringType!.IsAssignableFrom(type))
        {
            return false;
        }

        var map = type.GetInterfaceMap(interfaceProperty.DeclaringType);
        var index = Array.IndexOf(map.InterfaceMethods, getter);

        if (index < 0 || map.TargetMethods[index] is not { } target)
        {
            return false;
        }

        // The property whose getter implements it, implicitly (public) or explicitly (private), in the class that declares it
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        var implementation = target.DeclaringType?.GetProperties(all).FirstOrDefault(p => p.GetGetMethod(nonPublic: true) == target);

        return implementation != null && Attribute.IsDefined(implementation, typeof(ToonIgnoreAttribute), inherit: true);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = ToonSerializer.ReflectionJustification)]
    private static TypeMetadata GetTypeMetadata(Type type)
    {
        // A value tuple keeps its items in fields, which the serializer does not read: it would be written as an empty
        // object and read back empty
        if (type is { IsValueType: true, IsGenericType: true, Namespace: "System" } && type.Name.StartsWith("ValueTuple`", StringComparison.Ordinal))
        {
            throw new NotSupportedException(
                $"Value tuples ({type.Name}) are not supported: their items are fields, not properties. Use a record or a class.");
        }

        if (IsUnsupportedType(type))
        {
            // As in System.Text.Json: reading would let documents create them (a FileInfo whose setters change a file, a
            // FileStream that truncates one), and writing walks reflection graphs or exposes handles
            throw new NotSupportedException(
                $"Serializing and deserializing {type.FullName} is not supported: it is a reflection, delegate, stream, file system, process or handle type. Register a converter to write it as a value.");
        }

        if (type is { IsValueType: true, IsGenericType: true } && (type.GetGenericTypeDefinition() == typeof(Memory<>) ||
                                                                   type.GetGenericTypeDefinition() == typeof(ReadOnlyMemory<>)))
        {
            throw new NotSupportedException($"{type.Name} is not supported: its items are not properties. Use an array ({type.GetGenericArguments()[0].Name}[]).");
        }

        return TypeMetadataCache.GetOrAdd(type, static t =>
        {
            var properties = new List<(PropertyMetadata Metadata, int Order, int Index)>();
            var index = 0;

            // An interface (a type values are written as) has the properties of the interfaces it extends too
            IEnumerable<PropertyInfo> declared = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            if (t.IsInterface)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                // The properties of the extended interfaces come first, as those of base classes do
                declared = t.GetInterfaces().SelectMany(i => i.GetProperties(BindingFlags.Public | BindingFlags.Instance)).Concat(declared)
                            .Where(p => names.Add(p.Name)).ToList();
            }
            else
            {
                // A property hidden with `new` is listed with the one that hides it: keep the most derived, as the
                // generator does (both would be written under one name)
                declared = declared.GroupBy(p => p.Name, StringComparer.Ordinal)
                                   .Select(g => g.MaxBy(p => InheritanceDepth(p.DeclaringType!))!).ToList();
            }

            foreach (var prop in declared)
            {
                // A ref struct (Span<T>) cannot be boxed into a value: such a property is left out, as by the generator
                if (prop.GetIndexParameters().Length > 0 || prop.GetGetMethod() == null || prop.PropertyType.IsByRefLike ||
                    prop.GetCustomAttribute<ToonIgnoreAttribute>() != null)
                {
                    continue;
                }

                var metadata = new PropertyMetadata
                {
                    Property = prop,
                    Getter = CompileGetter(prop),
                    Setter = SettableFromDocuments(prop) is { } setter ? CompileSetter(prop, setter) : null,
                    AttributeName = prop.GetCustomAttribute<ToonPropertyAttribute>()?.Name,
                    Converter = CreateAttributeConverter(prop.GetCustomAttribute<ToonConverterAttribute>()),
                    Primitive = PrimitiveAccessor.Create(prop),
                    IsRequired = prop.IsDefined(typeof(RequiredMemberAttribute), inherit: false)
                };

                var order = prop.GetCustomAttribute<ToonPropertyOrderAttribute>()?.Order ?? 0;
                properties.Add((metadata, order, InheritanceDepth(prop.DeclaringType!) * 100_000 + index++));
            }

            var (constructor, parameters) = SelectConstructor(t);
            PropertyMetadata[] ordered = [.. properties.OrderBy(p => p.Order).ThenBy(p => p.Index).Select(p => p.Metadata)];
            var setsRequired = constructor?.IsDefined(typeof(SetsRequiredMembersAttribute), inherit: false) == true;

            return new TypeMetadata
            {
                Type = t,
                Properties = ordered,
                RequiredProperties = setsRequired ? [] : [.. Enumerable.Range(0, ordered.Length).Where(p => ordered[p].IsRequired)],
                Constructor = constructor,
                ConstructorParameters = parameters,
                TypeConverter = CreateAttributeConverter(t.GetCustomAttribute<ToonConverterAttribute>()),
                FixedNamingPolicy = GetFixedNamingPolicy(t)
            };
        });
    }

    /// <summary>
    ///     Types that are not data: reflection (<see cref="Type"/>, <see cref="MemberInfo"/>, <see cref="Assembly"/>,
    ///     <see cref="Module"/>), delegates, serialization state, streams, file system entries, processes and handles.
    /// </summary>
    private static bool IsUnsupportedType(Type type) =>
        typeof(MemberInfo).IsAssignableFrom(type) || typeof(Assembly).IsAssignableFrom(type) || typeof(Module).IsAssignableFrom(type) ||
        typeof(Delegate).IsAssignableFrom(type) || type == typeof(System.Runtime.Serialization.SerializationInfo) ||
        typeof(Stream).IsAssignableFrom(type) || typeof(FileSystemInfo).IsAssignableFrom(type) ||
        typeof(System.Diagnostics.Process).IsAssignableFrom(type) || type == typeof(System.Diagnostics.ProcessStartInfo) ||
        typeof(System.Runtime.InteropServices.SafeHandle).IsAssignableFrom(type) || typeof(WaitHandle).IsAssignableFrom(type);

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
    private static Action<object, object?> CompileSetter(PropertyInfo property, MethodInfo setter)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Parameter(typeof(object), "value");
        var declaringType = setter.DeclaringType!;

        Expression target = declaringType.IsValueType
            ? Expression.Unbox(instance, declaringType)
            : Expression.Convert(instance, declaringType);

        // The setter found for the property, which can be a base class's (private, or of an overridden property)
        var assign = Expression.Call(target, setter, Expression.Convert(value, property.PropertyType));

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
    ///     Puts the path of the state in front of the path of an error that generated code raised (relative to the value
    ///     it was given), as the error leaves generated code. Returns false, for use as an exception filter.
    /// </summary>
    private static bool InPath(ToonException exception, SerializerState state)
    {
        if (!state.IsRoot)
        {
            exception.PrependPath(state.Path);
        }

        return false;
    }

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

        /// <summary>Whether the path is the root (<c>$</c>).</summary>
        public bool IsRoot => _path is not { Count: > 0 };

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
                        // Keys come from documents: escaped and shortened (ToonErrorText)
                        ToonErrorText.AppendMember(sb, name);
                    }
                }

                return sb.ToString();
            }
        }
    }
}
