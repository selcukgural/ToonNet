using Microsoft.CodeAnalysis;
using ToonNet.SourceGenerators.Analysis;
using ToonNet.SourceGenerators.Utilities;

namespace ToonNet.SourceGenerators.Generators;

/// <summary>
/// Generates the static Deserialize method for ToonSerializable classes.
/// </summary>
internal static class DeserializeMethodGenerator
{
    /// <summary>
    /// Generates a complete Deserialize method implementation.
    /// </summary>
    public static string Generate(ClassInfo classInfo)
    {
        var code = new CodeBuilder();

        // Method signature
        if (classInfo.IncludeDocumentation)
        {
            code.AppendLine("/// <summary>");
            code.AppendLine("/// Deserializes a TOON document to an instance (generated code).");
            code.AppendLine("/// </summary>");
            code.AppendLine("/// <param name=\"doc\">The TOON document to deserialize.</param>");
            code.AppendLine("/// <param name=\"options\">Deserialization options (uses defaults if null).</param>");
            code.AppendLine("/// <returns>A new instance populated from the document.</returns>");
            code.AppendLine("/// <exception cref=\"global::System.ArgumentNullException\">Thrown if doc is null.</exception>");
        }

        var accessibility = classInfo.GeneratePublicMethods ? "public" : "internal";
        code.AppendLine($"{accessibility} static {classInfo.Name} Deserialize(");
        code.AppendLine("    global::ToonNet.Core.Models.ToonDocument doc,");
        code.AppendLine("    global::ToonNet.Core.Serialization.ToonSerializerOptions? options = null)");
        code.BeginBlock("");

        // Null check (if enabled)
        if (classInfo.IncludeNullChecks)
        {
            code.AppendLine("global::System.ArgumentNullException.ThrowIfNull(doc);");
        }

        code.AppendLine("options ??= new global::ToonNet.Core.Serialization.ToonSerializerOptions();");
        code.AppendLine();

        // Get root object
        code.AppendLine("var obj = (global::ToonNet.Core.Models.ToonObject)doc.Root;");
        
        // Use custom constructor or default constructor
        if (classInfo.HasCustomConstructor && classInfo.CustomConstructor?.Parameters.Length > 0)
        {
            // For now, fall back to parameterless constructor (Phase 4 enhancement)
            code.AppendLine($"var result = new {classInfo.Name}();");
        }
        else
        {
            code.AppendLine($"var result = new {classInfo.Name}();");
        }
        code.AppendLine();

        // Deserialize each property
        foreach (var prop in classInfo.Properties)
        {
            GeneratePropertyDeserialization(code, prop, classInfo);
        }

        code.AppendLine();
        code.AppendLine("return result;");

        code.EndBlock();

        return code.ToString();
    }

    /// <summary>
    /// Generates deserialization code for a single property.
    /// </summary>
    private static void GeneratePropertyDeserialization(
        CodeBuilder code,
        PropertyInfo prop,
        ClassInfo classInfo)
    {
        var propName = prop.Name;
        var serializedName = GetSerializedPropertyName(prop, classInfo);
        var typeName = prop.Type.ToDisplayString();

        code.AppendLine($"// Deserialize {propName}");
        code.AppendLine($"if (obj.Properties.TryGetValue(\"{serializedName}\", out var {propName}Value))");
        code.BeginBlock("");

        if (prop.Type.IsReferenceType)
        {
            code.AppendLine($"if ({propName}Value is not global::ToonNet.Core.Models.ToonNull)");
            code.BeginBlock("");
            GeneratePropertyValueDeserialization(code, prop, propName);
            code.EndBlock();
        }
        else
        {
            GeneratePropertyValueDeserialization(code, prop, propName);
        }

        code.EndBlock();
        code.AppendLine();
    }

    /// <summary>
    /// Generates the actual property deserialization logic.
    /// </summary>
    private static void GeneratePropertyValueDeserialization(
        CodeBuilder code,
        PropertyInfo prop,
        string propName)
    {
        var typeName = prop.Type.ToDisplayString();

        // Check nullable first, before checking if it's a simple type
        if (IsNullableType(prop.Type))
        {
            GenerateNullableTypeDeserialization(code, prop, propName);
        }
        else if (IsSimpleType(prop.Type))
        {
            GenerateSimpleTypeDeserialization(code, prop, propName);
        }
        else if (IsCollectionType(prop.Type))
        {
            GenerateCollectionDeserialization(code, prop, propName);
        }
        else
        {
            // Complex type - deserialize using reflection fallback
            code.AppendLine($"var innerString = {propName}Value.ToString();");
            code.AppendLine($"result.{prop.Name} = global::ToonNet.Core.Serialization.ToonSerializer.Deserialize<{typeName}>(innerString);");
        }
    }

    /// <summary>
    /// Generates deserialization for simple types (string, int, double, bool, etc).
    /// </summary>
    private static void GenerateSimpleTypeDeserialization(
        CodeBuilder code,
        PropertyInfo prop,
        string propName)
    {
        var typeName = prop.Type.ToDisplayString();

        if (typeName == "string")
        {
            code.AppendLine($"if ({propName}Value is global::ToonNet.Core.Models.ToonString {propName}String)");
            code.AppendLine($"    result.{prop.Name} = {propName}String.Value;");
        }
        else if (typeName is "bool" or "System.Boolean")
        {
            code.AppendLine($"if ({propName}Value is global::ToonNet.Core.Models.ToonBoolean {propName}Bool)");
            code.AppendLine($"    result.{prop.Name} = {propName}Bool.Value;");
        }
        else if (typeName is "int" or "System.Int32")
        {
            code.AppendLine($"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num && int.TryParse({propName}Num.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture), global::System.Globalization.NumberStyles.Any, global::System.Globalization.CultureInfo.InvariantCulture, out var {propName}Int))");
            code.AppendLine($"    result.{prop.Name} = {propName}Int;");
        }
        else if (typeName is "long" or "System.Int64")
        {
            code.AppendLine($"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num && long.TryParse({propName}Num.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture), global::System.Globalization.NumberStyles.Any, global::System.Globalization.CultureInfo.InvariantCulture, out var {propName}Long))");
            code.AppendLine($"    result.{prop.Name} = {propName}Long;");
        }
        else if (typeName is "double" or "System.Double")
        {
            code.AppendLine($"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num)");
            code.AppendLine($"    result.{prop.Name} = {propName}Num.Value;");
        }
        else if (typeName is "decimal" or "System.Decimal")
        {
            code.AppendLine($"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num && decimal.TryParse({propName}Num.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture), global::System.Globalization.NumberStyles.Any, global::System.Globalization.CultureInfo.InvariantCulture, out var {propName}Decimal))");
            code.AppendLine($"    result.{prop.Name} = {propName}Decimal;");
        }
        else if (typeName is "float" or "System.Single")
        {
            code.AppendLine($"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num)");
            code.AppendLine($"    result.{prop.Name} = (float){propName}Num.Value;");
        }
        else if (typeName is "byte" or "System.Byte")
        {
            code.AppendLine($"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num && byte.TryParse({propName}Num.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture), global::System.Globalization.NumberStyles.Any, global::System.Globalization.CultureInfo.InvariantCulture, out var {propName}Byte))");
            code.AppendLine($"    result.{prop.Name} = {propName}Byte;");
        }
        else if (typeName is "short" or "System.Int16")
        {
            code.AppendLine($"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num && short.TryParse({propName}Num.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture), global::System.Globalization.NumberStyles.Any, global::System.Globalization.CultureInfo.InvariantCulture, out var {propName}Short))");
            code.AppendLine($"    result.{prop.Name} = {propName}Short;");
        }
        else if (typeName is "uint" or "System.UInt32")
        {
            code.AppendLine($"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num && uint.TryParse({propName}Num.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture), global::System.Globalization.NumberStyles.Any, global::System.Globalization.CultureInfo.InvariantCulture, out var {propName}UInt))");
            code.AppendLine($"    result.{prop.Name} = {propName}UInt;");
        }
        else if (typeName is "ulong" or "System.UInt64")
        {
            code.AppendLine($"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num && ulong.TryParse({propName}Num.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture), global::System.Globalization.NumberStyles.Any, global::System.Globalization.CultureInfo.InvariantCulture, out var {propName}ULong))");
            code.AppendLine($"    result.{prop.Name} = {propName}ULong;");
        }
        else
        {
            // Fallback for other simple types
            code.AppendLine($"result.{prop.Name} = ({typeName}){propName}Value;");
        }
    }

    /// <summary>
    /// Generates deserialization for collection types (array, List, IEnumerable).
    /// </summary>
    private static void GenerateCollectionDeserialization(
        CodeBuilder code,
        PropertyInfo prop,
        string propName)
    {
        code.AppendLine($"var arrayString = {propName}Value.ToString();");
        code.AppendLine($"result.{prop.Name} = global::ToonNet.Core.Serialization.ToonSerializer.Deserialize<{prop.Type.ToDisplayString()}>(arrayString);");
    }

    /// <summary>
    /// Generates deserialization for nullable types (T?).
    /// </summary>
    private static void GenerateNullableTypeDeserialization(
        CodeBuilder code,
        PropertyInfo prop,
        string propName)
    {
        var underlyingType = ((INamedTypeSymbol)prop.Type).TypeArguments[0].ToDisplayString();

        code.AppendLine($"if ({propName}Value is not global::ToonNet.Core.Models.ToonNull)");
        code.BeginBlock("");

        if (IsNumericType(underlyingType))
        {
            code.AppendLine($"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num && {underlyingType}.TryParse({propName}Num.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture), global::System.Globalization.NumberStyles.Any, global::System.Globalization.CultureInfo.InvariantCulture, out var {propName}Parsed))");
            code.AppendLine($"    result.{prop.Name} = {propName}Parsed;");
        }
        else
        {
            code.AppendLine($"result.{prop.Name} = ({underlyingType}){propName}Value;");
        }

        code.EndBlock();
    }

    /// <summary>
    /// Gets the serialized property name (respecting naming policy and custom names).
    /// </summary>
    private static string GetSerializedPropertyName(PropertyInfo prop, ClassInfo classInfo)
    {
        // Custom name takes priority
        if (!string.IsNullOrEmpty(prop.CustomName))
            return prop.CustomName;

        // Apply naming policy
        var namingPolicy = GetNamingPolicy(classInfo.Attribute);
        return ApplyNamingPolicy(prop.Name, namingPolicy);
    }

    /// <summary>
    /// Extracts the naming policy from the [ToonSerializable] attribute.
    /// </summary>
    private static PropertyNamingPolicy GetNamingPolicy(AttributeData? attribute)
    {
        if (attribute is null)
            return PropertyNamingPolicy.Default;

        var namedArgs = attribute.NamedArguments;
        foreach (var arg in namedArgs)
        {
            if (arg.Key == "NamingPolicy" && arg.Value.Value is int policy)
                return (PropertyNamingPolicy)policy;
        }

        return PropertyNamingPolicy.Default;
    }

    /// <summary>
    /// Applies a naming policy to a property name.
    /// </summary>
    private static string ApplyNamingPolicy(string name, PropertyNamingPolicy policy)
    {
        return policy switch
        {
            PropertyNamingPolicy.CamelCase => ToCamelCase(name),
            PropertyNamingPolicy.SnakeCase => ToSnakeCase(name),
            PropertyNamingPolicy.LowerCase => name.ToLowerInvariant(),
            _ => name
        };
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && i > 0)
                result.Append('_');
            result.Append(char.ToLowerInvariant(name[i]));
        }
        return result.ToString();
    }

    /// <summary>
    /// Checks if a type is a simple/primitive type.
    /// </summary>
    private static bool IsSimpleType(ITypeSymbol type)
    {
        var name = type.ToDisplayString();
        return name is "string" or "int" or "long" or "double" or "decimal"
            or "bool" or "float" or "byte" or "short" or "uint" or "ulong"
            or "System.String" or "System.Int32" or "System.Int64" or "System.Double"
            or "System.Decimal" or "System.Boolean" or "System.Single" or "System.Byte"
            or "System.Int16" or "System.UInt32" or "System.UInt64";
    }

    /// <summary>
    /// Checks if a type is a numeric type.
    /// </summary>
    private static bool IsNumericType(string typeName)
    {
        return typeName is "int" or "long" or "double" or "decimal" or "float" or "byte" or "short"
            or "uint" or "ulong" or "System.Int32" or "System.Int64" or "System.Double"
            or "System.Decimal" or "System.Single" or "System.Byte" or "System.Int16"
            or "System.UInt32" or "System.UInt64";
    }

    /// <summary>
    /// Checks if a type is a collection type (array, List, IEnumerable, etc).
    /// </summary>
    private static bool IsCollectionType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol)
            return true;

        var name = type.Name;
        if (name is "List" or "Array" or "IEnumerable" or "ICollection" or "IList")
            return true;

        // Check interfaces
        if (type is INamedTypeSymbol namedType)
        {
            foreach (var iface in namedType.AllInterfaces)
            {
                if (iface.Name is "IEnumerable" or "ICollection" or "IList")
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a type is nullable (T?).
    /// </summary>
    private static bool IsNullableType(ITypeSymbol type)
    {
        return type.IsValueType && type.NullableAnnotation == NullableAnnotation.Annotated;
    }
}
