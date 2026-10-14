using System.Diagnostics.CodeAnalysis;
using System.Text;
using ToonNet.Core.Models;

namespace ToonNet.Core.Serialization;

// The shape of a type as a TOON template, for prompts: the document the serializer writes for a value of the type, with
// a placeholder (<integer>, <string>, ...) in place of each value and N in place of each length.
public static partial class ToonSerializer
{
    /// <summary>A TOON template of the documents <see cref="Serialize{T}(T, ToonSerializerOptions)"/> writes for a type.</summary>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    internal static string DescribeShape(Type type, ToonSerializerOptions options)
    {
        var writer = new ShapeWriter(options);
        writer.WriteRoot(type);
        return writer.ToString();
    }

    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    private sealed class ShapeWriter(ToonSerializerOptions options)
    {
        private enum Kind : byte
        {
            Scalar,
            Object,
            Collection,
            Dictionary
        }

        private readonly StringBuilder _text = new();
        private readonly HashSet<Type> _visiting = [];
        private readonly int _indent = options.ToonOptions.IndentSize;
        private readonly char _delimiter = options.ToonOptions.Delimiter;

        /// <summary>Separates the choices of a placeholder: '|', or '/' when '|' is the delimiter of the cells.</summary>
        private readonly char _or = options.ToonOptions.Delimiter == '|' ? '/' : '|';

        public override string ToString() => _text.ToString();

        public void WriteRoot(Type type)
        {
            switch (Classify(type, out var inner))
            {
                case Kind.Scalar:
                    Line(0, Placeholder(type));
                    break;
                case Kind.Collection:
                    _visiting.Add(type);
                    WriteArray(null, inner!, 0);
                    break;
                case Kind.Dictionary when KeyedColumns(inner!) is { } columns:
                    WriteKeyedTable(null, columns, 0);
                    break;
                case Kind.Dictionary:
                    _visiting.Add(type);
                    WriteMember("<key>", inner!, 0);
                    break;
                default:
                    WriteFields(type, 0);
                    break;
            }
        }

        /// <summary>A cell of a table header: a name, or a name with the columns of a nested group (<c>Customer{Name,Country}</c>).</summary>
        private sealed record Column(string Header, List<string> Placeholders);

        /// <summary>Whether values of the type (or of the property) are written by a converter, whose output is unknown.</summary>
        private bool IsConverted(Type type, PropertyMetadata? property = null) =>
            property?.Converter != null || options.GetConverter(type) != null ||
            (Classify(type, out _) == Kind.Object && GetTypeMetadata(type).TypeConverter != null);

        /// <summary>The columns of a dictionary's value type when the serializer writes it as a keyed table (§9.5), or null.</summary>
        private List<Column>? KeyedColumns(Type valueType) =>
            Classify(valueType, out _) == Kind.Object && !IsConverted(valueType) && !_visiting.Contains(valueType) ? FlatColumns(valueType, 0) : null;

        private void WriteKeyedTable(string? key, List<Column> columns, int depth)
        {
            var bracket = "[N:" + (_delimiter == ',' ? "" : _delimiter.ToString()) + "]";
            Line(depth, $"{key}{bracket}{{{string.Join(_delimiter, columns.Select(c => c.Header))}}}:");
            Line(depth + 1, "<key>: " + string.Join(_delimiter, columns.SelectMany(c => c.Placeholders)));
        }

        private static Kind Classify(Type type, out Type? inner)
        {
            inner = null;
            var target = Nullable.GetUnderlyingType(type) ?? type;

            if (IsScalarType(target) || target == typeof(object) || typeof(ToonValue).IsAssignableFrom(target) || ToonJsonInterop.IsDocumentType(target))
            {
                return Kind.Scalar;
            }

            if (DictionaryValueType(target) is { } valueType)
            {
                inner = valueType;
                return Kind.Dictionary;
            }

            if (GetElementType(target) is { } elementType)
            {
                inner = elementType;
                return Kind.Collection;
            }

            return Kind.Object;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = ReflectionJustification)]
        private static Type? DictionaryValueType(Type type)
        {
            foreach (var candidate in (Type[])[type, .. type.GetInterfaces()])
            {
                if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() is var definition &&
                    (definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>)))
                {
                    return candidate.GetGenericArguments()[1];
                }
            }

            return null;
        }

        /// <summary>
        ///     The placeholder of a value: <c>&lt;integer&gt;</c>, <c>&lt;Draft|Active&gt;</c>, <c>&lt;string|null&gt;</c>;
        ///     <paramref name="mayBeNull"/> adds <c>null</c> for a nullable reference.
        /// </summary>
        private string Placeholder(Type type, bool mayBeNull = false, bool converted = false)
        {
            var underlying = Nullable.GetUnderlyingType(type);
            var target = underlying ?? type;
            string name;

            if (converted)
            {
                name = "any"; // what a converter writes is not known from the type
            }
            else if (target.IsEnum && target.IsDefined(typeof(FlagsAttribute), inherit: false))
            {
                name = "flags: " + string.Join(_or, Enum.GetNames(target)); // written as "Read, Write"
            }
            else if (target.IsEnum)
            {
                name = string.Join(_or, Enum.GetNames(target));
            }
            else if (target == typeof(string) || target == typeof(char))
            {
                name = "string";
            }
            else if (target == typeof(bool))
            {
                name = "boolean";
            }
            else if (target == typeof(float) || target == typeof(double) || target == typeof(decimal) || target == typeof(Half))
            {
                name = "number";
            }
            else if (IsNumericType(target))
            {
                name = "integer";
            }
            else if (target == typeof(DateTime) || target == typeof(DateTimeOffset))
            {
                name = "date-time";
            }
            else if (target == typeof(DateOnly))
            {
                name = "date";
            }
            else if (target == typeof(TimeOnly))
            {
                name = "time";
            }
            else if (target == typeof(TimeSpan))
            {
                name = "duration";
            }
            else if (target == typeof(Guid))
            {
                name = "uuid";
            }
            else if (target == typeof(Uri))
            {
                name = "uri";
            }
            else
            {
                name = "any";
            }

            return underlying != null || mayBeNull ? $"<{name}{_or}null>" : $"<{name}>";
        }

        private void Line(int depth, string text)
        {
            if (_text.Length > 0)
            {
                _text.Append('\n');
            }

            _text.Append(' ', depth * _indent).Append(text);
        }

        /// <summary>The members of an object type, one per line at <paramref name="depth"/>, in the serializer's order.</summary>
        private void WriteFields(Type type, int depth)
        {
            _visiting.Add(type);
            var metadata = GetTypeMetadata(type);

            foreach (var property in metadata.Properties)
            {
                if (!options.IncludeReadOnlyProperties && property.Setter == null && !IsConstructorBound(metadata, property))
                {
                    continue;
                }

                WriteMember(GetPropertyName(property, metadata, options), property.Property.PropertyType, depth, MayBeNull(property.Property),
                            IsConverted(property.Property.PropertyType, property));
            }

            _visiting.Remove(type);
        }

        private static bool IsConstructorBound(TypeMetadata metadata, PropertyMetadata property) =>
            metadata.ConstructorParameters.Any(p => string.Equals(p.Name, property.Property.Name, StringComparison.OrdinalIgnoreCase));

        /// <summary>Whether a property of a reference type is annotated as nullable (<c>string?</c>).</summary>
        private bool MayBeNull(System.Reflection.PropertyInfo property) =>
            !property.PropertyType.IsValueType && _nullability.Create(property).ReadState == System.Reflection.NullabilityState.Nullable;

        private readonly System.Reflection.NullabilityInfoContext _nullability = new();

        private void WriteMember(string name, Type type, int depth, bool mayBeNull = false, bool converted = false)
        {
            var key = name == "<key>" ? name : Shared.ToonKeys.Encode(name);

            var kind = Classify(type, out var inner);

            switch (converted ? Kind.Scalar : kind)
            {
                case Kind.Scalar:
                    Line(depth, $"{key}: {Placeholder(type, mayBeNull, converted)}");
                    break;
                case Kind.Collection or Kind.Dictionary when !_visiting.Add(type):
                    Line(depth, $"{key}: <{type.Name}>"); // a collection within itself
                    break;
                case Kind.Collection:
                    WriteArray(key, inner!, depth);
                    _visiting.Remove(type);
                    break;
                case Kind.Dictionary when KeyedColumns(inner!) is { } columns:
                    WriteKeyedTable(key, columns, depth);
                    _visiting.Remove(type);
                    break;
                case Kind.Dictionary:
                    Line(depth, key + ":");
                    WriteMember("<key>", inner!, depth + 1);
                    _visiting.Remove(type);
                    break;
                default:
                    if (_visiting.Contains(type))
                    {
                        Line(depth, $"{key}: <{type.Name}>"); // a type within itself
                    }
                    else
                    {
                        Line(depth, key + ":");
                        WriteFields(type, depth + 1);
                    }

                    break;
            }
        }

        /// <summary>An array of <paramref name="element"/>: inline for scalars, a table for flat objects, a list otherwise.</summary>
        private void WriteArray(string? key, Type element, int depth)
        {
            var header = (key ?? "") + "[N" + (_delimiter == ',' ? "" : _delimiter.ToString()) + "]";

            var converted = IsConverted(element);

            var kind = Classify(element, out var inner);

            switch (converted ? Kind.Scalar : kind)
            {
                case Kind.Scalar:
                    Line(depth, $"{header}: {Placeholder(element, converted: converted)}{_delimiter}...");
                    return;
                case Kind.Object when !_visiting.Contains(element) && FlatColumns(element, 0) is { } columns:
                    Line(depth, $"{header}{{{string.Join(_delimiter, columns.Select(c => c.Header))}}}:");
                    Line(depth + 1, string.Join(_delimiter, columns.SelectMany(c => c.Placeholders)));
                    return;
            }

            Line(depth, header + ":");
            WriteListItem(element, inner, depth + 1);
        }

        /// <summary>
        ///     The columns of a type when the serializer writes its objects as table rows: members that are scalars, or flat
        ///     objects themselves (a nested field group, §9.4); null otherwise.
        /// </summary>
        private List<Column>? FlatColumns(Type type, int level)
        {
            if (level > 8 || _visiting.Contains(type))
            {
                return null;
            }

            var metadata = GetTypeMetadata(type);
            var columns = new List<Column>();
            _visiting.Add(type);

            try
            {
                foreach (var property in metadata.Properties)
                {
                    if (!options.IncludeReadOnlyProperties && property.Setter == null && !IsConstructorBound(metadata, property))
                    {
                        continue;
                    }

                    var propertyType = property.Property.PropertyType;
                    var name = Shared.ToonKeys.Encode(GetPropertyName(property, metadata, options));
                    var converted = IsConverted(propertyType, property);

                    if (converted || Classify(propertyType, out _) == Kind.Scalar)
                    {
                        columns.Add(new Column(name, [Placeholder(propertyType, MayBeNull(property.Property), converted)]));
                    }
                    else if (Classify(propertyType, out _) == Kind.Object && FlatColumns(propertyType, level + 1) is { } group)
                    {
                        columns.Add(new Column($"{name}{{{string.Join(_delimiter, group.Select(c => c.Header))}}}", [.. group.SelectMany(c => c.Placeholders)]));
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            finally
            {
                _visiting.Remove(type);
            }

            return columns.Count > 0 ? columns : null;
        }

        /// <summary>One list item: <c>- </c> followed by the item, whose first line shares the hyphen's line (§10).</summary>
        private void WriteListItem(Type element, Type? inner, int depth)
        {
            // Write the item one level deeper, then put the hyphen in place of the first line's indentation
            var start = _text.Length;

            switch (Classify(element, out _))
            {
                case Kind.Collection or Kind.Dictionary when !_visiting.Add(element):
                    Line(depth + 1, $"<{element.Name}>"); // a collection within itself
                    break;
                case Kind.Collection:
                    WriteArray(null, inner!, depth + 1);
                    _visiting.Remove(element);
                    break;
                case Kind.Dictionary:
                    WriteMember("<key>", inner!, depth + 1);
                    _visiting.Remove(element);
                    break;
                default:
                    if (_visiting.Contains(element))
                    {
                        Line(depth + 1, $"<{element.Name}>");
                    }
                    else
                    {
                        WriteFields(element, depth + 1);
                    }

                    break;
            }

            if (_text.Length == start)
            {
                // An object without members is a bare hyphen
                Line(depth, "-");
                return;
            }

            var firstLine = start == 0 ? 0 : start + 1;
            _text.Remove(firstLine, (depth + 1) * _indent).Insert(firstLine, new string(' ', depth * _indent) + "- ");
        }
    }
}
