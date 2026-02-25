using ControlPeso.Application.Filters;
using ControlPeso.Domain.Enums;
using FluentAssertions;

namespace ControlPeso.Application.Tests.Filters;

/// <summary>
/// Tests for UserFilter record.
/// Validates property initialization and default values.
/// </summary>
public sealed class UserFilterTests
{
    [Fact]
    public void UserFilter_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var filter = new UserFilter();

        // Assert
        filter.SearchTerm.Should().BeNull();
        filter.Role.Should().BeNull();
        filter.Status.Should().BeNull();
        filter.Language.Should().BeNull();
        filter.Page.Should().Be(1);
        filter.PageSize.Should().Be(20);
        filter.SortBy.Should().Be("MemberSince");
        filter.Descending.Should().BeTrue();
    }

    [Fact]
    public void UserFilter_CanBeCreatedWithCustomValues()
    {
        // Arrange & Act
        var filter = new UserFilter
        {
            SearchTerm = "test@example.com",
            Role = UserRole.Administrator,
            Status = UserStatus.Active,
            Language = "en",
            Page = 2,
            PageSize = 50,
            SortBy = "Name",
            Descending = false
        };

        // Assert
        filter.SearchTerm.Should().Be("test@example.com");
        filter.Role.Should().Be(UserRole.Administrator);
        filter.Status.Should().Be(UserStatus.Active);
        filter.Language.Should().Be("en");
        filter.Page.Should().Be(2);
        filter.PageSize.Should().Be(50);
        filter.SortBy.Should().Be("Name");
        filter.Descending.Should().BeFalse();
    }

    [Theory]
    [InlineData(UserRole.User)]
    [InlineData(UserRole.Administrator)]
    public void UserFilter_SupportsAllRoles(UserRole role)
    {
        // Arrange & Act
        var filter = new UserFilter { Role = role };

        // Assert
        filter.Role.Should().Be(role);
    }

    [Theory]
    [InlineData(UserStatus.Active)]
    [InlineData(UserStatus.Inactive)]
    [InlineData(UserStatus.Pending)]
    public void UserFilter_SupportsAllStatuses(UserStatus status)
    {
        // Arrange & Act
        var filter = new UserFilter { Status = status };

        // Assert
        filter.Status.Should().Be(status);
    }

    [Fact]
    public void UserFilter_SearchTermCanBeNull()
    {
        // Arrange & Act
        var filter = new UserFilter { SearchTerm = null };

        // Assert
        filter.SearchTerm.Should().BeNull();
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("Email")]
    [InlineData("MemberSince")]
    [InlineData("Status")]
    public void UserFilter_SupportsDifferentSortByValues(string sortBy)
    {
        // Arrange & Act
        var filter = new UserFilter { SortBy = sortBy };

        // Assert
        filter.SortBy.Should().Be(sortBy);
    }

    [Fact]
    public void UserFilter_IsRecord_SupportsWithExpression()
    {
        // Arrange
        var original = new UserFilter
        {
            SearchTerm = "test",
            Page = 1
        };

        // Act
        var modified = original with { Page = 2 };

        // Assert
        original.Page.Should().Be(1); // Original unchanged
        modified.Page.Should().Be(2);
        modified.SearchTerm.Should().Be("test"); // Other properties copied
    }

    [Fact]
    public void UserFilter_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var filter1 = new UserFilter { SearchTerm = "test", Page = 1 };
        var filter2 = new UserFilter { SearchTerm = "test", Page = 1 };
        var filter3 = new UserFilter { SearchTerm = "test", Page = 2 };

        // Assert
        filter1.Should().Be(filter2); // Same values
        filter1.Should().NotBe(filter3); // Different Page
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(1, 20)]
    [InlineData(5, 50)]
    [InlineData(10, 100)]
    public void UserFilter_SupportsVariousPaginationSettings(int page, int pageSize)
    {
        // Arrange & Act
        var filter = new UserFilter { Page = page, PageSize = pageSize };

        // Assert
        filter.Page.Should().Be(page);
        filter.PageSize.Should().Be(pageSize);
    }

    [Theory]
    [InlineData("es")]
    [InlineData("en")]
    [InlineData("pt")]
    public void UserFilter_SupportsLanguageFiltering(string language)
    {
        // Arrange & Act
        var filter = new UserFilter { Language = language };

        // Assert
        filter.Language.Should().Be(language);
    }
}
