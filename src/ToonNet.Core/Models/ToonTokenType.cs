namespace ToonNet.Core.Models;

/// <summary>
/// Represents the type of token in TOON format.
/// </summary>
public enum ToonTokenType
{
    /// <summary>
    /// A field name or key (e.g., "name" in "name: value")
    /// </summary>
    Key,
    
    /// <summary>
    /// A colon separator (:)
    /// </summary>
    Colon,
    
    /// <summary>
    /// A simple value (string, number, boolean, null)
    /// </summary>
    Value,
    
    /// <summary>
    /// Array length indicator (e.g., "[3]" in "tags[3]:")
    /// </summary>
    ArrayLength,
    
    /// <summary>
    /// Array field definition (e.g., "{id,name,role}" in "users[2]{id,name,role}:")
    /// </summary>
    ArrayFields,
    
    /// <summary>
    /// Comma separator in arrays or field definitions
    /// </summary>
    Comma,
    
    /// <summary>
    /// Indentation (spaces) indicating nesting level
    /// </summary>
    Indent,
    
    /// <summary>
    /// Newline character
    /// </summary>
    Newline,
    
    /// <summary>
    /// List item indicator (hyphen followed by space)
    /// </summary>
    ListItem,
    
    /// <summary>
    /// End of input
    /// </summary>
    EndOfInput
}
