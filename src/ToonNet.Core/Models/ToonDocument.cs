namespace ToonNet.Core.Models;

/// <summary>
///     Represents a complete TOON document.
/// </summary>
/// <remarks>
///     This class encapsulates the root value of a TOON document, which can be treated as either
///     an object or an array depending on its type. It provides methods to safely cast the root
///     to the expected type, throwing an exception if the type does not match.
/// </remarks>
public sealed class ToonDocument(ToonValue root)
{
    /// <summary>
    ///     Creates a new ToonDocument with an empty object as a root.
    /// </summary>
    /// <remarks>
    ///     This constructor initializes the document with a default root value of type <see cref="ToonObject"/>.
    /// </remarks>
    public ToonDocument() : this(new ToonObject()) { }

    /// <summary>
    ///     Gets the root value of the document.
    /// </summary>
    /// <value>
    ///     The root value of the document, which can be any type derived from <see cref="ToonValue"/>.
    /// </value>
    public ToonValue Root { get; } = root ?? throw new ArgumentNullException(nameof(root));

    /// <summary>
    ///     Attempts to treat the document root as an object.
    /// </summary>
    /// <returns>
    ///     The root value cast to <see cref="ToonObject"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the root value is not an instance of <see cref="ToonObject"/>.
    /// </exception>
    /// <remarks>
    ///     Use this method when you expect the root value to be an object. If the root is not
    ///     of the expected type, an exception is thrown to indicate the mismatch.
    /// </remarks>
    public ToonObject AsObject()
    {
        if (Root is ToonObject obj)
        {
            return obj;
        }

        throw new InvalidOperationException("Root is not an object");
    }

    /// <summary>
    ///     Attempts to treat the document root as an array.
    /// </summary>
    /// <returns>
    ///     The root value cast to <see cref="ToonArray"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the root value is not an instance of <see cref="ToonArray"/>.
    /// </exception>
    /// <remarks>
    ///     Use this method when you expect the root value to be an array. If the root is not
    ///     of the expected type, an exception is thrown to indicate the mismatch.
    /// </remarks>
    public ToonArray AsArray()
    {
        if (Root is ToonArray arr)
        {
            return arr;
        }

        throw new InvalidOperationException("Root is not an array");
    }
}