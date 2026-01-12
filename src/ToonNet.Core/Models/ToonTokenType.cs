namespace ToonNet.Core.Models;

/// <summary>
///     Represents the type of token in TOON format.
/// </summary>
/// <remarks>
///     This is an internal implementation detail used by the lexer and parser.
///     Users should interact with parsed TOON documents through <see cref="ToonDocument" /> and <see cref="ToonValue" />.
/// </remarks>
internal enum ToonTokenType
{
    /// <summary>
    ///     A field name or key (e.g., "name" in "name: value").
    /// </summary>
    Key,

    /// <summary>
    ///     A colon separator (:).
    /// </summary>
    Colon,

    /// <summary>
    ///     A simple value (string, number, boolean, null).
    /// </summary>
    Value,

    /// <summary>
    ///     A quoted string value.
    /// </summary>
    QuotedString,

    /// <summary>
    ///     Array length indicator (e.g., "[3]" in "tags[3]:").
    /// </summary>
    ArrayLength,

    /// <summary>
    ///     Array field definition (e.g., "{id,name,role}" in "users[2]{id,name,role}:").
    /// </summary>
    ArrayFields,

    /// <summary>
    ///     Comma separator in arrays or field definitions.
    /// </summary>
    Comma,

    /// <summary>
    ///     Indentation (spaces) indicating nesting level.
    /// </summary>
    Indent,

    /// <summary>
    ///     Newline character.
    /// </summary>
    Newline,

    /// <summary>
    ///     List item indicator (hyphen followed by space).
    /// </summary>
    ListItem,

    /// <summary>
    ///     End of input.
    /// </summary>
    EndOfInput
}

/// <summary>
///     Bitmask categories for fast token type checking.
/// </summary>
/// <remarks>
///     Using bitmasks allows for O(1) category checks with better branch prediction.
/// </remarks>
[Flags]
internal enum ToonTokenCategory
{
    None = 0,
    
    /// <summary>
    ///     Tokens that can start a value: Key, Value, QuotedString, ListItem
    /// </summary>
    ValueStart = 1 << 0,
    
    /// <summary>
    ///     Tokens that represent actual values: Value, QuotedString
    /// </summary>
    ActualValue = 1 << 1,
    
    /// <summary>
    ///     Structural tokens: Colon, Comma, Newline, Indent
    /// </summary>
    Structural = 1 << 2,
    
    /// <summary>
    ///     Array-related tokens: ArrayLength, ArrayFields
    /// </summary>
    ArrayRelated = 1 << 3,
    
    /// <summary>
    ///     Terminating tokens: Newline, EndOfInput
    /// </summary>
    Terminating = 1 << 4,
    
    /// <summary>
    ///     Whitespace tokens: Indent, Newline
    /// </summary>
    Whitespace = 1 << 5
}

/// <summary>
///     Extension methods for fast token type checking using bitmasks.
/// </summary>
internal static class ToonTokenTypeExtensions
{
    // Pre-computed bitmask lookup table for O(1) category checks
    private static readonly ToonTokenCategory[] CategoryLookup = new ToonTokenCategory[12];

    static ToonTokenTypeExtensions()
    {
        // Key
        CategoryLookup[(int)ToonTokenType.Key] = ToonTokenCategory.ValueStart;
        
        // Colon
        CategoryLookup[(int)ToonTokenType.Colon] = ToonTokenCategory.Structural;
        
        // Value
        CategoryLookup[(int)ToonTokenType.Value] = ToonTokenCategory.ValueStart | ToonTokenCategory.ActualValue;
        
        // QuotedString
        CategoryLookup[(int)ToonTokenType.QuotedString] = ToonTokenCategory.ValueStart | ToonTokenCategory.ActualValue;
        
        // ArrayLength
        CategoryLookup[(int)ToonTokenType.ArrayLength] = ToonTokenCategory.ArrayRelated;
        
        // ArrayFields
        CategoryLookup[(int)ToonTokenType.ArrayFields] = ToonTokenCategory.ArrayRelated;
        
        // Comma
        CategoryLookup[(int)ToonTokenType.Comma] = ToonTokenCategory.Structural;
        
        // Indent
        CategoryLookup[(int)ToonTokenType.Indent] = ToonTokenCategory.Structural | ToonTokenCategory.Whitespace;
        
        // Newline
        CategoryLookup[(int)ToonTokenType.Newline] = ToonTokenCategory.Structural | ToonTokenCategory.Terminating | ToonTokenCategory.Whitespace;
        
        // ListItem
        CategoryLookup[(int)ToonTokenType.ListItem] = ToonTokenCategory.ValueStart;
        
        // EndOfInput
        CategoryLookup[(int)ToonTokenType.EndOfInput] = ToonTokenCategory.Terminating;
    }

    /// <summary>
    ///     Checks if a token type belongs to the specified category using bitmask operations.
    /// </summary>
    /// <param name="type">The token type to check.</param>
    /// <param name="category">The category to test for.</param>
    /// <returns>True if the token belongs to the category; otherwise, false.</returns>
    private static bool Is(this ToonTokenType type, ToonTokenCategory category)
    {
        return (CategoryLookup[(int)type] & category) != 0;
    }

    /// <summary>
    ///     Checks if a token can start a value (Key, Value, QuotedString, ListItem).
    /// </summary>
    public static bool CanStartValue(this ToonTokenType type)
    {
        return type.Is(ToonTokenCategory.ValueStart);
    }

    /// <summary>
    ///     Checks if a token is an actual value (Value, QuotedString).
    /// </summary>
    public static bool IsActualValue(this ToonTokenType type)
    {
        return type.Is(ToonTokenCategory.ActualValue);
    }

    /// <summary>
    ///     Checks if a token is structural (Colon, Comma, Newline, Indent).
    /// </summary>
    public static bool IsStructural(this ToonTokenType type)
    {
        return type.Is(ToonTokenCategory.Structural);
    }

    /// <summary>
    ///     Checks if a token is array-related (ArrayLength, ArrayFields).
    /// </summary>
    public static bool IsArrayRelated(this ToonTokenType type)
    {
        return type.Is(ToonTokenCategory.ArrayRelated);
    }

    /// <summary>
    ///     Checks if a token is terminating (Newline, EndOfInput).
    /// </summary>
    public static bool IsTerminating(this ToonTokenType type)
    {
        return type.Is(ToonTokenCategory.Terminating);
    }
}