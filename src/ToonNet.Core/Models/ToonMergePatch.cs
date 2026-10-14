using System.Runtime.CompilerServices;

namespace ToonNet.Core.Models;

/// <summary>
///     JSON Merge Patch (RFC 7386) for <see cref="ToonValue"/> trees: a patch is a document that looks like the target,
///     with the members to change, <c>null</c> for the members to remove, and the rest left out.
/// </summary>
/// <example>
///     <code>
///     var patch = ToonDocument.Parse("meta:\n  region: us\n  draft: null").Root;
///     root = ToonMergePatch.Apply(root, patch);   // sets meta.region, removes meta.draft
///     </code>
/// </example>
public static class ToonMergePatch
{
    /// <summary>
    ///     Applies a merge patch to a value, changing it in place where it is an object (call
    ///     <see cref="ToonValue.DeepClone"/> first to keep the original).
    /// </summary>
    /// <param name="target">The value to patch.</param>
    /// <param name="patch">The patch: an object merges member by member (<c>null</c> removes a member); anything else replaces the target.</param>
    /// <returns>
    ///     The patched value: <paramref name="target"/> itself when both are objects, otherwise a new value (a copy of the
    ///     patch, so later changes to the patch do not reach the result).
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="InsufficientExecutionStackException">Thrown when the patch is nested too deeply.</exception>
    public static ToonValue Apply(ToonValue target, ToonValue patch)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(patch);
        return Merge(target, patch);
    }

    private static ToonValue Merge(ToonValue? target, ToonValue patch)
    {
        if (patch is not ToonObject members)
        {
            return patch.DeepClone();
        }

        RuntimeHelpers.EnsureSufficientExecutionStack();

        if (target is not ToonObject result)
        {
            result = new ToonObject(members.Properties.Count);
        }

        // A snapshot: the patch may be the target itself, or inside it
        HashSet<string>? removed = null;

        foreach (var (key, value) in members.Properties.ToArray())
        {
            if (value is ToonNull)
            {
                (removed ??= new HashSet<string>(StringComparer.Ordinal)).Add(key);
            }
            else
            {
                result.Properties[key] = Merge(result[key], value);
            }
        }

        // At once, after the other members (whose keys differ): removing them one by one is quadratic in the size of the
        // target. The order of the remaining and added members is the same either way.
        if (removed != null)
        {
            result.Properties.RemoveKeys(removed);
        }

        return result;
    }

    /// <summary>
    ///     Creates the merge patch that turns <paramref name="original"/> into <paramref name="modified"/>: changed and
    ///     added members, <c>null</c> for removed ones, nested objects as nested patches.
    /// </summary>
    /// <param name="original">The value before the change.</param>
    /// <param name="modified">The value after the change.</param>
    /// <returns>The patch; an empty object when nothing changed (between objects).</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="InsufficientExecutionStackException">Thrown when the values are nested too deeply.</exception>
    /// <remarks>
    ///     A merge patch cannot set a member to <c>null</c> (RFC 7386 §1): a member that is <c>null</c> in
    ///     <paramref name="modified"/> but not in <paramref name="original"/> is in the patch as <c>null</c>, which removes it.
    ///     Arrays are replaced as a whole.
    /// </remarks>
    public static ToonValue Create(ToonValue original, ToonValue modified)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(modified);

        if (original is not ToonObject before || modified is not ToonObject after)
        {
            return modified.DeepClone();
        }

        RuntimeHelpers.EnsureSufficientExecutionStack();
        var patch = new ToonObject();

        foreach (var (key, _) in before.Properties)
        {
            if (!after.Properties.ContainsKey(key))
            {
                patch.Properties.Add(key, ToonNull.Instance);
            }
        }

        foreach (var (key, value) in after.Properties)
        {
            if (!before.Properties.TryGetValue(key, out var old))
            {
                patch.Properties.Add(key, value.DeepClone());
            }
            else if (!ToonValue.DeepEquals(old, value))
            {
                patch.Properties.Add(key, old is ToonObject && value is ToonObject ? Create(old, value) : value.DeepClone());
            }
        }

        return patch;
    }
}
