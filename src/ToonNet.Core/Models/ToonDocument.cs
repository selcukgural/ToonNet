namespace ToonNet.Core.Models;

/// <summary>
/// Represents a complete TOON document.
/// </summary>
public sealed class ToonDocument(ToonValue root)
{
    public ToonValue Root { get; } = root;

    public ToonDocument() : this(new ToonObject()) { }
    
    public ToonObject AsObject()
    {
        if (Root is ToonObject obj)
        {
            return obj;
        }
        
        throw new InvalidOperationException("Root is not an object");
    }
    
    public ToonArray AsArray()
    {
        if (Root is ToonArray arr)
        {
            return arr;
        }
        
        throw new InvalidOperationException("Root is not an array");
    }
}
