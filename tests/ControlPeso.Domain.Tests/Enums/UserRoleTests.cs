using ControlPeso.Domain.Enums;
using FluentAssertions;

namespace ControlPeso.Domain.Tests.Enums;

public class UserRoleTests
{
    [Fact]
    public void UserRole_User_ShouldHaveValue0()
    {
        // Act
        var value = (int)UserRole.User;

        // Assert
        value.Should().Be(0);
    }

    [Fact]
    public void UserRole_Administrator_ShouldHaveValue1()
    {
        // Act
        var value = (int)UserRole.Administrator;

        // Assert
        value.Should().Be(1);
    }

    [Fact]
    public void UserRole_ShouldHaveTwoValues()
    {
        // Act
        var values = Enum.GetValues<UserRole>();

        // Assert
        values.Should().HaveCount(2);
    }

    [Fact]
    public void UserRole_ShouldBeAssignableFromInt()
    {
        // Act
        var role = (UserRole)0;

        // Assert
        role.Should().Be(UserRole.User);
    }
}
