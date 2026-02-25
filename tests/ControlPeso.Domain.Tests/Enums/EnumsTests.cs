using ControlPeso.Domain.Enums;
using FluentAssertions;

namespace ControlPeso.Domain.Tests.Enums;

public class UnitSystemTests
{
    [Fact]
    public void UnitSystem_Metric_ShouldHaveValue0()
    {
        ((int)UnitSystem.Metric).Should().Be(0);
    }

    [Fact]
    public void UnitSystem_Imperial_ShouldHaveValue1()
    {
        ((int)UnitSystem.Imperial).Should().Be(1);
    }

    [Fact]
    public void UnitSystem_ShouldHaveTwoValues()
    {
        Enum.GetValues<UnitSystem>().Should().HaveCount(2);
    }
}

public class WeightUnitTests
{
    [Fact]
    public void WeightUnit_Kg_ShouldHaveValue0()
    {
        ((int)WeightUnit.Kg).Should().Be(0);
    }

    [Fact]
    public void WeightUnit_Lb_ShouldHaveValue1()
    {
        ((int)WeightUnit.Lb).Should().Be(1);
    }

    [Fact]
    public void WeightUnit_ShouldHaveTwoValues()
    {
        Enum.GetValues<WeightUnit>().Should().HaveCount(2);
    }
}

public class WeightTrendTests
{
    [Fact]
    public void WeightTrend_Up_ShouldHaveValue0()
    {
        ((int)WeightTrend.Up).Should().Be(0);
    }

    [Fact]
    public void WeightTrend_Down_ShouldHaveValue1()
    {
        ((int)WeightTrend.Down).Should().Be(1);
    }

    [Fact]
    public void WeightTrend_Neutral_ShouldHaveValue2()
    {
        ((int)WeightTrend.Neutral).Should().Be(2);
    }

    [Fact]
    public void WeightTrend_ShouldHaveThreeValues()
    {
        Enum.GetValues<WeightTrend>().Should().HaveCount(3);
    }
}

public class NotificationSeverityTests
{
    [Fact]
    public void NotificationSeverity_Normal_ShouldHaveValue0()
    {
        ((int)NotificationSeverity.Normal).Should().Be(0);
    }

    [Fact]
    public void NotificationSeverity_Info_ShouldHaveValue1()
    {
        ((int)NotificationSeverity.Info).Should().Be(1);
    }

    [Fact]
    public void NotificationSeverity_Success_ShouldHaveValue2()
    {
        ((int)NotificationSeverity.Success).Should().Be(2);
    }

    [Fact]
    public void NotificationSeverity_Warning_ShouldHaveValue3()
    {
        ((int)NotificationSeverity.Warning).Should().Be(3);
    }

    [Fact]
    public void NotificationSeverity_Error_ShouldHaveValue4()
    {
        ((int)NotificationSeverity.Error).Should().Be(4);
    }

    [Fact]
    public void NotificationSeverity_ShouldHaveFiveValues()
    {
        Enum.GetValues<NotificationSeverity>().Should().HaveCount(5);
    }
}
