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

    [Fact]
    public async Task DeserializeStreamAsync_MultipleObjects_YieldsAllObjects()
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
            await foreach (var user in ToonSerializer.DeserializeStreamAsync<TestUser>(filePath, _options))
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
    public async Task DeserializeStreamAsync_ExplicitSeparator_YieldsAllObjects()
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
            await foreach (var user in ToonSerializer.DeserializeStreamAsync<TestUser>(filePath, _options, ToonMultiDocumentReadOptions.ExplicitSeparator))
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
    public async Task AsyncOperationsWithCancellation_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Cancel immediately
        
        var user = new TestUser { Name = "Test", Age = 1, Email = "test@test.com" };

        // Act & Assert
        var ex1 = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ToonSerializer.SerializeAsync(user, _options, cts.Token));
            
        var ex2 = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ToonSerializer.DeserializeAsync<TestUser>("Name: Test", _options, cts.Token));
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
    public async Task DeserializeStreamAsync_EmptyFile_ReturnsNoItems()
    {
        // Arrange
        var filePath = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(filePath, string.Empty);

            // Act
            var items = new List<TestUser>();
            await foreach (var item in ToonSerializer.DeserializeStreamAsync<TestUser>(filePath, _options))
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

    public sealed class TestUser
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}