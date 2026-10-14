using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.AsyncApi;

/// <summary>
///     The async methods must not resume on the caller's SynchronizationContext (ConfigureAwait(false)): in WPF,
///     WinForms or classic ASP.NET, a caller that blocks on the task would otherwise deadlock.
/// </summary>
/// <remarks>
///     Each method is started on a context that drops everything posted to it, with a stream that completes its reads and
///     writes asynchronously. An await without ConfigureAwait(false) posts its continuation to that context, which never
///     runs it, so the task does not complete and the test fails on its deadline instead of hanging.
/// </remarks>
public sealed class ConfigureAwaitTests
{
    private sealed class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    /// <summary>A context that never runs what is posted to it, like a UI thread blocked on the task.</summary>
    private sealed class BlockedContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
        }

        public override void Send(SendOrPostCallback d, object? state) => throw new InvalidOperationException();
    }

    /// <summary>A stream whose async reads and writes complete later, on the thread pool.</summary>
    private sealed class AsyncStream(byte[] data) : MemoryStream(data.Length)
    {
        private readonly MemoryStream _source = new(data);

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            return _source.Read(buffer.Span);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            base.Write(buffer.Span);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        }
    }

    private const string Toon = "Name: Ada\nAge: 36";
    private const string Rows = "[2]{Name,Age}:\n  Ada,36\n  Bob,40";

    private static AsyncStream Input(string text) => new(System.Text.Encoding.UTF8.GetBytes(text));

    /// <summary>Starts <paramref name="start"/> on the blocked context and waits for it with a deadline.</summary>
    private static void AssertCompletes(Func<Task> start)
    {
        var previous = SynchronizationContext.Current;
        Task task;

        try
        {
            SynchronizationContext.SetSynchronizationContext(new BlockedContext());
            task = start();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        Assert.True(task.Wait(TimeSpan.FromSeconds(10)), "the method resumed on the caller's context (missing ConfigureAwait(false))");
    }

    private static async Task<List<T?>> ReadAll<T>(IAsyncEnumerable<T?> items)
    {
        var list = new List<T?>();

        await foreach (var item in items.ConfigureAwait(false))
        {
            list.Add(item);
        }

        return list;
    }

    public static TheoryData<string> Methods() =>
    [
        "DeserializeAsync(Stream)", "DeserializeAsync(Stream, Type)", "DeserializeFromStreamAsync", "DeserializeFromStreamAsync(Type)",
        "DeserializeAtAsync", "DeserializeAllAsync", "DeserializeAsyncEnumerable", "DeserializeAsyncEnumerableAt", "DeserializeDocumentsAsync",
        "SerializeAsync(Stream)", "SerializeToStreamAsync", "SerializeDocumentsAsync", "ToonDocumentReader.CreateAsync", "ToonDocumentWriter.FlushAsync"
    ];

    [Theory]
    [MemberData(nameof(Methods))]
    public void AsyncMethods_DoNotResumeOnTheCallersContext(string method)
    {
        AssertCompletes(method switch
        {
            "DeserializeAsync(Stream)" => async () => Assert.Equal("Ada", ((await ToonSerializer.DeserializeAsync<Person>(Input(Toon)).ConfigureAwait(false)))!.Name),
            "DeserializeAsync(Stream, Type)" => async () => Assert.IsType<Person>((await ToonSerializer.DeserializeAsync(Input(Toon), typeof(Person)).ConfigureAwait(false))),
            "DeserializeFromStreamAsync" => async () => Assert.Equal(36, ((await ToonSerializer.DeserializeFromStreamAsync<Person>(Input(Toon)).ConfigureAwait(false)))!.Age),
            "DeserializeFromStreamAsync(Type)" => async () => Assert.IsType<Person>((await ToonSerializer.DeserializeFromStreamAsync(Input(Toon), typeof(Person)).ConfigureAwait(false))),
            "DeserializeAtAsync" => async () => Assert.Equal("Bob", ((await ToonSerializer.DeserializeAtAsync<Person>(Input(Rows), "$[1]").ConfigureAwait(false)))!.Name),
            "DeserializeAllAsync" => async () => Assert.Equal(2, ((await ToonSerializer.DeserializeAllAsync<Person>(Input(Rows), "$[*]").ConfigureAwait(false))).Count),
            "DeserializeAsyncEnumerable" => async () => Assert.Equal(2, ((await ReadAll(ToonSerializer.DeserializeAsyncEnumerable<Person>(Input(Rows))).ConfigureAwait(false))).Count),
            "DeserializeAsyncEnumerableAt" => async () =>
                Assert.Equal(2, ((await ReadAll(ToonSerializer.DeserializeAsyncEnumerableAt<Person>(Input("d:\n  r" + Rows.Replace("\n", "\n  ")), "$.d.r")).ConfigureAwait(false))).Count),
            "DeserializeDocumentsAsync" => async () =>
                Assert.Equal(2, ((await ReadAll(ToonSerializer.DeserializeDocumentsAsync<Person>(Input(Toon + "\n\n" + Toon))).ConfigureAwait(false))).Count),
            "SerializeAsync(Stream)" => () => ToonSerializer.SerializeAsync(Input(""), new Person { Name = "Ada" }).AsTask(),
            "SerializeToStreamAsync" => () => ToonSerializer.SerializeToStreamAsync(new Person { Name = "Ada" }, Input("")).AsTask(),
            "SerializeDocumentsAsync" => () => ToonSerializer.SerializeDocumentsAsync(new[] { new Person(), new Person() }, Input("")).AsTask(),
            "ToonDocumentReader.CreateAsync" => async () =>
            {
                using var reader = (await ToonDocumentReader.CreateAsync(Input(Toon)).ConfigureAwait(false));
                Assert.True(reader.Read());
            },
            _ => async () =>
            {
                var writer = new ToonDocumentWriter(Input(""));
                await using var _ = writer.ConfigureAwait(false);
                writer.WriteStartObject();
                writer.WriteString("a", "b");
                writer.WriteEndObject();
                await writer.FlushAsync().ConfigureAwait(false);
            }
        });
    }

    [Fact]
    public void DocumentsOverStreamReader_DoNotResumeOnTheCallersContext()
    {
        AssertCompletes(async () =>
        {
            using var reader = new StreamReader(Input(Toon + "\n\n" + Toon));
            Assert.Equal(2, ((await ReadAll(ToonSerializer.DeserializeDocumentsAsync<Person>(reader)).ConfigureAwait(false))).Count);
        });
    }
}
