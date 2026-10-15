using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ToonNet.AspNetCore.Mvc.Http;

/// <summary>
///     Collects the items of an <see cref="IAsyncEnumerable{T}"/> response value into a list: a TOON array header needs
///     the number of items, and the serializer does not enumerate asynchronous sequences.
/// </summary>
internal static class AsyncEnumerableBuffer
{
    private static readonly MethodInfo CollectMethod =
        typeof(AsyncEnumerableBuffer).GetMethod(nameof(CollectAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    ///     Returns the value to write and its type: the items in a <see cref="List{T}"/> for an asynchronous sequence, the
    ///     value itself otherwise.
    /// </summary>
    [RequiresUnreferencedCode(AotMessages.Reflection)]
    [RequiresDynamicCode(AotMessages.Reflection)]
    public static async ValueTask<(object? Value, Type Type)> CollectIfAsyncAsync(object? value, Type type, CancellationToken cancellationToken)
    {
        var sequence = value?.GetType().GetInterfaces()
                            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));

        if (sequence == null)
        {
            return (value, type);
        }

        var itemType = sequence.GetGenericArguments()[0];
        var list = await ((Task<object>)CollectMethod.MakeGenericMethod(itemType).Invoke(null, [value, cancellationToken])!).ConfigureAwait(false);
        return (list, list.GetType());
    }

    private static async Task<object> CollectAsync<T>(IAsyncEnumerable<T> items, CancellationToken cancellationToken)
    {
        var list = new List<T>();

        await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            list.Add(item);
        }

        return list;
    }
}
