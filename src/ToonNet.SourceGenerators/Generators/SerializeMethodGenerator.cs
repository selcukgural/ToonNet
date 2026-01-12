using System.Text;
using Microsoft.CodeAnalysis;
using ToonNet.SourceGenerators.Analysis;
using ToonNet.SourceGenerators.Utilities;

namespace ToonNet.SourceGenerators.Generators;

/// <summary>
///     Generates the static Serialize method for ToonSerializable classes.
/// </summary>
internal static class SerializeMethodGenerator
{
    /// <summary>
    ///     Generates a complete Serialize method implementation.
    /// </summary>
    /// <param name="classInfo">
    ///     Metadata about the class to generate the Serialize method for, including its properties,
    ///     null-check preferences, and naming policies.
    /// </param>
    /// <returns>
    ///     A string containing the generated C# code for the Serialize method.
    /// </returns>
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

        // Create a root object
        code.AppendLine("var obj = new global::ToonNet.Core.Models.ToonObject();");
        code.AppendLine();

        // Serialize each property
        foreach (var prop in classInfo.Properties)
        {
            GeneratePropertySerialization(code, prop, classInfo);
        }

        code.AppendLine();

        // Create and return a document
        code.AppendLine("return new global::ToonNet.Core.Models.ToonDocument(obj);");

        code.EndBlock();

        return code.ToString();
    }

    /// <summary>
    ///     Generates serialization code for a single property.
    /// </summary>
    /// <param name="code">The <see cref="CodeBuilder"/> instance to append the generated code to.</param>
    /// <param name="prop">Metadata about the property to serialize.</param>
    /// <param name="classInfo">Metadata about the class containing the property.</param>
    private static void GeneratePropertySerialization(CodeBuilder code, PropertyInfo prop, ClassInfo classInfo)
    {
        var propName = prop.Name;
        var serializedName = GetSerializedPropertyName(prop, classInfo);

        code.AppendLine($"// Serialize {propName}");

        // Check if the property value is null
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
    ///     Generates the actual property serialization logic.
    /// </summary>
    /// <param name="code">The <see cref="CodeBuilder"/> instance to append the generated code to.</param>
    /// <param name="prop">Metadata about the property to serialize.</param>
    /// <param name="serializedName">The name to use for the serialized property.</param>
    /// <param name="propName">The name of the property in the source class.</param>
    private static void GeneratePropertyValueSerialization(CodeBuilder code, PropertyInfo prop, string serializedName, string propName)
    {
        // Check for the custom converter first
        if (prop.HasCustomConverter)
        {
            var converterName = prop.CustomConverter!.ToDisplayString();
            code.AppendLine($"var {propName}Converter = new {converterName}();");
            code.AppendLine($"var {propName}Serialized = {propName}Converter.Write(value.{propName}, options);");
            code.AppendLine($"obj[\"{serializedName}\"] = {propName}Serialized ?? global::ToonNet.Core.Models.ToonNull.Instance;");
            return;
        }

        // Check for nested [ToonSerializable] class
        if (prop.IsNestedSerializable)
        {
            var nestedTypeName = GetTypeNameForNested(prop.Type);
            code.AppendLine($"var {propName}Doc = {nestedTypeName}.Serialize(value.{propName}, options);");
            code.AppendLine($"obj[\"{serializedName}\"] = {propName}Doc.Root;");
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
    ///     Generates serialization for simple types (string, int, double, bool, etc).
    /// </summary>
    /// <param name="code">The <see cref="CodeBuilder"/> instance to append the generated code to.</param>
    /// <param name="prop">Metadata about the property to serialize.</param>
    /// <param name="serializedName">The name to use for the serialized property.</param>
    /// <param name="propName">The name of the property in the source class.</param>
    private static void GenerateSimpleTypeSerialization(CodeBuilder code, PropertyInfo prop, string serializedName, string propName)
    {
        var typeName = prop.Type.ToDisplayString();

        switch (typeName)
        {
            case "string":
                code.AppendLine($"obj[\"{serializedName}\"] = new global::ToonNet.Core.Models.ToonString(value.{propName});");
                break;
            case "bool" or "System.Boolean":
                code.AppendLine($"obj[\"{serializedName}\"] = new global::ToonNet.Core.Models.ToonBoolean(value.{propName});");
                break;
            default:
            {
                code.AppendLine(IsNumericType(typeName)
                                    ? $"obj[\"{serializedName}\"] = new global::ToonNet.Core.Models.ToonNumber((double)value.{propName});"
                                    // Fallback for other simple types
                                    : $"obj[\"{serializedName}\"] = new global::ToonNet.Core.Models.ToonString(value.{propName}.ToString() ?? \"\");");

                break;
            }
        }
    }

    /// <summary>
    ///     Gets the serialized property name (respecting naming policy and custom names).
    /// </summary>
    /// <param name="prop">Metadata about the property to serialize.</param>
    /// <param name="classInfo">Metadata about the class containing the property.</param>
    /// <returns>The name to use for the serialized property.</returns>
    private static string GetSerializedPropertyName(PropertyInfo prop, ClassInfo classInfo)
    {
        // Custom name takes priority
        if (!string.IsNullOrEmpty(prop.CustomName))
        {
            return prop.CustomName;
        }

        // Apply naming policy
        var namingPolicy = GetNamingPolicy(classInfo.Attribute);
        return ApplyNamingPolicy(prop.Name, namingPolicy);
    }

    /// <summary>
    ///     Extracts the naming policy from the [ToonSerializable] attribute.
    /// </summary>
    /// <param name="attribute">The attribute data containing the naming policy.</param>
    /// <returns>The extracted naming policy, or the default policy if none is specified.</returns>
    private static PropertyNamingPolicy GetNamingPolicy(AttributeData? attribute)
    {
        if (attribute is null)
        {
            return PropertyNamingPolicy.Default;
        }

        var namedArgs = attribute.NamedArguments;

        foreach (var arg in namedArgs)
        {
            if (arg is { Key: "NamingPolicy", Value.Value: int policy })
            {
                return (PropertyNamingPolicy)policy;
            }
        }

        return PropertyNamingPolicy.Default;
    }

    /// <summary>
    ///     Applies a naming policy to a property name.
    /// </summary>
    /// <param name="name">The original property name.</param>
    /// <param name="policy">The naming policy to apply.</param>
    /// <returns>The property name after applying the naming policy.</returns>
    private static string ApplyNamingPolicy(string name, PropertyNamingPolicy policy)
    {
        return policy switch
        {
            PropertyNamingPolicy.CamelCase => ToCamelCase(name),
            PropertyNamingPolicy.SnakeCase => ToSnakeCase(name),
            PropertyNamingPolicy.LowerCase => name.ToLowerInvariant(),
            _                              => name
        };
    }

    /// <summary>
    /// Converts the specified string to camel case formatting.
    /// </summary>
    /// <param name="name">
    /// The input string to convert to camel case. This is typically a property or field name.
    /// </param>
    /// <returns>
    /// A string where the first character is converted to lowercase and the remaining characters are unchanged.
    /// </returns>
    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    /// <summary>
    /// Converts a given string to snake_case format by inserting underscores before uppercase letters
    /// and converting all characters to lowercase.
    /// </summary>
    /// <param name="name">
    /// The string to be converted to snake_case format. Expected to be a non-null string.
    /// </param>
    /// <returns>
    /// A string transformed to snake_case format. If the input is null or empty, the original input
    /// string is returned.
    /// </returns>
    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var result = new StringBuilder();

        for (var i = 0; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && i > 0)
            {
                result.Append('_');
            }

            result.Append(char.ToLowerInvariant(name[i]));
        }

        return result.ToString();
    }

    /// <summary>
    ///     Checks if a type is a simple/primitive type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a simple/primitive type; otherwise, false.</returns>
    private static bool IsSimpleType(ITypeSymbol type)
    {
        var name = type.ToDisplayString();

        return name is "string" or "int" or "long" or "double" or "decimal" or "bool" or "float" or "byte" or "short" or "uint" or "ulong"
                       or "System.String" or "System.Int32" or "System.Int64" or "System.Double" or "System.Decimal" or "System.Boolean"
                       or "System.Single" or "System.Byte" or "System.Int16" or "System.UInt32" or "System.UInt64";
    }

    /// <summary>
    ///     Checks if a type is a numeric type.
    /// </summary>
    /// <param name="typeName">The name of the type to check.</param>
    /// <returns>True if the type is numeric; otherwise, false.</returns>
    private static bool IsNumericType(string typeName)
    {
        return typeName is "int" or "long" or "double" or "decimal" or "float" or "byte" or "short" or "uint" or "ulong" or "System.Int32"
                           or "System.Int64" or "System.Double" or "System.Decimal" or "System.Single" or "System.Byte" or "System.Int16"
                           or "System.UInt32" or "System.UInt64";
    }

    /// <summary>
    ///     Checks if a type is a collection type (array, List, IEnumerable, etc).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a collection type; otherwise, false.</returns>
    private static bool IsCollectionType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol)
        {
            return true;
        }

        var name = type.Name;

        if (name is "List" or "Array" or "IEnumerable" or "ICollection" or "IList")
        {
            return true;
        }

        // Check interfaces
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        foreach (var namedTypeSymbol in namedType.AllInterfaces)
        {
            if (namedTypeSymbol.Name is "IEnumerable" or "ICollection" or "IList")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Checks if a type is nullable (T?).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is nullable; otherwise, false.</returns>
    private static bool IsNullableType(ITypeSymbol type)
    {
        return type is { IsValueType: true, NullableAnnotation: NullableAnnotation.Annotated };
    }

    /// <summary>
    ///     Gets the simple type name for nested [ToonSerializable] classes (they're in the same namespace).
    /// </summary>
    /// <param name="type">The type to get the name for.</param>
    /// <returns>The simple type name for the nested class.</returns>
    private static string GetTypeNameForNested(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType)
        {
            return namedType.Name;
        }

        return type.ToDisplayString();
    }
}

/// <summary>
///     Property naming policy enum (defined in ToonSerializerOptions.cs in Core).
/// </summary>
public enum PropertyNamingPolicy
{
    Default = 0,
    CamelCase = 1,
    SnakeCase = 2,
    LowerCase = 3
}