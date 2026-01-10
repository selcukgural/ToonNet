using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Validation;

/// <summary>
///     Tests for ToonSerializerOptions validation.
/// </summary>
public class ToonSerializerOptionsValidationTests
{
    #region ToonOptions Property Tests

    [Fact]
    public void ToonOptions_SetNull_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ToonSerializerOptions();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => options.ToonOptions = null!);
        Assert.Contains("ToonOptions cannot be null", ex.Message);
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void ToonOptions_SetValidValue_Succeeds()
    {
        // Arrange
        var options = new ToonSerializerOptions();
        var toonOptions = new ToonOptions { IndentSize = 4 };

        // Act
        options.ToonOptions = toonOptions;

        // Assert
        Assert.Same(toonOptions, options.ToonOptions);
    }

    [Fact]
    public void ToonOptions_Default_IsNotNull()
    {
        // Arrange & Act
        var options = new ToonSerializerOptions();

        // Assert
        Assert.NotNull(options.ToonOptions);
    }

    #endregion

    #region MaxDepth Property Tests

    [Theory]
    [InlineData(-100)]
    [InlineData(-1)]
    [InlineData(0)]
    public void MaxDepth_BelowMinimum_ThrowsArgumentOutOfRangeException(int value)
    {
        // Arrange
        var options = new ToonSerializerOptions();

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
        var options = new ToonSerializerOptions { AllowExtendedLimits = false };

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxDepth = value);
        Assert.Contains("cannot exceed 200", ex.Message);
        Assert.Contains("AllowExtendedLimits", ex.Message);
        Assert.Equal(nameof(value), ex.ParamName);
    }

    [Theory]
    [InlineData(1001)]
    [InlineData(10000)]
    [InlineData(int.MaxValue)]
    public void MaxDepth_AboveExtendedMaximum_WithExtendedLimits_ThrowsArgumentOutOfRangeException(int value)
    {
        // Arrange
        var options = new ToonSerializerOptions { AllowExtendedLimits = true };

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxDepth = value);
        Assert.Contains("cannot exceed 1000", ex.Message);
        Assert.Equal(nameof(value), ex.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(100)]
    [InlineData(150)]
    [InlineData(200)]
    public void MaxDepth_ValidValue_WithoutExtendedLimits_Succeeds(int value)
    {
        // Arrange
        var options = new ToonSerializerOptions { AllowExtendedLimits = false };

        // Act
        options.MaxDepth = value;

        // Assert
        Assert.Equal(value, options.MaxDepth);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(500)]
    [InlineData(1000)]
    public void MaxDepth_ValidValue_WithExtendedLimits_Succeeds(int value)
    {
        // Arrange
        var options = new ToonSerializerOptions { AllowExtendedLimits = true };

        // Act
        options.MaxDepth = value;

        // Assert
        Assert.Equal(value, options.MaxDepth);
    }

    [Fact]
    public void MaxDepth_Default_Is100()
    {
        // Arrange & Act
        var options = new ToonSerializerOptions();

        // Assert
        Assert.Equal(100, options.MaxDepth);
    }

    [Fact]
    public void AllowExtendedLimits_Default_IsFalse()
    {
        // Arrange & Act
        var options = new ToonSerializerOptions();

        // Assert
        Assert.False(options.AllowExtendedLimits);
    }

    #endregion

    #region AddConverter Tests

    [Fact]
    public void AddConverter_NullConverter_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ToonSerializerOptions();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => options.AddConverter(null!));
        Assert.Equal("converter", ex.ParamName);
    }

    [Fact]
    public void AddConverter_ValidConverter_AddsToCollection()
    {
        // Arrange
        var options = new ToonSerializerOptions();
        var converter = new TestConverter();

        // Act
        options.AddConverter(converter);

        // Assert
        Assert.Contains(converter, options.Converters);
        Assert.Single(options.Converters);
    }

    [Fact]
    public void AddConverter_MultipleConverters_AddsAllToCollection()
    {
        // Arrange
        var options = new ToonSerializerOptions();
        var converter1 = new TestConverter();
        var converter2 = new TestConverter();

        // Act
        options.AddConverter(converter1);
        options.AddConverter(converter2);

        // Assert
        Assert.Equal(2, options.Converters.Count);
        Assert.Contains(converter1, options.Converters);
        Assert.Contains(converter2, options.Converters);
    }

    #endregion

    #region GetConverter Tests

    [Fact]
    public void GetConverter_NoMatchingConverter_ReturnsNull()
    {
        // Arrange
        var options = new ToonSerializerOptions();

        // Act
        var converter = options.GetConverter(typeof(string));

        // Assert
        Assert.Null(converter);
    }

    [Fact]
    public void GetConverter_MatchingConverter_ReturnsConverter()
    {
        // Arrange
        var options = new ToonSerializerOptions();
        var testConverter = new TestConverter();
        options.AddConverter(testConverter);

        // Act
        var converter = options.GetConverter(typeof(TestClass));

        // Assert
        Assert.Same(testConverter, converter);
    }

    #endregion

    #region Boolean Properties Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IgnoreNullValues_AnyBooleanValue_Succeeds(bool value)
    {
        // Arrange
        var options = new ToonSerializerOptions();

        // Act
        options.IgnoreNullValues = value;

        // Assert
        Assert.Equal(value, options.IgnoreNullValues);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IncludeTypeInformation_AnyBooleanValue_Succeeds(bool value)
    {
        // Arrange
        var options = new ToonSerializerOptions();

        // Act
        options.IncludeTypeInformation = value;

        // Assert
        Assert.Equal(value, options.IncludeTypeInformation);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PublicOnly_AnyBooleanValue_Succeeds(bool value)
    {
        // Arrange
        var options = new ToonSerializerOptions();

        // Act
        options.PublicOnly = value;

        // Assert
        Assert.Equal(value, options.PublicOnly);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IncludeReadOnlyProperties_AnyBooleanValue_Succeeds(bool value)
    {
        // Arrange
        var options = new ToonSerializerOptions();

        // Act
        options.IncludeReadOnlyProperties = value;

        // Assert
        Assert.Equal(value, options.IncludeReadOnlyProperties);
    }

    #endregion

    #region PropertyNamingPolicy Tests

    [Theory]
    [InlineData(PropertyNamingPolicy.Default)]
    [InlineData(PropertyNamingPolicy.CamelCase)]
    [InlineData(PropertyNamingPolicy.SnakeCase)]
    [InlineData(PropertyNamingPolicy.LowerCase)]
    public void PropertyNamingPolicy_ValidEnumValue_Succeeds(PropertyNamingPolicy value)
    {
        // Arrange
        var options = new ToonSerializerOptions();

        // Act
        options.PropertyNamingPolicy = value;

        // Assert
        Assert.Equal(value, options.PropertyNamingPolicy);
    }

    #endregion

    #region Default Instance Tests

    [Fact]
    public void Default_ReturnsNewInstance_WithStandardSettings()
    {
        // Act
        var options = ToonSerializerOptions.Default;

        // Assert
        Assert.NotNull(options.ToonOptions);
        Assert.Equal(100, options.MaxDepth);
        Assert.False(options.IgnoreNullValues);
        Assert.Equal(PropertyNamingPolicy.Default, options.PropertyNamingPolicy);
        Assert.False(options.IncludeTypeInformation);
        Assert.True(options.PublicOnly);
        Assert.True(options.IncludeReadOnlyProperties);
        Assert.False(options.AllowExtendedLimits);
        Assert.Empty(options.Converters);
    }

    #endregion

    #region Test Helper Classes

    private class TestConverter : IToonConverter
    {
        public bool CanConvert(Type type) => type == typeof(TestClass);

        public ToonValue? Write(object? value, ToonSerializerOptions options) => null;

        public object? Read(ToonValue value, Type targetType, ToonSerializerOptions options) => null;
    }

    private class TestClass
    {
    }

    #endregion
}
