using ControlPeso.Domain.Enums;
using FluentAssertions;

namespace ControlPeso.Domain.Tests.Enums;

public class UserStatusTests
{
    [Fact]
    public void UserStatus_Active_ShouldHaveValue0()
    {
        ((int)UserStatus.Active).Should().Be(0);
    }

    [Fact]
    public void UserStatus_Inactive_ShouldHaveValue1()
    {
        ((int)UserStatus.Inactive).Should().Be(1);
    }

    [Fact]
    public void UserStatus_Pending_ShouldHaveValue2()
    {
        ((int)UserStatus.Pending).Should().Be(2);
    }

    [Fact]
    public void UserStatus_ShouldHaveThreeValues()
    {
        Enum.GetValues<UserStatus>().Should().HaveCount(3);
    }
}
