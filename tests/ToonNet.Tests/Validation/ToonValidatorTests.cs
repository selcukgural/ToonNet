using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Validation;

namespace ToonNet.Tests.Validation;

/// <summary>
/// Tests for TOON validation API behaviors.
/// </summary>
public sealed class ToonValidatorTests
{
    /// <summary>
    /// Verifies that a valid TOON payload yields no errors.
    /// </summary>
    [Fact]
    public void Validate_ValidContent_ReturnsValidResult()
    {
        const string input = "key: value";

        var result = ToonValidator.Validate(input);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// Verifies that an invalid TOON payload yields a parse error.
    /// </summary>
    [Fact]
    public void Validate_InvalidContent_ReturnsError()
    {
        const string input = "key: \"unterminated string";

        var result = ToonValidator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(ToonValidationErrorCodes.ParseError, result.Errors[0].Code);
        Assert.Equal(ValidationSeverity.Error, result.Errors[0].Severity);
    }

    /// <summary>
    /// Verifies that non-strict mode reports strict-only violations as warnings.
    /// </summary>
    [Fact]
    public void Validate_NonStrictContent_ReturnsWarning()
    {
        const string input = "items[5]: 1,2,3";
        var options = new ToonOptions { StrictMode = false };

        var result = ToonValidator.Validate(input, options);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Single(result.Warnings);
        Assert.Equal(ToonValidationErrorCodes.StrictModeViolation, result.Warnings[0].Code);
        Assert.Equal(ValidationSeverity.Warning, result.Warnings[0].Severity);
    }

    /// <summary>
    /// Verifies that document depth violations are reported as errors.
    /// </summary>
    [Fact]
    public void Validate_DocumentDepthExceeded_ReturnsError()
    {
        var document = new ToonDocument(new ToonObject
        {
            ["level1"] = new ToonObject
            {
                ["level2"] = new ToonObject()
            }
        });

        var options = new ToonOptions { MaxDepth = 1 };

        var result = ToonValidator.Validate(document, options);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(ToonValidationErrorCodes.MaxDepthExceeded, result.Errors[0].Code);
    }

    /// <summary>
    /// Verifies that complex nested TOON content is validated correctly.
    /// </summary>
    [Fact]
    public void Validate_ComplexNestedContent_ReturnsValidResult()
    {
        const string input = """
root:
  name: sample
  tags[3]: alpha,beta,gamma
  items:
    - id: 1
      name: first
    - id: 2
      name: second
""";

        var result = ToonValidator.Validate(input);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    /// <summary>
    /// Verifies that fatal syntax errors in non-strict mode return errors, not warnings.
    /// </summary>
    [Fact]
    public void Validate_NonStrictFatalSyntax_ReturnsErrorNotWarning()
    {
        const string input = """
root:
  name: value
  items[5
""";
        var options = new ToonOptions { StrictMode = false };

        var result = ToonValidator.Validate(input, options);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Empty(result.Warnings);
        Assert.Equal(ToonValidationErrorCodes.ParseError, result.Errors[0].Code);
        Assert.Equal(ValidationSeverity.Error, result.Errors[0].Severity);
    }

    /// <summary>
    /// Verifies that documents within the allowed depth limit are valid.
    /// </summary>
    [Fact]
    public void Validate_DocumentWithinDepthLimit_ReturnsValid()
    {
        var document = new ToonDocument(new ToonObject
        {
            ["level1"] = new ToonObject
            {
                ["level2"] = new ToonObject
                {
                    ["level3"] = new ToonString("ok")
                }
            }
        });

        var options = new ToonOptions { MaxDepth = 5 };

        var result = ToonValidator.Validate(document, options);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }
}
