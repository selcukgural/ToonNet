using ToonNet.Core.Serialization;
using ToonNet.Core;

namespace ToonNet.Tests.AsyncApi;

/// <summary>
///     Tests for async APIs in ToonSerializer.
/// </summary>
public sealed class ToonSerializerAsyncTests
{
    private readonly ToonSerializerOptions _options = new()
    {
        ToonOptions = new ToonOptions { IndentSize = 2 }
    };

    [Fact]
    public async Task SerializeAsync_SimpleObject_ReturnsExpectedToon()
    {
        // Arrange
        var user = new TestUser
        {
            Name = "Alice",
            Age = 30,
            Email = "alice@example.com"
        };

        // Act
        var result = await ToonSerializer.SerializeAsync(user, _options);

        // Assert
        Assert.Contains("Name: Alice", result);
        Assert.Contains("Age: 30", result);
        Assert.Contains("Email: alice@example.com", result);
    }

    [Fact]
    public async Task DeserializeAsync_ValidToon_ReturnsExpectedObject()
    {
        // Arrange
        var toon = """
                  Name: Bob
                  Age: 25
                  Email: bob@test.com
                  """;

        // Act
        var result = await ToonSerializer.DeserializeAsync<TestUser>(toon, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Bob", result.Name);
        Assert.Equal(25, result.Age);
        Assert.Equal("bob@test.com", result.Email);
    }

    [Fact]
    public async Task SerializeToFileAsync_ValidObject_WritesFileCorrectly()
    {
        // Arrange
        var user = new TestUser
        {
            Name = "Charlie",
            Age = 35,
            Email = "charlie@example.com"
        };
        var filePath = Path.GetTempFileName();

        try
        {
            // Act
            await ToonSerializer.SerializeToFileAsync(user, filePath, _options);

            // Assert
            Assert.True(File.Exists(filePath));
            var content = await File.ReadAllTextAsync(filePath);
            Assert.Contains("Name: Charlie", content);
            Assert.Contains("Age: 35", content);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task DeserializeFromFileAsync_ValidFile_ReturnsExpectedObject()
    {
        // Arrange
        var toon = """
                  Name: David
                  Age: 40
                  Email: david@test.com
                  """;
        var filePath = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(filePath, toon);

            // Act
            var result = await ToonSerializer.DeserializeFromFileAsync<TestUser>(filePath, _options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("David", result.Name);
            Assert.Equal(40, result.Age);
            Assert.Equal("david@test.com", result.Email);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task SerializeToStreamAsync_ValidObject_WritesStreamCorrectly()
    {
        // Arrange
        var user = new TestUser
        {
            Name = "Eve",
            Age = 28,
            Email = "eve@example.com"
        };

        using var stream = new MemoryStream();

        // Act
        await ToonSerializer.SerializeToStreamAsync(user, stream, _options);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        
        Assert.Contains("Name: Eve", content);
        Assert.Contains("Age: 28", content);
        Assert.Contains("Email: eve@example.com", content);
    }

    [Fact]
    public async Task DeserializeFromStreamAsync_ValidStream_ReturnsExpectedObject()
    {
        // Arrange
        var toon = """
                  Name: Frank
                  Age: 45
                  Email: frank@test.com
                  """;
        var bytes = System.Text.Encoding.UTF8.GetBytes(toon);
        using var stream = new MemoryStream(bytes);

        // Act
        var result = await ToonSerializer.DeserializeFromStreamAsync<TestUser>(stream, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Frank", result.Name);
        Assert.Equal(45, result.Age);
        Assert.Equal("frank@test.com", result.Email);
    }

    private static async IAsyncEnumerable<TestUser> Users(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await Task.Yield();
            yield return new TestUser { Name = $"U{i}", Age = i, Email = $"u{i}@test.com" };
        }
    }

    private static async Task<List<string?>> Names(IAsyncEnumerable<TestUser?> users)
    {
        var names = new List<string?>();

        await foreach (var user in users)
        {
            names.Add(user?.Name);
        }

        return names;
    }

    [Fact]
    public async Task DeserializeDocumentsAsync_FromAStream_LeavesItOpen()
    {
        foreach (var (write, read) in new[]
                 {
                     (ToonMultiDocumentWriteOptions.BlankLine, ToonMultiDocumentReadOptions.BlankLine),
                     (ToonMultiDocumentWriteOptions.ExplicitSeparator, ToonMultiDocumentReadOptions.ExplicitSeparator)
                 })
        {
            using var stream = new MemoryStream();
            await ToonSerializer.SerializeDocumentsAsync(Users(3), stream, _options, write);
            stream.Position = 0;

            Assert.Equal(["U0", "U1", "U2"], await Names(ToonSerializer.DeserializeDocumentsAsync<TestUser>(stream, _options, read)));
            Assert.True(stream.CanRead);
        }

        using var collection = new MemoryStream();
        await ToonSerializer.SerializeDocumentsAsync(new[] { new TestUser { Name = "A" }, new TestUser { Name = "B" } }, collection);
        collection.Position = 0;
        Assert.Equal(["A", "B"], await Names(ToonSerializer.DeserializeDocumentsAsync<TestUser>(collection)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeserializeDocumentsAsync_Errors_HaveTheLineOfTheStream(bool explicitSeparator)
    {
        // The second document starts at line 4 (blank line) or 5 (separator line and a blank line); its error is on its line 2
        var text = explicitSeparator ? "Name: A\nAge: 1\n---\n\nName: B\n Age: 2\n" : "Name: A\nAge: 1\n\nName: B\n Age: 2\n";
        var read = explicitSeparator ? ToonMultiDocumentReadOptions.ExplicitSeparator : ToonMultiDocumentReadOptions.BlankLine;
        var names = new List<string?>();

        var ex = await Assert.ThrowsAsync<ToonParseException>(async () =>
        {
            await foreach (var user in ToonSerializer.DeserializeDocumentsAsync<TestUser>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text)), _options, read))
            {
                names.Add(user!.Name);
            }
        });

        Assert.Equal(["A"], names);
        Assert.Equal(explicitSeparator ? 6 : 5, ex.Line);
        Assert.Equal(1, ex.Column);
        Assert.Equal($"Indentation of 1 spaces is not a multiple of the indent size 2 (line {ex.Line}, column 1)", ex.Message);
    }

#pragma warning disable CS0618 // the names before 3.2 still work
    [Fact]
    public async Task NamesBefore32_StillWork()
    {
        using var stream = new MemoryStream();
        await ToonSerializer.SerializeStreamAsync(Users(2), stream, _options);
        stream.Position = 0;

        using var reader = new StreamReader(stream, leaveOpen: true);
        Assert.Equal(["U0", "U1"], await Names(ToonSerializer.DeserializeStreamAsync<TestUser>(reader, _options)));

        using var collection = new MemoryStream();
        await ToonSerializer.SerializeCollectionToStreamAsync(new[] { new TestUser { Name = "A" } }, collection);
        Assert.Equal("Name: A", System.Text.Encoding.UTF8.GetString(collection.ToArray()).Split('\n')[0]);
    }
#pragma warning restore CS0618

    [Fact]
    public async Task DeserializeDocumentsAsync_MultipleObjects_YieldsAllObjects()
    {
        // Arrange
        var toonData = """
                      Name: User1
                      Age: 20
                      Email: user1@test.com

                      Name: User2
                      Age: 30
                      Email: user2@test.com

                      Name: User3
                      Age: 40
                      Email: user3@test.com
                      """;
        var filePath = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(filePath, toonData);

            // Act
            var users = new List<TestUser>();
            await foreach (var user in ToonSerializer.DeserializeDocumentsAsync<TestUser>(filePath, _options))
            {
                if (user != null)
                {
                    users.Add(user);
                }
            }

            // Assert
            Assert.Equal(3, users.Count);
            Assert.Equal("User1", users[0].Name);
            Assert.Equal("User2", users[1].Name);
            Assert.Equal("User3", users[2].Name);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task DeserializeDocumentsAsync_ExplicitSeparator_YieldsAllObjects()
    {
        // Arrange
        var toonData = """
                      Name: User1
                      Age: 20
                      Email: user1@test.com
                      ---
                      Name: User2
                      Age: 30
                      Email: user2@test.com
                      ---
                      Name: User3
                      Age: 40
                      Email: user3@test.com
                      """;
        var filePath = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(filePath, toonData);

            // Act
            var users = new List<TestUser>();
            await foreach (var user in ToonSerializer.DeserializeDocumentsAsync<TestUser>(filePath, _options, ToonMultiDocumentReadOptions.ExplicitSeparator))
            {
                if (user != null)
                {
                    users.Add(user);
                }
            }

            // Assert
            Assert.Equal(3, users.Count);
            Assert.Equal("User1", users[0].Name);
            Assert.Equal("User2", users[1].Name);
            Assert.Equal("User3", users[2].Name);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task SerializeCollectionToFileAsync_MultipleObjects_WritesCorrectFormat()
    {
        // Arrange
        var users = new[]
        {
            new TestUser { Name = "Alice", Age = 25, Email = "alice@test.com" },
            new TestUser { Name = "Bob", Age = 30, Email = "bob@test.com" },
            new TestUser { Name = "Charlie", Age = 35, Email = "charlie@test.com" }
        };
        var filePath = Path.GetTempFileName();

        try
        {
            // Act
            await ToonSerializer.SerializeCollectionToFileAsync(users, filePath, _options);

            // Assert
            var content = await File.ReadAllTextAsync(filePath);
            Assert.Contains("Name: Alice", content);
            Assert.Contains("Name: Bob", content);
            Assert.Contains("Name: Charlie", content);
            
            // Check that objects are separated by blank lines
            var lines = content.Split('\n');
            var blankLines = lines.Count(string.IsNullOrWhiteSpace);
            Assert.True(blankLines >= 2, $"Expected at least 2 blank lines, but found {blankLines}"); // At least 2 blank lines for 3 objects
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task SerializeDocumentsAsync_Collection_MultipleObjects_RoundTrips()
    {
        // Arrange
        var users = new[]
        {
            new TestUser { Name = "Alice", Age = 25, Email = "alice@test.com" },
            new TestUser { Name = "Bob", Age = 30, Email = "bob@test.com" },
            new TestUser { Name = "Charlie", Age = 35, Email = "charlie@test.com" }
        };
        using var stream = new MemoryStream();

        // Act
        await ToonSerializer.SerializeDocumentsAsync(users, stream, _options);

        // Assert
        var content = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal(2, content.Split("\n\n").Length - 1);

        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var readUsers = new List<TestUser?>();
        await foreach (var user in ToonSerializer.DeserializeDocumentsAsync<TestUser>(reader, _options))
        {
            readUsers.Add(user);
        }

        Assert.Equal(["Alice", "Bob", "Charlie"], readUsers.Select(u => u!.Name));
    }

    [Fact]
    public async Task AsyncOperationsWithCancellation_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Cancel immediately
        
        var user = new TestUser { Name = "Test", Age = 1, Email = "test@test.com" };

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ToonSerializer.SerializeAsync(user, _options, cts.Token).AsTask());
            
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ToonSerializer.DeserializeAsync<TestUser>("Name: Test", _options, cts.Token).AsTask());
    }

    [Fact]
    public async Task AsyncOperationsWithValidCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var user = new TestUser { Name = "Test", Age = 1, Email = "test@test.com" };

        // Act
        var serialized = await ToonSerializer.SerializeAsync(user, _options, cts.Token);
        var deserialized = await ToonSerializer.DeserializeAsync<TestUser>(serialized, _options, cts.Token);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("Test", deserialized.Name);
        Assert.Equal(1, deserialized.Age);
        Assert.Equal("test@test.com", deserialized.Email);
    }

    [Fact]
    public async Task DeserializeDocumentsAsync_EmptyFile_ReturnsNoItems()
    {
        // Arrange
        var filePath = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(filePath, string.Empty);

            // Act
            var items = new List<TestUser>();
            await foreach (var item in ToonSerializer.DeserializeDocumentsAsync<TestUser>(filePath, _options))
            {
                if (item != null)
                {
                    items.Add(item);
                }
            }

            // Assert
            Assert.Empty(items);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task SerializeDocumentsAsync_LargeDataset_WritesIncrementally()
    {
        // Arrange
        var filePath = Path.GetTempFileName();
        var itemCount = 10_000;

        try
        {
            // Act - Stream large dataset without loading all into memory
            await ToonSerializer.SerializeDocumentsAsync(
                GenerateUsersAsync(itemCount), 
                filePath, 
                _options
            );

            // Assert - Verify file was written
            Assert.True(File.Exists(filePath));
            var fileInfo = new FileInfo(filePath);
            Assert.True(fileInfo.Length > 0);

            // Verify roundtrip - read back and count
            var readCount = 0;
            await foreach (var user in ToonSerializer.DeserializeDocumentsAsync<TestUser>(filePath, _options))
            {
                Assert.NotNull(user);
                readCount++;
            }

            Assert.Equal(itemCount, readCount);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task SerializeDocumentsAsync_WithExplicitSeparator_WritesCorrectFormat()
    {
        // Arrange
        var filePath = Path.GetTempFileName();
        var users = GenerateUsersAsync(3);

        try
        {
            // Act - Use explicit separator mode
            await ToonSerializer.SerializeDocumentsAsync(
                users,
                filePath,
                _options,
                ToonMultiDocumentWriteOptions.ExplicitSeparator
            );

            // Assert - Verify separator format
            var content = await File.ReadAllTextAsync(filePath);
            var separatorCount = content.Split("---").Length - 1;
            Assert.Equal(2, separatorCount); // 3 items = 2 separators

            // Verify roundtrip with matching read options
            var readUsers = new List<TestUser>();
            await foreach (var user in ToonSerializer.DeserializeDocumentsAsync<TestUser>(
                filePath, 
                _options, 
                ToonMultiDocumentReadOptions.ExplicitSeparator))
            {
                if (user != null)
                {
                    readUsers.Add(user);
                }
            }

            Assert.Equal(3, readUsers.Count);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task SerializeDocumentsAsync_WithBlankLineSeparator_WritesCorrectFormat()
    {
        // Arrange
        var filePath = Path.GetTempFileName();
        var users = GenerateUsersAsync(5);

        try
        {
            // Act - Use blank line separator (default)
            await ToonSerializer.SerializeDocumentsAsync(
                users,
                filePath,
                _options,
                ToonMultiDocumentWriteOptions.BlankLine
            );

            // Assert - Verify blank line format
            var content = await File.ReadAllTextAsync(filePath);
            Assert.Contains("\n\n", content); // Should have blank line separators

            // Verify roundtrip
            var readUsers = new List<TestUser>();
            await foreach (var user in ToonSerializer.DeserializeDocumentsAsync<TestUser>(
                filePath, 
                _options, 
                ToonMultiDocumentReadOptions.BlankLine))
            {
                if (user != null)
                {
                    readUsers.Add(user);
                }
            }

            Assert.Equal(5, readUsers.Count);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task SerializeDocumentsAsync_ToStream_WritesCorrectly()
    {
        // Arrange
        using var stream = new MemoryStream();
        var users = GenerateUsersAsync(100);

        // Act
        await ToonSerializer.SerializeDocumentsAsync(users, stream, _options);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        
        var readUsers = new List<TestUser>();
        await foreach (var user in ToonSerializer.DeserializeDocumentsAsync<TestUser>(reader, _options))
        {
            if (user != null)
            {
                readUsers.Add(user);
            }
        }

        Assert.Equal(100, readUsers.Count);
    }

    [Fact]
    public async Task SerializeDocumentsAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var filePath = Path.GetTempFileName();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Cancel immediately

        try
        {
            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await ToonSerializer.SerializeDocumentsAsync(
                    GenerateUsersAsync(1000),
                    filePath,
                    _options,
                    cts.Token
                );
            });
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task SerializeDocumentsAsync_CustomBatchSize_ProcessesCorrectly()
    {
        // Arrange
        var filePath = Path.GetTempFileName();
        var customOptions = new ToonMultiDocumentWriteOptions
        {
            Mode = ToonMultiDocumentSeparatorMode.BlankLine,
            BatchSize = 10 // Small batch size for testing
        };

        try
        {
            // Act
            await ToonSerializer.SerializeDocumentsAsync(
                GenerateUsersAsync(50),
                filePath,
                _options,
                customOptions
            );

            // Assert - Verify all items written
            var readCount = 0;
            await foreach (var user in ToonSerializer.DeserializeDocumentsAsync<TestUser>(filePath, _options))
            {
                Assert.NotNull(user);
                readCount++;
            }

            Assert.Equal(50, readCount);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task SerializeDocumentsAsync_EmptyStream_WritesNothing()
    {
        // Arrange
        var filePath = Path.GetTempFileName();

        try
        {
            // Act
            await ToonSerializer.SerializeDocumentsAsync(
                EmptyUsersAsync(),
                filePath,
                _options
            );

            // Assert
            var content = await File.ReadAllTextAsync(filePath);
            Assert.Empty(content);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    public sealed class OptionalUser
    {
        public string? Name { get; set; }
    }

    private static async IAsyncEnumerable<T> ToAsync<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    private static async Task<List<string?>> RoundTripExplicitAsync(string?[] names, int batchSize = 50)
    {
        var ignoreNulls = new ToonSerializerOptions { IgnoreNullValues = true };
        var writeOptions = new ToonMultiDocumentWriteOptions
        {
            Mode = ToonMultiDocumentSeparatorMode.ExplicitSeparator,
            BatchSize = batchSize
        };

        using var stream = new MemoryStream();
        await ToonSerializer.SerializeDocumentsAsync(ToAsync(names.Select(n => new OptionalUser { Name = n })), stream, ignoreNulls, writeOptions);
        stream.Position = 0;

        using var reader = new StreamReader(stream);
        var result = new List<string?>();

        await foreach (var user in ToonSerializer.DeserializeDocumentsAsync<OptionalUser>(reader, ignoreNulls, ToonMultiDocumentReadOptions.ExplicitSeparator))
        {
            result.Add(Assert.IsType<OptionalUser>(user).Name);
        }

        return result;
    }

    [Theory]
    [InlineData("a,~,b")]
    [InlineData("~,a")]
    [InlineData("a,~")]
    [InlineData("a,~,~")]
    [InlineData("~")]
    [InlineData("~,~")]
    public async Task SerializeDocumentsAsync_ExplicitSeparator_KeepsEmptyDocuments(string items)
    {
        // "~" is an item without a name, which encodes to an empty document
        var names = items.Split(',').Select(n => n == "~" ? null : n).ToArray();

        Assert.Equal(names, await RoundTripExplicitAsync(names));
        Assert.Equal(names, await RoundTripExplicitAsync(names, batchSize: 1));
    }

    [Fact]
    public async Task SerializeDocumentsAsync_BlankLine_RejectsEmptyDocument()
    {
        var ignoreNulls = new ToonSerializerOptions { IgnoreNullValues = true };
        var items = ToAsync(new[] { new OptionalUser { Name = "a" }, new OptionalUser() });

        using var stream = new MemoryStream();

        var ex = await Assert.ThrowsAsync<ToonSerializationException>(() => ToonSerializer.SerializeDocumentsAsync(items, stream, ignoreNulls).AsTask());
        Assert.Contains("ExplicitSeparator", ex.Message);
    }

    [Fact]
    public async Task SerializeDocumentsAsync_Collection_RejectsEmptyDocument()
    {
        using var stream = new MemoryStream();

        await Assert.ThrowsAsync<ToonSerializationException>(() => ToonSerializer.SerializeDocumentsAsync(
            new[] { new OptionalUser() }, stream, new ToonSerializerOptions { IgnoreNullValues = true }).AsTask());
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("--\n--")]
    public async Task ExplicitSeparator_MustBeOneNonBlankLine(string separator)
    {
        using var stream = new MemoryStream();
        var writeOptions = new ToonMultiDocumentWriteOptions { Mode = ToonMultiDocumentSeparatorMode.ExplicitSeparator, DocumentSeparator = separator };
        var readOptions = new ToonMultiDocumentReadOptions { Mode = ToonMultiDocumentSeparatorMode.ExplicitSeparator, DocumentSeparator = separator };

        await Assert.ThrowsAsync<ArgumentException>(() => ToonSerializer.SerializeDocumentsAsync(GenerateUsersAsync(1), stream, null, writeOptions).AsTask());
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (var _ in ToonSerializer.DeserializeDocumentsAsync<TestUser>(new StreamReader(stream), null, readOptions))
            {
            }
        });
    }

    // Helper: Generate async enumerable of test users
    private static async IAsyncEnumerable<TestUser> GenerateUsersAsync(int count)
    {
        for (int i = 0; i < count; i++)
        {
            await Task.Yield(); // Simulate async data source
            yield return new TestUser
            {
                Name = $"User{i}",
                Age = 20 + (i % 50),
                Email = $"user{i}@test.com"
            };
        }
    }

    // Helper: Empty async enumerable
    private static async IAsyncEnumerable<TestUser> EmptyUsersAsync()
    {
        await Task.CompletedTask;
        yield break;
    }

    public sealed class TestUser
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}