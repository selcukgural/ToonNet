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

        // Serialized names per naming policy (thread-safe)
        public ConcurrentDictionary<PropertyNamingPolicy, string> Names { get; } = new();
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
                    Converter = CreateAttributeConverter(prop.GetCustomAttribute<ToonConverterAttribute>())
                };

                var order = prop.GetCustomAttribute<ToonPropertyOrderAttribute>()?.Order ?? 0;
                properties.Add((metadata, order, InheritanceDepth(prop.DeclaringType!) * 100_000 + index++));
            }

            var (constructor, parameters) = SelectConstructor(t);

            return new TypeMetadata
            {
                Properties = [.. properties.OrderBy(p => p.Order).ThenBy(p => p.Index).Select(p => p.Metadata)],
                Constructor = constructor,
                ConstructorParameters = parameters,
                TypeConverter = CreateAttributeConverter(t.GetCustomAttribute<ToonConverterAttribute>())
            };
        });
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
    private static (ConstructorInfo? Constructor, ParameterInfo[] Parameters) SelectConstructor(Type type)
    {
        if (type.IsAbstract || type.IsInterface)
        {
            return (null, []);
        }

        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var constructor = constructors.FirstOrDefault(c => c.GetCustomAttribute<ToonConstructorAttribute>() != null)
                          ?? constructors.FirstOrDefault(c => c.GetParameters().Length == 0)
                          ?? constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

        return (constructor, constructor?.GetParameters() ?? []);
    }

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
    ///     Gets the serialized name for a property: <see cref="ToonPropertyAttribute"/> first, then the naming policy.
    /// </summary>
    private static string GetPropertyName(PropertyMetadata property, ToonSerializerOptions options)
    {
        if (property.AttributeName != null)
        {
            return property.AttributeName;
        }

        return property.Names.GetOrAdd(options.PropertyNamingPolicy, static (policy, name) => policy switch
        {
            PropertyNamingPolicy.CamelCase => ToCamelCase(name),
            PropertyNamingPolicy.SnakeCase => ToSnakeCase(name),
            PropertyNamingPolicy.LowerCase => name.ToLowerInvariant(),
            _                              => name
        }, property.Property.Name);
    }

    /// <summary>
    ///     Converts a property name to camelCase format.
    /// </summary>
    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
        {
            return name;
        }

        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    /// <summary>
    /// Converts a string to snake_case format.
    /// </summary>
    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var result = new StringBuilder();
        result.Append(char.ToLowerInvariant(name[0]));

        for (var i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(name[i]));
            }
            else
            {
                result.Append(name[i]);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Tracks the property path for error messages and the objects being serialized for cycle detection.
    /// </summary>
    private sealed class SerializerState
    {
        private readonly List<string> _path = [];

        public HashSet<object> Visiting { get; } = new(ReferenceEqualityComparer.Instance);

        public void Push(string segment) => _path.Add(segment);

        public void Pop() => _path.RemoveAt(_path.Count - 1);

        public string Path
        {
            get
            {
                var sb = new StringBuilder("$");

                foreach (var segment in _path)
                {
                    if (!segment.StartsWith('['))
                    {
                        sb.Append('.');
                    }

                    sb.Append(segment);
                }

                return sb.ToString();
            }
        }
    }
}
