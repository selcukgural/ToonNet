using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using Xunit.Abstractions;

namespace ToonNet.Tests.Serialization;

/// <summary>
///     Tests for ToonConverter&lt;T&gt; base class and custom converters.
/// </summary>
public class ToonConverterTests
{
    private readonly ITestOutputHelper _output;

    public ToonConverterTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Custom Converter Examples

    // Example: Custom Point converter
    private class Point
    {
        public Point() { }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public override bool Equals(object? obj)
        {
            return obj is Point p && p.X == X && p.Y == Y;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
    }

    // Custom converter: Point as "x,y" string
    private class PointConverter : ToonConverter<Point>
    {
        public override ToonValue Write(Point? value, ToonSerializerOptions options)
        {
            if (value == null)
            {
                return ToonNull.Instance;
            }

            return new ToonString($"{value.X},{value.Y}");
        }

        public override Point? Read(ToonValue value, ToonSerializerOptions options)
        {
            if (value is ToonNull)
            {
                return null;
            }

            if (value is not ToonString str)
            {
                throw ToonSerializationException.Create("Expected string for Point conversion", value: value,
                                                        suggestion: "Use format: 'x,y' (e.g., '10,20')");
            }

            var parts = str.Value.Split(',');

            if (parts.Length != 2)
            {
                throw ToonSerializationException.Create("Invalid Point format", value: str.Value, suggestion: "Use format: 'x,y' (e.g., '10,20')");
            }

            if (!int.TryParse(parts[0], out var x) || !int.TryParse(parts[1], out var y))
            {
                throw ToonSerializationException.Create("Invalid Point coordinates", value: str.Value,
                                                        suggestion: "Both x and y must be valid integers");
            }

            return new Point(x, y);
        }
    }

    // Example: Custom Color converter
    private class Color
    {
        public Color() { }

        public Color(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public override bool Equals(object? obj)
        {
            return obj is Color c && c.R == R && c.G == G && c.B == B;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(R, G, B);
        }
    }

    // Custom converter: Color as hex string
    private class ColorConverter : ToonConverter<Color>
    {
        public override ToonValue Write(Color? value, ToonSerializerOptions options)
        {
            if (value == null)
            {
                return ToonNull.Instance;
            }

            return new ToonString($"#{value.R:X2}{value.G:X2}{value.B:X2}");
        }

        public override Color? Read(ToonValue value, ToonSerializerOptions options)
        {
            if (value is ToonNull)
            {
                return null;
            }

            if (value is not ToonString str)
            {
                throw ToonSerializationException.Create("Expected string for Color conversion", value: value,
                                                        suggestion: "Use hex format: '#RRGGBB' (e.g., '#FF5733')");
            }

            var hex = str.Value;

            if (!hex.StartsWith("#") || hex.Length != 7)
            {
                throw ToonSerializationException.Create("Invalid Color format", value: hex,
                                                        suggestion: "Use hex format: '#RRGGBB' (e.g., '#FF5733')");
            }

            try
            {
                var r = Convert.ToByte(hex.Substring(1, 2), 16);
                var g = Convert.ToByte(hex.Substring(3, 2), 16);
                var b = Convert.ToByte(hex.Substring(5, 2), 16);
                return new Color(r, g, b);
            }
            catch (Exception)
            {
                throw ToonSerializationException.Create("Failed to parse Color hex value", value: hex,
                                                        suggestion: "Ensure each RGB component is a valid hex value (00-FF)");
            }
        }
    }

    // Example: Temperature converter with unit conversion
    private class Temperature
    {
        public Temperature() { }

        public Temperature(double value, string unit)
        {
            Value = value;
            Unit = unit;
        }

        public double Value { get; }
        public string Unit { get; } = "C"; // C or F

        public override bool Equals(object? obj)
        {
            return obj is Temperature t && Math.Abs(t.Value - Value) < 0.001 && t.Unit == Unit;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, Unit);
        }
    }

    // Custom converter: Always store as Celsius
    private class TemperatureConverter : ToonConverter<Temperature>
    {
        public override ToonValue Write(Temperature? value, ToonSerializerOptions options)
        {
            if (value == null)
            {
                return ToonNull.Instance;
            }

            // Convert to Celsius for storage
            var celsius = value.Unit == "F" ? (value.Value - 32) * 5 / 9 : value.Value;

            var obj = new ToonObject();
            obj["value"] = new ToonNumber(celsius);
            obj["unit"] = new ToonString("C");
            return obj;
        }

        public override Temperature? Read(ToonValue value, ToonSerializerOptions options)
        {
            if (value is ToonNull)
            {
                return null;
            }

            if (value is not ToonObject obj)
            {
                throw ToonSerializationException.Create("Expected object for Temperature conversion", value: value);
            }

            if (!obj.Properties.TryGetValue("value", out var valueNode) || valueNode is not ToonNumber num)
            {
                throw ToonSerializationException.Create("Temperature object must have 'value' field", value: value);
            }

            // Always read as Celsius (stored format)
            return new Temperature(num.Value, "C");
        }
    }

    #endregion

    #region CanConvert Tests

    [Fact]
    public void CanConvert_ExactType_ReturnsTrue()
    {
        // Arrange
        var converter = new PointConverter();

        // Act
        var canConvert = converter.CanConvert(typeof(Point));

        // Assert
        Assert.True(canConvert);
    }

    [Fact]
    public void CanConvert_DifferentType_ReturnsFalse()
    {
        // Arrange
        var converter = new PointConverter();

        // Act
        var canConvert = converter.CanConvert(typeof(Color));

        // Assert
        Assert.False(canConvert);
    }

    [Fact]
    public void CanConvert_DerivedType_ReturnsTrue()
    {
        // Arrange
        var converter = new PointConverter();

        // Act - Point is assignable to Point
        var canConvert = converter.CanConvert(typeof(Point));

        // Assert
        Assert.True(canConvert);
    }

    #endregion

    #region Write Tests

    [Fact]
    public void Write_ValidPoint_ReturnsStringValue()
    {
        // Arrange
        var converter = new PointConverter();
        var point = new Point(10, 20);
        var options = new ToonSerializerOptions();

        // Act
        var result = converter.Write(point, options);

        // Assert
        Assert.IsType<ToonString>(result);
        Assert.Equal("10,20", ((ToonString)result!).Value);
    }

    [Fact]
    public void Write_NullPoint_ReturnsNull()
    {
        // Arrange
        var converter = new PointConverter();
        var options = new ToonSerializerOptions();

        // Act
        var result = converter.Write(null, options);

        // Assert
        Assert.IsType<ToonNull>(result);
    }

    [Fact]
    public void Write_ValidColor_ReturnsHexString()
    {
        // Arrange
        var converter = new ColorConverter();
        var color = new Color(255, 87, 51); // #FF5733
        var options = new ToonSerializerOptions();

        // Act
        var result = converter.Write(color, options);

        // Assert
        Assert.IsType<ToonString>(result);
        Assert.Equal("#FF5733", ((ToonString)result!).Value);
    }

    [Fact]
    public void Write_BlackColor_ReturnsCorrectHex()
    {
        // Arrange
        var converter = new ColorConverter();
        var color = new Color(0, 0, 0);
        var options = new ToonSerializerOptions();

        // Act
        var result = converter.Write(color, options);

        // Assert
        Assert.IsType<ToonString>(result);
        Assert.Equal("#000000", ((ToonString)result!).Value);
    }

    [Fact]
    public void Write_Temperature_ConvertsToObject()
    {
        // Arrange
        var converter = new TemperatureConverter();
        var temp = new Temperature(20, "C");
        var options = new ToonSerializerOptions();

        // Act
        var result = converter.Write(temp, options);

        // Assert
        Assert.IsType<ToonObject>(result);
        var obj = (ToonObject)result!;
        Assert.True(obj.Properties.TryGetValue("value", out var valueNode));
        Assert.Equal(20, ((ToonNumber)valueNode).Value);
        Assert.True(obj.Properties.TryGetValue("unit", out var unitNode));
        Assert.Equal("C", ((ToonString)unitNode).Value);
    }

    [Fact]
    public void Write_FahrenheitTemperature_ConvertsToCelsius()
    {
        // Arrange
        var converter = new TemperatureConverter();
        var temp = new Temperature(68, "F"); // 68°F = 20°C
        var options = new ToonSerializerOptions();

        // Act
        var result = converter.Write(temp, options);

        // Assert
        Assert.IsType<ToonObject>(result);
        var obj = (ToonObject)result!;
        Assert.True(obj.Properties.TryGetValue("value", out var valueNode));
        var celsius = ((ToonNumber)valueNode).Value;
        Assert.Equal(20, celsius, 0.1); // Allow small floating point diff
    }

    #endregion

    #region Read Tests

    [Fact]
    public void Read_ValidStringPoint_ReturnsPoint()
    {
        // Arrange
        var converter = new PointConverter();
        var value = new ToonString("10,20");
        var options = new ToonSerializerOptions();

        // Act
        var result = converter.Read(value, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result!.X);
        Assert.Equal(20, result.Y);
    }

    [Fact]
    public void Read_NullValue_ReturnsNull()
    {
        // Arrange
        var converter = new PointConverter();
        var value = ToonNull.Instance;
        var options = new ToonSerializerOptions();

        // Act
        var result = converter.Read(value, options);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Read_InvalidTypeForPoint_ThrowsException()
    {
        // Arrange
        var converter = new PointConverter();
        var value = new ToonNumber(42);
        var options = new ToonSerializerOptions();

        // Act & Assert
        var ex = Assert.Throws<ToonSerializationException>(() => converter.Read(value, options));

        Assert.Contains("Expected string", ex.Message);
        Assert.Contains("'x,y'", ex.ToString()); // Suggestion is in ToString()
    }

    [Fact]
    public void Read_InvalidPointFormat_ThrowsException()
    {
        // Arrange
        var converter = new PointConverter();
        var value = new ToonString("invalid");
        var options = new ToonSerializerOptions();

        // Act & Assert
        var ex = Assert.Throws<ToonSerializationException>(() => converter.Read(value, options));

        Assert.Contains("Invalid Point format", ex.Message);
        Assert.Contains("x,y", ex.ToString()); // Suggestion is in ToString()
    }

    [Fact]
    public void Read_NonNumericPointCoordinates_ThrowsException()
    {
        // Arrange
        var converter = new PointConverter();
        var value = new ToonString("abc,def");
        var options = new ToonSerializerOptions();

        // Act & Assert
        var ex = Assert.Throws<ToonSerializationException>(() => converter.Read(value, options));

        Assert.Contains("Invalid Point coordinates", ex.Message);
    }

    [Fact]
    public void Read_ValidColorHex_ReturnsColor()
    {
        // Arrange
        var converter = new ColorConverter();
        var value = new ToonString("#FF5733");
        var options = new ToonSerializerOptions();

        // Act
        var result = converter.Read(value, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(255, result!.R);
        Assert.Equal(87, result.G);
        Assert.Equal(51, result.B);
    }

    [Fact]
    public void Read_InvalidColorFormat_ThrowsException()
    {
        // Arrange
        var converter = new ColorConverter();
        var value = new ToonString("FF5733"); // Missing #
        var options = new ToonSerializerOptions();

        // Act & Assert
        var ex = Assert.Throws<ToonSerializationException>(() => converter.Read(value, options));

        Assert.Contains("Invalid Color format", ex.Message);
        Assert.Contains("#RRGGBB", ex.ToString()); // Suggestion is in ToString()
    }

    [Fact]
    public void Read_InvalidColorHex_ThrowsException()
    {
        // Arrange
        var converter = new ColorConverter();
        var value = new ToonString("#GGGGGG"); // Invalid hex
        var options = new ToonSerializerOptions();

        // Act & Assert
        var ex = Assert.Throws<ToonSerializationException>(() => converter.Read(value, options));

        Assert.Contains("Failed to parse Color", ex.Message);
    }

    [Fact]
    public void Read_TemperatureObject_ReturnsTemperature()
    {
        // Arrange
        var converter = new TemperatureConverter();
        var obj = new ToonObject();
        obj["value"] = new ToonNumber(25);
        obj["unit"] = new ToonString("C");
        var options = new ToonSerializerOptions();

        // Act
        var result = converter.Read(obj, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25, result!.Value);
        Assert.Equal("C", result.Unit);
    }

    [Fact]
    public void Read_TemperatureMissingValue_ThrowsException()
    {
        // Arrange
        var converter = new TemperatureConverter();
        var obj = new ToonObject();
        obj["unit"] = new ToonString("C");
        var options = new ToonSerializerOptions();

        // Act & Assert
        var ex = Assert.Throws<ToonSerializationException>(() => converter.Read(obj, options));

        Assert.Contains("must have 'value' field", ex.Message);
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void RoundTrip_Point_PreservesValue()
    {
        // Arrange
        var converter = new PointConverter();
        var original = new Point(42, 100);
        var options = new ToonSerializerOptions();

        // Act
        var written = converter.Write(original, options);
        var restored = converter.Read(written!, options);

        // Assert
        Assert.Equal(original, restored);
    }

    [Fact]
    public void RoundTrip_Color_PreservesValue()
    {
        // Arrange
        var converter = new ColorConverter();
        var original = new Color(128, 64, 255);
        var options = new ToonSerializerOptions();

        // Act
        var written = converter.Write(original, options);
        var restored = converter.Read(written!, options);

        // Assert
        Assert.Equal(original, restored);
    }

    [Fact]
    public void RoundTrip_Temperature_PreservesValue()
    {
        // Arrange
        var converter = new TemperatureConverter();
        var original = new Temperature(30, "C");
        var options = new ToonSerializerOptions();

        // Act
        var written = converter.Write(original, options);
        var restored = converter.Read(written!, options);

        // Assert
        Assert.Equal(original, restored);
    }

    [Fact]
    public void RoundTrip_NullPoint_PreservesNull()
    {
        // Arrange
        var converter = new PointConverter();
        Point? original = null;
        var options = new ToonSerializerOptions();

        // Act
        var written = converter.Write(original, options);
        var restored = converter.Read(written!, options);

        // Assert
        Assert.Null(restored);
    }

    #endregion

    #region Non-Generic Interface Tests

    [Fact]
    public void NonGenericWrite_ValidPoint_ReturnsStringValue()
    {
        // Arrange
        IToonConverter converter = new PointConverter();
        var point = new Point(15, 25);
        var options = new ToonSerializerOptions();

        // Act
        var result = converter.Write(point, options);

        // Assert
        Assert.IsType<ToonString>(result);
        Assert.Equal("15,25", ((ToonString)result!).Value);
    }

    [Fact]
    public void NonGenericRead_ValidString_ReturnsPoint()
    {
        // Arrange
        IToonConverter converter = new PointConverter();
        var value = new ToonString("30,40");
        var options = new ToonSerializerOptions();

        // Act
        var result = converter.Read(value, typeof(Point), options);

        // Assert
        Assert.IsType<Point>(result);
        var point = (Point)result!;
        Assert.Equal(30, point.X);
        Assert.Equal(40, point.Y);
    }

    [Fact]
    public void NonGenericWrite_NullValue_ReturnsNull()
    {
        // Arrange
        IToonConverter converter = new PointConverter();
        var options = new ToonSerializerOptions();

        // Act
        var result = converter.Write(null, options);

        // Assert
        Assert.IsType<ToonNull>(result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Write_NegativePointCoordinates_WorksCorrectly()
    {
        // Arrange
        var converter = new PointConverter();
        var point = new Point(-10, -20);
        var options = new ToonSerializerOptions();

        // Act
        var result = converter.Write(point, options);

        // Assert
        Assert.IsType<ToonString>(result);
        Assert.Equal("-10,-20", ((ToonString)result!).Value);
    }

    [Fact]
    public void Read_NegativePointCoordinates_ParsesCorrectly()
    {
        // Arrange
        var converter = new PointConverter();
        var value = new ToonString("-5,-15");
        var options = new ToonSerializerOptions();

        // Act
        var result = converter.Read(value, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(-5, result!.X);
        Assert.Equal(-15, result.Y);
    }

    [Fact]
    public void Write_ZeroValues_WorksCorrectly()
    {
        // Arrange
        var converter = new PointConverter();
        var point = new Point(0, 0);
        var options = new ToonSerializerOptions();

        // Act
        var result = converter.Write(point, options);

        // Assert
        Assert.IsType<ToonString>(result);
        Assert.Equal("0,0", ((ToonString)result!).Value);
    }

    [Fact]
    public void Read_LowercaseColorHex_ThrowsException()
    {
        // Arrange
        var converter = new ColorConverter();
        var value = new ToonString("#ff5733"); // lowercase
        var options = new ToonSerializerOptions();

        // Act
        var result = converter.Read(value, options);

        // Assert - Should still work (hex parsing is case-insensitive)
        Assert.NotNull(result);
        Assert.Equal(255, result!.R);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void MultipleConverters_WorkIndependently()
    {
        // Arrange
        var pointConverter = new PointConverter();
        var colorConverter = new ColorConverter();
        var options = new ToonSerializerOptions();

        var point = new Point(10, 20);
        var color = new Color(255, 0, 0);

        // Act
        var pointValue = pointConverter.Write(point, options);
        var colorValue = colorConverter.Write(color, options);
        var restoredPoint = pointConverter.Read(pointValue!, options);
        var restoredColor = colorConverter.Read(colorValue!, options);

        // Assert
        Assert.Equal(point, restoredPoint);
        Assert.Equal(color, restoredColor);
    }

    [Fact]
    public void Converter_WithComplexTransformation_WorksCorrectly()
    {
        // Arrange - Temperature converter does Fahrenheit to Celsius conversion
        var converter = new TemperatureConverter();
        var options = new ToonSerializerOptions();
        var fahrenheit = new Temperature(212, "F"); // Boiling point

        // Act
        var written = converter.Write(fahrenheit, options);
        var restored = converter.Read(written!, options);

        // Assert - Should be stored and read as Celsius
        Assert.NotNull(restored);
        Assert.Equal(100, restored!.Value, 0.1); // ~100°C
        Assert.Equal("C", restored.Unit);
    }

    #endregion
}