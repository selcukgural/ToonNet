using System.Text;
using Microsoft.CodeAnalysis;
using ToonNet.SourceGenerators.Analysis;
using ToonNet.SourceGenerators.Utilities;

namespace ToonNet.SourceGenerators.Generators;

/// <summary>
///     Generates the static Deserialize method for ToonSerializable classes.
/// </summary>
internal static class DeserializeMethodGenerator
{
    /// <summary>
    ///     Generates a complete Deserialize method implementation for a given class.
    /// </summary>
    /// <param name="classInfo">
    ///     Metadata about the class for which the Deserialize method is being generated.
    ///     Includes information such as class name, properties, constructors, and serialization options.
    /// </param>
    /// <returns>
    ///     A string containing the generated C# code for the Deserialize method.
    /// </returns>
    public static string Generate(ClassInfo classInfo)
    {
        var code = new CodeBuilder();

        // Add method signature and XML documentation if enabled
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

        // Determine method accessibility (public/internal)
        var accessibility = classInfo.GeneratePublicMethods ? "public" : "internal";
        code.AppendLine($"{accessibility} static {classInfo.Name} Deserialize(");
        code.AppendLine("    global::ToonNet.Core.Models.ToonDocument doc,");
        code.AppendLine("    global::ToonNet.Core.Serialization.ToonSerializerOptions? options = null)");
        code.BeginBlock("");

        // Add null check for the document if enabled
        if (classInfo.IncludeNullChecks)
        {
            code.AppendLine("global::System.ArgumentNullException.ThrowIfNull(doc);");
        }

        // Initialize options if null
        code.AppendLine("options ??= new global::ToonNet.Core.Serialization.ToonSerializerOptions();");
        code.AppendLine();

        // Retrieve the root object from the document
        code.AppendLine("var obj = (global::ToonNet.Core.Models.ToonObject)doc.Root;");
        code.AppendLine();

        // Create an instance of the class using either a custom or default constructor
        if (classInfo is { HasCustomConstructor: true, CustomConstructor.Parameters.Length: > 0 })
        {
            // Build constructor parameters using parameter names
            var ctor = classInfo.CustomConstructor;
            var parameterStrings = new List<string>();

            foreach (var param in ctor.Parameters)
            {
                // Use the actual constructor parameter name (not property name)
                parameterStrings.Add($"{param.Name}: default");
            }

            var ctorParams = string.Join(", ", parameterStrings);
            code.AppendLine($"var result = new {classInfo.Name}({ctorParams});");
        }
        else
        {
            code.AppendLine($"var result = new {classInfo.Name}();");
        }

        code.AppendLine();

        // Generate deserialization logic for each property in the class
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
    ///     Generates the deserialization logic for a single property of the class.
    /// </summary>
    /// <param name="code">
    ///     The <see cref="CodeBuilder"/> instance used to append the generated code.
    /// </param>
    /// <param name="prop">
    ///     Metadata about the property being deserialized, including its name, type, and serialization details.
    /// </param>
    /// <param name="classInfo">
    ///     Metadata about the class containing the property, including naming policies and serialization options.
    /// </param>
    private static void GeneratePropertyDeserialization(CodeBuilder code, PropertyInfo prop, ClassInfo classInfo)
    {
        var propName = prop.Name;
        var serializedName = GetSerializedPropertyName(prop, classInfo);

        // Add code to check if the property exists in the serialized object
        code.AppendLine($"// Deserialize {propName}");
        code.AppendLine($"if (obj.Properties.TryGetValue(\"{serializedName}\", out var {propName}Value))");
        code.BeginBlock("");

        // Handle reference types with null checks
        if (prop.Type.IsReferenceType)
        {
            code.AppendLine($"if ({propName}Value is not global::ToonNet.Core.Models.ToonNull)");
            code.BeginBlock("");
            GeneratePropertyValueDeserialization(code, prop, propName);
            code.EndBlock();
        }
        else
        {
            // Directly generate deserialization logic for value types
            GeneratePropertyValueDeserialization(code, prop, propName);
        }

        code.EndBlock();
        code.AppendLine();
    }

    /// <summary>
    ///     Generates the actual property deserialization logic for a given property.
    /// </summary>
    /// <param name="code">
    ///     The <see cref="CodeBuilder"/> instance used to append the generated code.
    /// </param>
    /// <param name="prop">
    ///     Metadata about the property being deserialized, including its name, type, and serialization details.
    /// </param>
    /// <param name="propName">
    ///     The name of the property being deserialized, used as a variable name in the generated code.
    /// </param>
    private static void GeneratePropertyValueDeserialization(CodeBuilder code, PropertyInfo prop, string propName)
    {
        var typeName = prop.Type.ToDisplayString();

        // Check for the custom converter first
        if (prop.HasCustomConverter)
        {
            var converterName = prop.CustomConverter!.ToDisplayString();
            code.AppendLine($"var {propName}Converter = new {converterName}();");
            code.AppendLine($"result.{prop.Name} = {propName}Converter.Read({propName}Value, options);");
            return;
        }

        // Check for nested [ToonSerializable] class
        if (prop.IsNestedSerializable)
        {
            var nestedTypeName = GetTypeNameForNested(prop.Type);
            code.AppendLine($"var {propName}Doc = new global::ToonNet.Core.Models.ToonDocument({propName}Value);");
            code.AppendLine($"result.{prop.Name} = {nestedTypeName}.Deserialize({propName}Doc, options);");
            return;
        }

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
    ///     Generates deserialization logic for simple types such as string, int, double, bool, etc.
    /// </summary>
    /// <param name="code">
    ///  The <see cref="CodeBuilder"/> instance used to append the generated code.
    /// </param>
    /// <param name="prop">
    ///     Metadata about the property being deserialized, including its name, type, and serialization details.
    /// </param>
    /// <param name="propName">
    ///     The name of the property being deserialized, used as a variable name in the generated code.
    /// </param>
    private static void GenerateSimpleTypeDeserialization(CodeBuilder code, PropertyInfo prop, string propName)
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
            code.AppendLine(
                $"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num && int.TryParse({propName}Num.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture), global::System.Globalization.NumberStyles.Any, global::System.Globalization.CultureInfo.InvariantCulture, out var {propName}Int))");
            code.AppendLine($"    result.{prop.Name} = {propName}Int;");
        }
        else if (typeName is "long" or "System.Int64")
        {
            code.AppendLine(
                $"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num && long.TryParse({propName}Num.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture), global::System.Globalization.NumberStyles.Any, global::System.Globalization.CultureInfo.InvariantCulture, out var {propName}Long))");
            code.AppendLine($"    result.{prop.Name} = {propName}Long;");
        }
        else if (typeName is "double" or "System.Double")
        {
            code.AppendLine($"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num)");
            code.AppendLine($"    result.{prop.Name} = {propName}Num.Value;");
        }
        else if (typeName is "decimal" or "System.Decimal")
        {
            code.AppendLine(
                $"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num && decimal.TryParse({propName}Num.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture), global::System.Globalization.NumberStyles.Any, global::System.Globalization.CultureInfo.InvariantCulture, out var {propName}Decimal))");
            code.AppendLine($"    result.{prop.Name} = {propName}Decimal;");
        }
        else if (typeName is "float" or "System.Single")
        {
            code.AppendLine($"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num)");
            code.AppendLine($"    result.{prop.Name} = (float){propName}Num.Value;");
        }
        else if (typeName is "byte" or "System.Byte")
        {
            code.AppendLine(
                $"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num && byte.TryParse({propName}Num.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture), global::System.Globalization.NumberStyles.Any, global::System.Globalization.CultureInfo.InvariantCulture, out var {propName}Byte))");
            code.AppendLine($"    result.{prop.Name} = {propName}Byte;");
        }
        else if (typeName is "short" or "System.Int16")
        {
            code.AppendLine(
                $"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num && short.TryParse({propName}Num.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture), global::System.Globalization.NumberStyles.Any, global::System.Globalization.CultureInfo.InvariantCulture, out var {propName}Short))");
            code.AppendLine($"    result.{prop.Name} = {propName}Short;");
        }
        else if (typeName is "uint" or "System.UInt32")
        {
            code.AppendLine(
                $"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num && uint.TryParse({propName}Num.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture), global::System.Globalization.NumberStyles.Any, global::System.Globalization.CultureInfo.InvariantCulture, out var {propName}UInt))");
            code.AppendLine($"    result.{prop.Name} = {propName}UInt;");
        }
        else if (typeName is "ulong" or "System.UInt64")
        {
            code.AppendLine(
                $"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num && ulong.TryParse({propName}Num.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture), global::System.Globalization.NumberStyles.Any, global::System.Globalization.CultureInfo.InvariantCulture, out var {propName}ULong))");
            code.AppendLine($"    result.{prop.Name} = {propName}ULong;");
        }
        else
        {
            // Fallback for other simple types
            code.AppendLine($"result.{prop.Name} = ({typeName}){propName}Value;");
        }
    }

    /// <summary>
    ///     Generates deserialization logic for collection types such as arrays, lists, or IEnumerable.
    /// </summary>
    /// <param name="code">
    ///     The <see cref="CodeBuilder"/> instance used to append the generated code.
    /// </param>
    /// <param name="prop">
    ///     Metadata about the property being deserialized, including its name, type, and serialization details.
    /// </param>
    /// <param name="propName">
    ///     The name of the property being deserialized, used as a variable name in the generated code.
    /// </param>
    private static void GenerateCollectionDeserialization(CodeBuilder code, PropertyInfo prop, string propName)
    {
        code.AppendLine($"var arrayString = {propName}Value.ToString();");

        code.AppendLine(
            $"result.{prop.Name} = global::ToonNet.Core.Serialization.ToonSerializer.Deserialize<{prop.Type.ToDisplayString()}>(arrayString);");
    }

    /// <summary>
    ///     Generates deserialization logic for nullable types (e.g., T?).
    /// </summary>
    /// <param name="code">
    ///     The <see cref="CodeBuilder"/> instance used to append the generated code.
    /// </param>
    /// <param name="prop">
    ///     Metadata about the property being deserialized, including its name, type, and serialization details.
    /// </param>
    /// <param name="propName">
    ///     The name of the property being deserialized, used as a variable name in the generated code.
    /// </param>
    private static void GenerateNullableTypeDeserialization(CodeBuilder code, PropertyInfo prop, string propName)
    {
        var underlyingType = ((INamedTypeSymbol)prop.Type).TypeArguments[0].ToDisplayString();

        code.AppendLine($"if ({propName}Value is not global::ToonNet.Core.Models.ToonNull)");
        code.BeginBlock("");

        if (IsNumericType(underlyingType))
        {
            code.AppendLine(
                $"if ({propName}Value is global::ToonNet.Core.Models.ToonNumber {propName}Num && {underlyingType}.TryParse({propName}Num.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture), global::System.Globalization.NumberStyles.Any, global::System.Globalization.CultureInfo.InvariantCulture, out var {propName}Parsed))");
            code.AppendLine($"    result.{prop.Name} = {propName}Parsed;");
        }
        else
        {
            code.AppendLine($"result.{prop.Name} = ({underlyingType}){propName}Value;");
        }

        code.EndBlock();
    }

    /// <summary>
    ///     Gets the serialized property name for a given property, respecting naming policy and custom names.
    /// </summary>
    /// <param name="prop">
    ///     Metadata about the property for which the serialized name is being determined.
    ///     Includes details such as the property's name, type, and any custom serialization attributes.
    /// </param>
    /// <param name="classInfo">
    ///     Metadata about the class containing the property, including serialization options and naming policies.
    /// </param>
    /// <returns>
    ///     The serialized property name as a string, either the custom name (if specified) or the name transformed
    ///     according to the class's naming policy.
    /// </returns>
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
    ///     Extracts the naming policy from the [ToonSerializable] attribute, if present.
    /// </summary>
    /// <param name="attribute">
    ///     The attribute data representing the [ToonSerializable] attribute applied to the class.
    ///     May be null if the attribute is not present.
    /// </param>
    /// <returns>
    ///     The naming policy specified in the attribute, or the default naming policy if none is specified.
    /// </returns>
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
    ///     Applies a naming policy to a given property name.
    /// </summary>
    /// <param name="name">
    ///     The original property name to which the naming policy will be applied.
    /// </param>
    /// <param name="policy">
    ///     The naming policy to apply, such as CamelCase, SnakeCase, or LowerCase.
    /// </param>
    /// <returns>
    ///     The transformed property name as a string, based on the specified naming policy.
    /// </returns>
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
    ///     Converts a given string to camelCase.
    /// </summary>
    /// <param name="name">
    ///     The string to convert to camelCase.
    /// </param>
    /// <returns>
    ///     The camelCase version of the input string.
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
    ///     Converts a given string to snake_case.
    /// </summary>
    /// <param name="name">
    ///     The string to convert to snake_case.
    /// </param>
    /// <returns>
    ///     The snake_case version of the input string.
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
    ///     Determines whether a given type is a simple or primitive type.
    /// </summary>
    /// <param name="type">
    ///     The type to check, represented as an <see cref="ITypeSymbol"/>.
    /// </param>
    /// <returns>
    ///     True if the type is a simple or primitive type (e.g., string, int, bool); otherwise, false.
    /// </returns>
    private static bool IsSimpleType(ITypeSymbol type)
    {
        var name = type.ToDisplayString();

        return name is "string" or "int" or "long" or "double" or "decimal" or "bool" or "float" or "byte" or "short" or "uint" or "ulong"
                       or "System.String" or "System.Int32" or "System.Int64" or "System.Double" or "System.Decimal" or "System.Boolean"
                       or "System.Single" or "System.Byte" or "System.Int16" or "System.UInt32" or "System.UInt64";
    }

    /// <summary>
    ///     Determines whether a given type name represents a numeric type.
    /// </summary>
    /// <param name="typeName">
    ///     The name of the type to check, as a string.
    /// </param>
    /// <returns>
    ///     True if the type name represents a numeric type (e.g., int, double, decimal); otherwise, false.
    /// </returns>
    private static bool IsNumericType(string typeName)
    {
        return typeName is "int" or "long" or "double" or "decimal" or "float" or "byte" or "short" or "uint" or "ulong" or "System.Int32"
                           or "System.Int64" or "System.Double" or "System.Decimal" or "System.Single" or "System.Byte" or "System.Int16"
                           or "System.UInt32" or "System.UInt64";
    }

    /// <summary>
    ///     Determines whether a given type is a collection type (e.g., array, List, IEnumerable).
    /// </summary>
    /// <param name="type">
    ///     The type to check, represented as an <see cref="ITypeSymbol"/>.
    /// </param>
    /// <returns>
    ///     True if the type is a collection type; otherwise, false.
    /// </returns>
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
        if (type is INamedTypeSymbol namedType)
        {
            foreach (var iface in namedType.AllInterfaces)
            {
                if (iface.Name is "IEnumerable" or "ICollection" or "IList")
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    ///     Determines whether a given type is nullable (e.g., T?).
    /// </summary>
    /// <param name="type">
    ///     The type to check, represented as an <see cref="ITypeSymbol"/>.
    /// </param>
    /// <returns>
    ///     True if the type is nullable; otherwise, false.
    /// </returns>
    private static bool IsNullableType(ITypeSymbol type)
    {
        return type is { IsValueType: true, NullableAnnotation: NullableAnnotation.Annotated };
    }

    /// <summary>
    ///     Gets the simple type name for a nested [ToonSerializable] class.
    /// </summary>
    /// <param name="type">
    ///     The type to retrieve the name for, represented as an <see cref="ITypeSymbol"/>.
    /// </param>
    /// <returns>
    ///     The simple type name of the nested class, or the full type name if the type is not named.
    /// </returns>
    private static string GetTypeNameForNested(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType)
        {
            return namedType.Name;
        }

        return type.ToDisplayString();
    }
}