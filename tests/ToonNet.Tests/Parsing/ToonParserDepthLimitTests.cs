using System.Text;
using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Parsing;

/// <summary>
///     Tests that deeply nested input is rejected with a catchable exception instead of overflowing the stack.
/// </summary>
public class ToonParserDepthLimitTests
{
    private static string NestedObjects(int depth)
    {
        var sb = new StringBuilder();

        for (var i = 0; i < depth; i++)
        {
            sb.Append(' ', i * 2).Append("k:\n");
        }

        sb.Append(' ', depth * 2).Append("leaf: 1\n");
        return sb.ToString();
    }

    [Fact]
    public void Deserialize_NestingWithinMaxDepth_Succeeds()
    {
        var options = new ToonSerializerOptions { ToonOptions = new ToonOptions { MaxDepth = 10 } };

        var result = ToonSerializer.Deserialize<Dictionary<string, object>>(NestedObjects(10), options);

        Assert.NotNull(result);
    }

    [Fact]
    public void Deserialize_NestingBeyondMaxDepth_ThrowsParseException()
    {
        var options = new ToonSerializerOptions { ToonOptions = new ToonOptions { MaxDepth = 10 } };

        var ex = Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<Dictionary<string, object>>(NestedObjects(11), options));

        Assert.Contains("Maximum nesting depth of 10 exceeded", ex.Message);
    }

    [Fact]
    public void Deserialize_DeeplyNestedListItems_ThrowsParseException()
    {
        // k[1]: / - k[1]: / ... nests an array and a list-item object per level
        var sb = new StringBuilder("k[1]:\n");

        for (var i = 0; i < 500; i++)
        {
            sb.Append(' ', 2 + i * 4).Append("- k[1]:\n");
        }

        var ex = Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<Dictionary<string, object>>(sb.ToString()));

        Assert.Contains("Maximum nesting depth", ex.Message);
    }

    [Fact]
    public void Deserialize_HostileNestingOnSmallStack_DoesNotCrashProcess()
    {
        // Before the depth guard, ~800 levels overflowed a 1 MB stack and terminated the test host.
        var input = NestedObjects(5_000);
        Exception? caught = null;

        var thread = new Thread(() =>
        {
            try
            {
                ToonSerializer.Deserialize<Dictionary<string, object>>(input);
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        }, 1024 * 1024);

        thread.Start();
        thread.Join();

        Assert.IsType<ToonParseException>(caught);
    }

    [Fact]
    public void Deserialize_ExtendedLimitsOnSmallStack_FailsGracefully()
    {
        // MaxDepth = 1000 may need more stack than a 1 MB thread provides; the stack guard must turn that into an exception.
        var options = new ToonSerializerOptions
        {
            ToonOptions = new ToonOptions { AllowExtendedLimits = true, MaxDepth = 1000 },
            AllowExtendedLimits = true,
            MaxDepth = 1000
        };
        var input = NestedObjects(999);
        Exception? caught = null;

        var thread = new Thread(() =>
        {
            try
            {
                ToonSerializer.Deserialize<Dictionary<string, object>>(input, options);
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        }, 1024 * 1024);

        thread.Start();
        thread.Join();

        Assert.True(caught is null or ToonParseException, $"Unexpected exception: {caught}");
    }

    [Theory]
    [InlineData("a: 1", 0)]
    [InlineData("[2]: 1,2", 0)]
    [InlineData("a:\n  b: 1", 1)]
    [InlineData("a: []", 1)]
    [InlineData("[1]:\n  -", 1)]
    [InlineData("[1]:\n  - x: 1", 1)]
    [InlineData("items[2]{a,b}:\n  1,2\n  3,4", 2)]
    [InlineData("a:\n  b[1]:\n    - [2]: 1,2", 3)]
    public void DepthLimit_IsTheSameForParserEncoderValidatorAndSerializer(string toon, int depth)
    {
        // The root is at depth 0 and every object or array inside another is one level deeper
        var document = ToonDocument.Parse(toon);

        for (var maxDepth = 1; maxDepth <= 4; maxDepth++)
        {
            var allowed = depth <= maxDepth;
            var options = new ToonOptions { MaxDepth = maxDepth };
            var serializerOptions = new ToonSerializerOptions { MaxDepth = maxDepth };

            Assert.Equal(allowed, Succeeds(() => ToonDocument.Parse(toon, options)));
            Assert.Equal(allowed, Succeeds(() => new ToonNet.Core.Encoding.ToonEncoder(options).Encode(document)));
            Assert.Equal(allowed, ToonNet.Core.Validation.ToonValidator.Validate(document, options).IsValid);
            Assert.Equal(allowed, Succeeds(() => ToonSerializer.DeserializeFromValue<object>(document.Root, serializerOptions)));
        }

        static bool Succeeds(Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (ToonException)
            {
                return false;
            }
        }
    }
}
