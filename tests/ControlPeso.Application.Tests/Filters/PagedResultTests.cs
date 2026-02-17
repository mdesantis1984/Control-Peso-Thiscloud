using ControlPeso.Application.Filters;
using FluentAssertions;

namespace ControlPeso.Application.Tests.Filters;

public sealed class PagedResultTests
{
    [Fact]
    public void TotalPages_WithExactDivision_ReturnsCorrectCount()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Items = new[] { "A", "B" },
            Page = 1,
            PageSize = 2,
            TotalCount = 10
        };
        
        // Act
        var totalPages = result.TotalPages;
        
        // Assert
        totalPages.Should().Be(5); // 10 / 2 = 5
    }
    
    [Fact]
    public void TotalPages_WithRemainder_RoundsUp()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Items = new[] { "A", "B" },
            Page = 1,
            PageSize = 3,
            TotalCount = 10
        };
        
        // Act
        var totalPages = result.TotalPages;
        
        // Assert
        totalPages.Should().Be(4); // Ceiling(10 / 3) = 4
    }
    
    [Fact]
    public void TotalPages_WithZeroItems_ReturnsZero()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Items = Array.Empty<string>(),
            Page = 1,
            PageSize = 10,
            TotalCount = 0
        };
        
        // Act
        var totalPages = result.TotalPages;
        
        // Assert
        totalPages.Should().Be(0);
    }
    
    [Fact]
    public void HasPreviousPage_WhenOnFirstPage_ReturnsFalse()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Items = new[] { "A" },
            Page = 1,
            PageSize = 10,
            TotalCount = 50
        };
        
        // Act & Assert
        result.HasPreviousPage.Should().BeFalse();
    }
    
    [Fact]
    public void HasPreviousPage_WhenOnSecondPage_ReturnsTrue()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Items = new[] { "A" },
            Page = 2,
            PageSize = 10,
            TotalCount = 50
        };
        
        // Act & Assert
        result.HasPreviousPage.Should().BeTrue();
    }
    
    [Fact]
    public void HasNextPage_WhenOnLastPage_ReturnsFalse()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Items = new[] { "A" },
            Page = 5,
            PageSize = 10,
            TotalCount = 50
        };
        
        // Act & Assert
        result.HasNextPage.Should().BeFalse();
    }
    
    [Fact]
    public void HasNextPage_WhenNotOnLastPage_ReturnsTrue()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Items = new[] { "A" },
            Page = 1,
            PageSize = 10,
            TotalCount = 50
        };
        
        // Act & Assert
        result.HasNextPage.Should().BeTrue();
    }
    
    [Fact]
    public void PagedResult_WithSingleItem_ReturnsCorrectProperties()
    {
        // Arrange & Act
        var result = new PagedResult<int>
        {
            Items = new[] { 42 },
            Page = 1,
            PageSize = 1,
            TotalCount = 1
        };
        
        // Assert
        result.Items.Should().ContainSingle().Which.Should().Be(42);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(1);
        result.TotalCount.Should().Be(1);
        result.TotalPages.Should().Be(1);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
    }
}
