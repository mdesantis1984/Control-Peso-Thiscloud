using ControlPeso.Domain.Enums;

namespace ControlPeso.Web.Models;

/// <summary>
/// Model for Profile page form binding with MudBlazor components.
/// Represents the editable state of user profile fields.
/// </summary>
public sealed class ProfileFormModel
{
    // ========================================================================
    // PERSONAL DETAILS
    // ========================================================================
    
    public string Name { get; set; } = string.Empty;
    
    public decimal Height { get; set; } = 170m; // Default: 170 cm
    
    public DateTime? DateOfBirth { get; set; }
    
    public decimal? GoalWeight { get; set; }
    
    public UnitSystem UnitSystem { get; set; } = UnitSystem.Metric;
    
    public string Language { get; set; } = "es";
    
    // ========================================================================
    // PREFERENCES
    // ========================================================================
    
    public bool DarkMode { get; set; } = true;
    
    public bool NotificationsEnabled { get; set; } = true;
    
    // ========================================================================
    // METHODS
    // ========================================================================
    
    /// <summary>
    /// Creates a new instance with default values.
    /// </summary>
    public static ProfileFormModel CreateDefault() => new();
    
    /// <summary>
    /// Creates a shallow copy of this instance.
    /// </summary>
    public ProfileFormModel Clone() => (ProfileFormModel)MemberwiseClone();
}
