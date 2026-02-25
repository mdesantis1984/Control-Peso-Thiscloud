using ControlPeso.Application.Services;

namespace ControlPeso.Application.Tests.Services;

/// <summary>
/// Tests unitarios para AvatarHelper - helper estático para generación de avatares fallback.
/// </summary>
public sealed class AvatarHelperTests
{
    [Theory]
    [InlineData("Marco De Santis", "MS")] // First + Last (Santis, not De)
    [InlineData("Juan Pérez", "JP")]
    [InlineData("José María García", "JG")] // First + Last (García)
    [InlineData("Ana", "A")]
    [InlineData("  Spaces  Around  ", "SA")]
    [InlineData("", "?")]
    [InlineData(null, "?")]
    [InlineData("   ", "?")]
    [InlineData("A B C D", "AD")] // First + Last
    public void GetInitials_WithVariousNames_ShouldReturnExpectedInitials(string? fullName, string expected)
    {
        // Act
        var result = AvatarHelper.GetInitials(fullName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetInitials_WithDiacritics_ShouldRemoveAccents()
    {
        // Arrange
        var nameWithAccents = "José María";

        // Act
        var result = AvatarHelper.GetInitials(nameWithAccents);

        // Assert
        Assert.Equal("JM", result);
    }

    [Fact]
    public void GetInitials_WithSingleCharacterName_ShouldReturnSingleLetter()
    {
        // Arrange
        var singleChar = "X";

        // Act
        var result = AvatarHelper.GetInitials(singleChar);

        // Assert
        Assert.Equal("X", result);
    }

    [Fact]
    public void GetInitials_WithMultipleSpaces_ShouldIgnoreExtraSpaces()
    {
        // Arrange
        var nameWithSpaces = "Marco    De     Santis";

        // Act
        var result = AvatarHelper.GetInitials(nameWithSpaces);

        // Assert - First + Last (Santis)
        Assert.Equal("MS", result);
    }

    [Theory]
    [InlineData("Marco De Santis")]
    [InlineData("Juan Pérez")]
    [InlineData("Test User")]
    public void GetColorFromName_WithSameName_ShouldReturnSameColor(string fullName)
    {
        // Act
        var color1 = AvatarHelper.GetColorFromName(fullName);
        var color2 = AvatarHelper.GetColorFromName(fullName);

        // Assert
        Assert.Equal(color1, color2); // Deterministic: same name → same color
    }

    [Fact]
    public void GetColorFromName_WithDifferentNames_ShouldReturnValidHexColors()
    {
        // Arrange
        var names = new[] { "Marco De Santis", "Juan Pérez", "Ana García", "Test User" };

        // Act
        var colors = names.Select(AvatarHelper.GetColorFromName).ToArray();

        // Assert
        Assert.All(colors, color =>
        {
            Assert.StartsWith("#", color);
            Assert.Equal(7, color.Length); // #RRGGBB format
        });
    }

    [Fact]
    public void GetColorFromName_WithNullOrEmpty_ShouldReturnDefaultColor()
    {
        // Act
        var colorNull = AvatarHelper.GetColorFromName(null);
        var colorEmpty = AvatarHelper.GetColorFromName("");
        var colorWhitespace = AvatarHelper.GetColorFromName("   ");

        // Assert
        Assert.Equal(colorNull, colorEmpty);
        Assert.Equal(colorEmpty, colorWhitespace);
        Assert.StartsWith("#", colorNull); // Should still be valid hex color
    }

    [Fact]
    public void GetColorFromName_WithCaseVariations_ShouldReturnSameColor()
    {
        // Arrange
        var nameLower = "marco de santis";
        var nameUpper = "MARCO DE SANTIS";
        var nameMixed = "Marco De Santis";

        // Act
        var colorLower = AvatarHelper.GetColorFromName(nameLower);
        var colorUpper = AvatarHelper.GetColorFromName(nameUpper);
        var colorMixed = AvatarHelper.GetColorFromName(nameMixed);

        // Assert
        // GetColorFromName usa ToLowerInvariant() así que deberían ser iguales
        Assert.Equal(colorLower, colorUpper);
        Assert.Equal(colorUpper, colorMixed);
    }

    [Fact]
    public void GetInitials_WithUpperAndLowerCase_ShouldReturnUpperCase()
    {
        // Arrange
        var nameLower = "marco de santis";
        var nameUpper = "MARCO DE SANTIS";

        // Act
        var resultLower = AvatarHelper.GetInitials(nameLower);
        var resultUpper = AvatarHelper.GetInitials(nameUpper);

        // Assert - First + Last (santis/SANTIS)
        Assert.Equal("MS", resultLower);
        Assert.Equal("MS", resultUpper);
    }

    [Fact]
    public void GetInitials_WithTabsAndNewlines_ShouldHandleWhitespace()
    {
        // Arrange
        var nameWithWhitespace = "Marco\tDe\nSantis";

        // Act
        var result = AvatarHelper.GetInitials(nameWithWhitespace);

        // Assert - First + Last (Santis)
        Assert.Equal("MS", result);
    }
}
