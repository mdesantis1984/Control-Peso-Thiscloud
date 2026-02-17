using ControlPeso.Application.Filters;
using FluentAssertions;

namespace ControlPeso.Application.Tests.Filters;

public sealed class DateRangeTests
{
    [Fact]
    public void DaysInRange_WithSameDate_ReturnsOne()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 15);
        var range = new DateRange
        {
            StartDate = date,
            EndDate = date
        };
        
        // Act
        var days = range.DaysInRange;
        
        // Assert
        days.Should().Be(1);
    }
    
    [Fact]
    public void DaysInRange_WithMultipleDays_ReturnsCorrectCount()
    {
        // Arrange
        var range = new DateRange
        {
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 1, 7)
        };
        
        // Act
        var days = range.DaysInRange;
        
        // Assert
        days.Should().Be(7); // Inclusive: 1, 2, 3, 4, 5, 6, 7
    }
    
    [Fact]
    public void IsValid_WhenStartBeforeEnd_ReturnsTrue()
    {
        // Arrange
        var range = new DateRange
        {
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 1, 31)
        };
        
        // Act & Assert
        range.IsValid.Should().BeTrue();
    }
    
    [Fact]
    public void IsValid_WhenStartEqualsEnd_ReturnsTrue()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 15);
        var range = new DateRange
        {
            StartDate = date,
            EndDate = date
        };
        
        // Act & Assert
        range.IsValid.Should().BeTrue();
    }
    
    [Fact]
    public void IsValid_WhenStartAfterEnd_ReturnsFalse()
    {
        // Arrange
        var range = new DateRange
        {
            StartDate = new DateOnly(2025, 1, 31),
            EndDate = new DateOnly(2025, 1, 1)
        };
        
        // Act & Assert
        range.IsValid.Should().BeFalse();
    }
    
    [Fact]
    public void LastDays_WithValidDays_ReturnsCorrectRange()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        // Act
        var range = DateRange.LastDays(7);
        
        // Assert
        range.EndDate.Should().Be(today);
        range.StartDate.Should().Be(today.AddDays(-7));
        range.DaysInRange.Should().Be(8); // 7 días atrás + hoy = 8 días inclusive
    }
    
    [Fact]
    public void LastDays_WithZero_ThrowsArgumentOutOfRangeException()
    {
        // Act
        Action act = () => DateRange.LastDays(0);
        
        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
    
    [Fact]
    public void LastDays_WithNegative_ThrowsArgumentOutOfRangeException()
    {
        // Act
        Action act = () => DateRange.LastDays(-5);
        
        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
    
    [Fact]
    public void CurrentMonth_ReturnsFirstAndLastDayOfMonth()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expectedStart = new DateOnly(today.Year, today.Month, 1);
        var expectedEnd = expectedStart.AddMonths(1).AddDays(-1);
        
        // Act
        var range = DateRange.CurrentMonth();
        
        // Assert
        range.StartDate.Should().Be(expectedStart);
        range.EndDate.Should().Be(expectedEnd);
        range.IsValid.Should().BeTrue();
    }
    
    [Theory]
    [InlineData(1, 31)]  // January
    [InlineData(2, 28)]  // February (non-leap year 2025)
    [InlineData(4, 30)]  // April
    [InlineData(12, 31)] // December
    public void CurrentMonth_HandlesMonthsCorrectly(int month, int expectedDays)
    {
        // Arrange - Force a specific month using reflection/manual construction
        var firstDay = new DateOnly(2025, month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        
        var range = new DateRange
        {
            StartDate = firstDay,
            EndDate = lastDay
        };
        
        // Act
        var days = range.DaysInRange;
        
        // Assert
        days.Should().Be(expectedDays);
    }
}
