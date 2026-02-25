using System.Globalization;
using System.Text;

namespace ControlPeso.Application.Services;

/// <summary>
/// Helper service for generating avatar fallbacks when user has no profile photo.
/// Generates initials and deterministic colors based on user name.
/// </summary>
public static class AvatarHelper
{
    private static readonly string[] MaterialColors =
    [
        "#F44336", "#E91E63", "#9C27B0", "#673AB7", "#3F51B5",
        "#2196F3", "#03A9F4", "#00BCD4", "#009688", "#4CAF50",
        "#8BC34A", "#CDDC39", "#FFC107", "#FF9800", "#FF5722"
    ];

    /// <summary>
    /// Generates initials from a user's full name.
    /// Returns first letter of first name + first letter of last name (max 2 chars).
    /// </summary>
    /// <param name="fullName">User's full name</param>
    /// <returns>Uppercase initials (1-2 characters)</returns>
    public static string GetInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "?";
        }

        // Remove diacritics and normalize
        var normalized = RemoveDiacritics(fullName.Trim());

        // Split by whitespace and filter empty entries
        var parts = normalized
            .Split([' ', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        if (parts.Length == 0)
        {
            return "?";
        }

        if (parts.Length == 1)
        {
            // Single name: use first character
            return parts[0][0].ToString().ToUpperInvariant();
        }

        // Multiple names: first char of first name + first char of last name
        var firstName = parts[0];
        var lastName = parts[^1]; // Last element

        return $"{firstName[0]}{lastName[0]}".ToUpperInvariant();
    }

    /// <summary>
    /// Generates a deterministic color from a user's name using hash-based selection.
    /// Same name always returns the same color (for consistency across sessions).
    /// </summary>
    /// <param name="fullName">User's full name</param>
    /// <returns>Material Design color hex code</returns>
    public static string GetColorFromName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return MaterialColors[0]; // Default to first color
        }

        // Generate hash from name (case-insensitive)
        var hash = fullName.ToLowerInvariant().GetHashCode();
        
        // Map to positive index
        var index = Math.Abs(hash) % MaterialColors.Length;

        return MaterialColors[index];
    }

    /// <summary>
    /// Removes diacritics (accents) from a string for normalization.
    /// Converts "José María" → "Jose Maria"
    /// </summary>
    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }
}
