namespace ToonNet.Core.Models;

/// <summary>
/// Represents a complete TOON document.
/// </summary>
public sealed class ToonDocument(ToonValue root)
{
    /// <summary>
    /// Gets the root value of the document.
    /// </summary>
    public ToonValue Root { get; } = root;

    /// <summary>
    /// Creates a new ToonDocument with an empty object as root.
    /// </summary>
    public ToonDocument() : this(new ToonObject()) { }
    
    /// <summary>
    /// Attempts to treat the document root as an object.
    /// </summary>
    /// <returns>The root cast to ToonObject.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the root is not a ToonObject instance.
    /// </exception>
    public ToonObject AsObject()
    {
        if (Root is ToonObject obj)
        {
            return obj;
        }
        
        throw new InvalidOperationException("Root is not an object");
    }
    
    /// <summary>
    /// Attempts to treat the document root as an array.
    /// </summary>
    /// <returns>The root cast to ToonArray.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the root is not a ToonArray instance.
    /// </exception>
    public ToonArray AsArray()
    {
        if (Root is ToonArray arr)
        {
            return arr;
        }
        
        throw new InvalidOperationException("Root is not an array");
    }
}
