using System.Collections.Concurrent;
using System.Diagnostics;
using ToonNet.Core;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.AsyncApi;

/// <summary>
/// Tests for ConfigureAwait(false) usage to prevent deadlock scenarios.
/// </summary>
/// <remarks>
/// These tests verify that async methods properly use ConfigureAwait(false)
/// to avoid capturing the SynchronizationContext, which can cause deadlocks
/// in non-ASP.NET Core environments (WPF, WinForms, legacy ASP.NET).
/// </remarks>
public sealed class ConfigureAwaitTests
{
    private readonly ToonSerializerOptions _options = new()
    {
        ToonOptions = new ToonOptions { IndentSize = 2 }
    };

    /// <summary>
    /// Test data class for serialization tests.
    /// </summary>
    private class TestPerson
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    [Fact]
    public async Task SerializeAsync_WithCustomSynchronizationContext_DoesNotDeadlock()
    {
        // Arrange
        var person = new TestPerson
        {
            Name = "Alice",
            Age = 30,
            Email = "alice@test.com"
        };

        var originalContext = SynchronizationContext.Current;
        var customContext = new SingleThreadedSynchronizationContext();

        try
        {
            SynchronizationContext.SetSynchronizationContext(customContext);

            // Act - This would deadlock without ConfigureAwait(false)
            var task = Task.Run(async () =>
            {
                return await ToonSerializer.SerializeAsync(person, _options);
            });

            var result = await task.ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Name: Alice", result);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [Fact]
    public async Task DeserializeAsync_WithCustomSynchronizationContext_DoesNotDeadlock()
    {
        // Arrange
        var toon = """
                  Name: Bob
                  Age: 25
                  Email: bob@test.com
                  """;

        var originalContext = SynchronizationContext.Current;
        var customContext = new SingleThreadedSynchronizationContext();

        try
        {
            SynchronizationContext.SetSynchronizationContext(customContext);

            // Act - This would deadlock without ConfigureAwait(false)
            var task = Task.Run(async () =>
            {
                return await ToonSerializer.DeserializeAsync<TestPerson>(toon, _options);
            });

            var result = await task.ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Bob", result.Name);
            Assert.Equal(25, result.Age);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [Fact]
    public async Task SerializeToFileAsync_WithCustomSynchronizationContext_DoesNotDeadlock()
    {
        // Arrange
        var person = new TestPerson
        {
            Name = "Charlie",
            Age = 35,
            Email = "charlie@test.com"
        };

        var tempFile = Path.GetTempFileName();
        var originalContext = SynchronizationContext.Current;
        var customContext = new SingleThreadedSynchronizationContext();

        try
        {
            SynchronizationContext.SetSynchronizationContext(customContext);

            // Act - This would deadlock without ConfigureAwait(false)
            var task = Task.Run(async () =>
            {
                await ToonSerializer.SerializeToFileAsync(person, tempFile, _options);
            });

            await task.ConfigureAwait(false);

            // Assert
            var content = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("Name: Charlie", content);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task DeserializeFromFileAsync_WithCustomSynchronizationContext_DoesNotDeadlock()
    {
        // Arrange
        var toon = """
                  Name: Diana
                  Age: 28
                  Email: diana@test.com
                  """;

        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, toon);

        var originalContext = SynchronizationContext.Current;
        var customContext = new SingleThreadedSynchronizationContext();

        try
        {
            SynchronizationContext.SetSynchronizationContext(customContext);

            // Act - This would deadlock without ConfigureAwait(false)
            var task = Task.Run(async () =>
            {
                return await ToonSerializer.DeserializeFromFileAsync<TestPerson>(tempFile, _options);
            });

            var result = await task.ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Diana", result.Name);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task SerializeToStreamAsync_WithCustomSynchronizationContext_DoesNotDeadlock()
    {
        // Arrange
        var person = new TestPerson
        {
            Name = "Eve",
            Age = 32,
            Email = "eve@test.com"
        };

        var originalContext = SynchronizationContext.Current;
        var customContext = new SingleThreadedSynchronizationContext();

        try
        {
            SynchronizationContext.SetSynchronizationContext(customContext);

            // Act - This would deadlock without ConfigureAwait(false)
            var task = Task.Run(async () =>
            {
                using var stream = new MemoryStream();
                await ToonSerializer.SerializeToStreamAsync(person, stream, _options);
                stream.Position = 0;
                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync();
            });

            var result = await task.ConfigureAwait(false);

            // Assert
            Assert.Contains("Name: Eve", result);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [Fact]
    public async Task DeserializeFromStreamAsync_WithCustomSynchronizationContext_DoesNotDeadlock()
    {
        // Arrange
        var toon = """
                  Name: Frank
                  Age: 40
                  Email: frank@test.com
                  """;

        var originalContext = SynchronizationContext.Current;
        var customContext = new SingleThreadedSynchronizationContext();

        try
        {
            SynchronizationContext.SetSynchronizationContext(customContext);

            // Act - This would deadlock without ConfigureAwait(false)
            var task = Task.Run(async () =>
            {
                using var stream = new MemoryStream();
                await using var writer = new StreamWriter(stream, leaveOpen: true);
                await writer.WriteAsync(toon);
                await writer.FlushAsync();
                stream.Position = 0;

                return await ToonSerializer.DeserializeFromStreamAsync<TestPerson>(stream, _options);
            });

            var result = await task.ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Frank", result.Name);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    /// <summary>
    /// Custom SynchronizationContext that simulates WPF/WinForms behavior.
    /// </summary>
    /// <remarks>
    /// This context posts all continuations back to a single thread,
    /// which can cause deadlocks if ConfigureAwait(false) is not used properly.
    /// </remarks>
    private sealed class SingleThreadedSynchronizationContext : SynchronizationContext
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();
        private readonly Thread _thread;

        public SingleThreadedSynchronizationContext()
        {
            _thread = new Thread(RunOnCurrentThread)
            {
                Name = "SingleThreadedSynchronizationContext",
                IsBackground = true
            };
            _thread.Start();
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            if (_queue.IsAddingCompleted)
            {
                return;
            }

            _queue.Add((d, state));
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (Thread.CurrentThread == _thread)
            {
                d(state);
            }
            else
            {
                var completed = new ManualResetEventSlim(false);
                Exception? exception = null;

                Post(_ =>
                {
                    try
                    {
                        d(state);
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                    finally
                    {
                        completed.Set();
                    }
                }, null);

                completed.Wait();

                if (exception != null)
                {
                    throw exception;
                }
            }
        }

        private void RunOnCurrentThread()
        {
            SynchronizationContext.SetSynchronizationContext(this);

            foreach (var (callback, state) in _queue.GetConsumingEnumerable())
            {
                callback(state);
            }
        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }
    }
}
