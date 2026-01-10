using Microsoft.CodeAnalysis;
using ToonNet.SourceGenerators.Analysis;
using ToonNet.SourceGenerators.Utilities;

namespace ToonNet.SourceGenerators.Generators;

/// <summary>
/// Generates the static Serialize method for ToonSerializable classes.
/// </summary>
internal static class SerializeMethodGenerator
{
    /// <summary>
    /// Generates a complete Serialize method implementation.
    /// </summary>
    public static string Generate(ClassInfo classInfo)
    {
        var code = new CodeBuilder();

        // Method signature
        if (classInfo.IncludeDocumentation)
        {
            code.AppendLine("/// <summary>");
            code.AppendLine("/// Serializes this instance to a TOON document (generated code).");
            code.AppendLine("/// </summary>");
            code.AppendLine("/// <param name=\"value\">The instance to serialize.</param>");
            code.AppendLine("/// <param name=\"options\">Serialization options (uses defaults if null).</param>");
            code.AppendLine("/// <returns>A ToonDocument containing the serialized data.</returns>");
        }

        var accessibility = classInfo.GeneratePublicMethods ? "public" : "internal";
        code.AppendLine($"{accessibility} static global::ToonNet.Core.Models.ToonDocument Serialize(");
        code.AppendLine($"    {classInfo.Name} value,");
        code.AppendLine("    global::ToonNet.Core.Serialization.ToonSerializerOptions? options = null)");
        code.BeginBlock("");

        // Null check (if enabled)
        if (classInfo.IncludeNullChecks)
        {
            code.AppendLine("global::System.ArgumentNullException.ThrowIfNull(value);");
        }

        code.AppendLine("options ??= new global::ToonNet.Core.Serialization.ToonSerializerOptions();");
        code.AppendLine();

        // Create root object
        code.AppendLine("var obj = new global::ToonNet.Core.Models.ToonObject();");
        code.AppendLine();

        // Serialize each property
        foreach (var prop in classInfo.Properties)
        {
            GeneratePropertySerialization(code, prop, classInfo);
        }

        code.AppendLine();

        // Create and return document
        code.AppendLine("return new global::ToonNet.Core.Models.ToonDocument(obj);");

        code.EndBlock();

        return code.ToString();
    }

    /// <summary>
    /// Generates serialization code for a single property.
    /// </summary>
    private static void GeneratePropertySerialization(
        CodeBuilder code,
        PropertyInfo prop,
        ClassInfo classInfo)
    {
        var propName = prop.Name;
        var serializedName = GetSerializedPropertyName(prop, classInfo);
        var typeName = prop.Type.ToDisplayString();

        code.AppendLine($"// Serialize {propName}");

        // Check if property value is null
        if (prop.Type.IsReferenceType)
        {
            code.AppendLine($"if (value.{propName} != null)");
            code.BeginBlock("");
            GeneratePropertyValueSerialization(code, prop, serializedName, propName);
            code.EndBlock();
            code.AppendLine("else if (!options.IgnoreNullValues)");
            code.BeginBlock("");
            code.AppendLine($"obj[\"{serializedName}\"] = global::ToonNet.Core.Models.ToonNull.Instance;");
            code.EndBlock();
        }
        else
        {
            GeneratePropertyValueSerialization(code, prop, serializedName, propName);
        }

        code.AppendLine();
    }

    /// <summary>
    /// Generates the actual property serialization logic.
    /// </summary>
    private static void GeneratePropertyValueSerialization(
        CodeBuilder code,
        PropertyInfo prop,
        string serializedName,
        string propName)
    {
        var typeName = prop.Type.ToDisplayString();

        // Check for custom converter first
        if (prop.HasCustomConverter)
        {
            var converterName = prop.CustomConverter!.ToDisplayString();
            code.AppendLine($"var {propName}Converter = new {converterName}();");
            code.AppendLine($"var {propName}Serialized = {propName}Converter.Write(value.{propName}, options);");
            code.AppendLine($"obj[\"{serializedName}\"] = {propName}Serialized ?? global::ToonNet.Core.Models.ToonNull.Instance;");
            return;
        }

        if (IsSimpleType(prop.Type))
        {
            GenerateSimpleTypeSerialization(code, prop, serializedName, propName);
        }
        else
        {
            // For collections and complex types, fall back to ToonSerializer
            // Store the serialized TOON string as a ToonString value
            code.AppendLine($"var {propName}Serialized = global::ToonNet.Core.Serialization.ToonSerializer.Serialize(value.{propName});");
            code.AppendLine($"obj[\"{serializedName}\"] = new global::ToonNet.Core.Models.ToonString({propName}Serialized);");
        }
    }

    /// <summary>
    /// Generates serialization for simple types (string, int, double, bool, etc).
    /// </summary>
    private static void GenerateSimpleTypeSerialization(
        CodeBuilder code,
        PropertyInfo prop,
        string serializedName,
        string propName)
    {
        var typeName = prop.Type.ToDisplayString();

        if (typeName == "string")
        {
            code.AppendLine($"obj[\"{serializedName}\"] = new global::ToonNet.Core.Models.ToonString(value.{propName});");
        }
        else if (typeName is "bool" or "System.Boolean")
        {
            code.AppendLine($"obj[\"{serializedName}\"] = new global::ToonNet.Core.Models.ToonBoolean(value.{propName});");
        }
        else if (IsNumericType(typeName))
        {
            code.AppendLine($"obj[\"{serializedName}\"] = new global::ToonNet.Core.Models.ToonNumber((double)value.{propName});");
        }
        else
        {
            // Fallback for other simple types
            code.AppendLine($"obj[\"{serializedName}\"] = new global::ToonNet.Core.Models.ToonString(value.{propName}.ToString() ?? \"\");");
        }
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

/// <summary>
/// Property naming policy enum (defined in ToonSerializerOptions.cs in Core).
/// </summary>
public enum PropertyNamingPolicy
{
    Default = 0,
    CamelCase = 1,
    SnakeCase = 2,
    LowerCase = 3
}
