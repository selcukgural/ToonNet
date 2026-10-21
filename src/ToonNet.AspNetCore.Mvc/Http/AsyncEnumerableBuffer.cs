using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ToonNet.AspNetCore.Mvc.Http;

/// <summary>
///     Collects the items of an <see cref="IAsyncEnumerable{T}"/> response value into a list: a TOON array header needs
///     the number of items, and the serializer does not enumerate asynchronous sequences.
/// </summary>
internal static class AsyncEnumerableBuffer
{
    /// <summary>The items collected when the application has no <c>MvcOptions.MaxIAsyncEnumerableBufferLimit</c>: its default.</summary>
    public const int DefaultLimit = 8192;

    private static readonly MethodInfo CollectMethod =
        typeof(AsyncEnumerableBuffer).GetMethod(nameof(CollectAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    ///     Returns the value to write and its type: the items in a <see cref="List{T}"/> for an asynchronous sequence (of
    ///     the item type <paramref name="type"/> declares, when it is one), the value itself otherwise.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="type">The type to write the value as.</param>
    /// <param name="limit">The most items to collect, as MVC limits the sequences it buffers.</param>
    /// <param name="cancellationToken">The token of the request.</param>
    /// <exception cref="InvalidOperationException">Thrown when the sequence has more than <paramref name="limit"/> items.</exception>
    [RequiresUnreferencedCode(AotMessages.Reflection)]
    [RequiresDynamicCode(AotMessages.Reflection)]
    public static async ValueTask<(object? Value, Type Type)> CollectIfAsyncAsync(object? value, Type type, int limit, CancellationToken cancellationToken)
    {
        var sequence = value == null ? null
            : IsAsyncEnumerable(type) ? type
            : value.GetType().GetInterfaces().FirstOrDefault(IsAsyncEnumerable);

        if (sequence == null)
        {
            return (value, type);
        }

        var itemType = sequence.GetGenericArguments()[0];
        var list = await ((Task<object>)CollectMethod.MakeGenericMethod(itemType).Invoke(null, [value, limit, cancellationToken])!).ConfigureAwait(false);
        return (list, list.GetType());

        static bool IsAsyncEnumerable(Type t) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>);
    }

    private static async Task<object> CollectAsync<T>(IAsyncEnumerable<T> items, int limit, CancellationToken cancellationToken)
    {
        var list = new List<T>();

        await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            // A long or endless sequence (a feed, paging through a table) would otherwise fill the memory
            if (list.Count == limit)
            {
                throw new InvalidOperationException(
                    $"The response has more than {limit} items of IAsyncEnumerable<{typeof(T).Name}>, the most that are collected to write its array " +
                    "(MvcOptions.MaxIAsyncEnumerableBufferLimit). Return fewer items, or a list of them.");
            }

            list.Add(item);
        }

        return list;
    }
}
