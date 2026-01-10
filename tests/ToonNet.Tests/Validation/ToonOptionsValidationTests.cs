using ToonNet.Core;

namespace ToonNet.Tests.Validation;

/// <summary>
///     Tests for ToonOptions validation.
/// </summary>
public class ToonOptionsValidationTests
{
    #region IndentSize Validation Tests

    [Theory]
    [InlineData(-10)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void IndentSize_BelowMinimum_ThrowsArgumentOutOfRangeException(int value)
    {
        // Arrange
        var options = new ToonOptions();

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.IndentSize = value);
        Assert.Contains("IndentSize must be at least 2", ex.Message);
        Assert.Equal(nameof(value), ex.ParamName);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(99)]
    public void IndentSize_OddNumber_ThrowsArgumentOutOfRangeException(int value)
    {
        // Arrange
        var options = new ToonOptions();

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.IndentSize = value);
        Assert.Contains("must be an even number", ex.Message);
        Assert.Contains("TOON specification §12", ex.Message);
        Assert.Equal(nameof(value), ex.ParamName);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(200)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void IndentSize_AboveMaximum_ThrowsArgumentOutOfRangeException(int value)
    {
        // Arrange
        var options = new ToonOptions();

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.IndentSize = value);
        Assert.Contains("cannot exceed 100", ex.Message);
        Assert.Equal(nameof(value), ex.ParamName);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(100)]
    public void IndentSize_ValidValue_Succeeds(int value)
    {
        // Arrange
        var options = new ToonOptions();

        // Act
        options.IndentSize = value;

        // Assert
        Assert.Equal(value, options.IndentSize);
    }

    [Fact]
    public void IndentSize_Default_IsTwo()
    {
        // Arrange & Act
        var options = new ToonOptions();

        // Assert
        Assert.Equal(2, options.IndentSize);
    }

    #endregion

    #region MaxDepth Validation Tests

    [Theory]
    [InlineData(-1000)]
    [InlineData(-1)]
    [InlineData(0)]
    public void MaxDepth_BelowMinimum_ThrowsArgumentOutOfRangeException(int value)
    {
        // Arrange
        var options = new ToonOptions();

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxDepth = value);
        Assert.Contains("MaxDepth must be at least 1", ex.Message);
        Assert.Equal(nameof(value), ex.ParamName);
    }

    [Theory]
    [InlineData(201)]
    [InlineData(500)]
    [InlineData(1000)]
    public void MaxDepth_AboveStandardMaximum_WithoutExtendedLimits_ThrowsArgumentOutOfRangeException(int value)
    {
        // Arrange
        var options = new ToonOptions { AllowExtendedLimits = false };

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxDepth = value);
        Assert.Contains("cannot exceed 200", ex.Message);
        Assert.Contains("AllowExtendedLimits", ex.Message);
        Assert.Equal(nameof(value), ex.ParamName);
    }

    [Theory]
    [InlineData(1001)]
    [InlineData(5000)]
    [InlineData(int.MaxValue)]
    public void MaxDepth_AboveExtendedMaximum_WithExtendedLimits_ThrowsArgumentOutOfRangeException(int value)
    {
        // Arrange
        var options = new ToonOptions { AllowExtendedLimits = true };

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxDepth = value);
        Assert.Contains("cannot exceed 1000", ex.Message);
        Assert.Equal(nameof(value), ex.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(150)]
    [InlineData(200)]
    public void MaxDepth_ValidValue_WithoutExtendedLimits_Succeeds(int value)
    {
        // Arrange
        var options = new ToonOptions { AllowExtendedLimits = false };

        // Act
        options.MaxDepth = value;

        // Assert
        Assert.Equal(value, options.MaxDepth);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(500)]
    [InlineData(1000)]
    public void MaxDepth_ValidValue_WithExtendedLimits_Succeeds(int value)
    {
        // Arrange
        var options = new ToonOptions { AllowExtendedLimits = true };

        // Act
        options.MaxDepth = value;

        // Assert
        Assert.Equal(value, options.MaxDepth);
    }

    [Fact]
    public void MaxDepth_Default_Is100()
    {
        // Arrange & Act
        var options = new ToonOptions();

        // Assert
        Assert.Equal(100, options.MaxDepth);
    }

    [Fact]
    public void AllowExtendedLimits_Default_IsFalse()
    {
        // Arrange & Act
        var options = new ToonOptions();

        // Assert
        Assert.False(options.AllowExtendedLimits);
    }

    #endregion

    #region Delimiter Validation Tests

    [Theory]
    [InlineData(' ')]
    [InlineData('\u00A0')] // Non-breaking space
    [InlineData('\u2000')] // En quad
    [InlineData('\u3000')] // Ideographic space
    public void Delimiter_WhitespaceCharacter_ThrowsArgumentException(char value)
    {
        // Arrange
        var options = new ToonOptions();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Delimiter = value);
        Assert.Contains("cannot be a whitespace character", ex.Message);
        Assert.Contains($"U+{(int)value:X4}", ex.Message);
        Assert.Equal(nameof(value), ex.ParamName);
    }

    [Theory]
    [InlineData('\n')]
    [InlineData('\r')]
    [InlineData('\t')]
    public void Delimiter_NewlineOrTab_ThrowsArgumentException(char value)
    {
        // Arrange
        var options = new ToonOptions();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Delimiter = value);
        Assert.Contains("cannot be a newline or tab character", ex.Message);
        Assert.Contains($"U+{(int)value:X4}", ex.Message);
        Assert.Equal(nameof(value), ex.ParamName);
    }

    [Theory]
    [InlineData('\u0000')] // Null
    [InlineData('\u0001')] // Start of heading
    [InlineData('\u001F')] // Unit separator
    [InlineData('\u007F')] // Delete
    public void Delimiter_ControlCharacter_ThrowsArgumentException(char value)
    {
        // Arrange
        var options = new ToonOptions();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Delimiter = value);
        Assert.Contains("cannot be a control character", ex.Message);
        Assert.Contains($"U+{(int)value:X4}", ex.Message);
        Assert.Equal(nameof(value), ex.ParamName);
    }

    [Theory]
    [InlineData(',')]
    [InlineData(';')]
    [InlineData('|')]
    [InlineData('-')]
    [InlineData('_')]
    [InlineData('.')]
    public void Delimiter_ValidCharacter_Succeeds(char value)
    {
        // Arrange
        var options = new ToonOptions();

        // Act
        options.Delimiter = value;

        // Assert
        Assert.Equal(value, options.Delimiter);
    }

    [Fact]
    public void Delimiter_Default_IsComma()
    {
        // Arrange & Act
        var options = new ToonOptions();

        // Assert
        Assert.Equal(',', options.Delimiter);
    }

    #endregion

    #region StrictMode Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StrictMode_AnyBooleanValue_Succeeds(bool value)
    {
        // Arrange
        var options = new ToonOptions();

        // Act
        options.StrictMode = value;

        // Assert
        Assert.Equal(value, options.StrictMode);
    }

    [Fact]
    public void StrictMode_Default_IsTrue()
    {
        // Arrange & Act
        var options = new ToonOptions();

        // Assert
        Assert.True(options.StrictMode);
    }

    #endregion

    #region Default Instance Tests

    [Fact]
    public void Default_ReturnsNewInstance_WithStandardSettings()
    {
        // Act
        var options = ToonOptions.Default;

        // Assert
        Assert.Equal(2, options.IndentSize);
        Assert.Equal(100, options.MaxDepth);
        Assert.Equal(',', options.Delimiter);
        Assert.True(options.StrictMode);
        Assert.False(options.AllowExtendedLimits);
    }

    [Fact]
    public void Default_ReturnsNewInstance_EachTime()
    {
        // Act
        var options1 = ToonOptions.Default;
        var options2 = ToonOptions.Default;

        // Assert
        Assert.NotSame(options1, options2);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ToonOptions_CanSetMultipleProperties_InSequence()
    {
        // Arrange
        var options = new ToonOptions();

        // Act
        options.IndentSize = 4;
        options.MaxDepth = 100;
        options.Delimiter = ';';
        options.StrictMode = false;

        // Assert
        Assert.Equal(4, options.IndentSize);
        Assert.Equal(100, options.MaxDepth);
        Assert.Equal(';', options.Delimiter);
        Assert.False(options.StrictMode);
    }

    [Fact]
    public void ToonOptions_InvalidValue_DoesNotChangeProperty()
    {
        // Arrange
        var options = new ToonOptions { IndentSize = 4 };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => options.IndentSize = 3);
        Assert.Equal(4, options.IndentSize); // Value should remain unchanged
    }

    #endregion
}
