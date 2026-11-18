using System.Globalization;
using System.Security.Claims;
using Bunit;
using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Enums;
using ControlPeso.Shared.Resources.Extensions;
using ControlPeso.Web.Pages;
using ControlPeso.Web.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;

namespace ControlPeso.Web.Tests.Pages;

/// <summary>
/// S11: Dashboard labels follow the user's display unit and numeric deltas use the current culture.
/// </summary>
public sealed class DashboardPageTests : TestContext
{
    private static readonly Guid UserId = Guid.NewGuid();
    private readonly CultureInfo _previousCulture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _previousUiCulture = CultureInfo.CurrentUICulture;
    private readonly UserStateService _userState = new(NullLogger<UserStateService>.Instance);

    public DashboardPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var today = DateOnly.FromDateTime(DateTime.Now);
        var weightLogs = Substitute.For<IWeightLogService>();
        weightLogs.GetByUserAsync(UserId, Arg.Any<WeightLogFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<WeightLogDto>
            {
                Items =
                [
                    Log(today, 80.4m, WeightTrend.Up),
                    Log(today.AddDays(-1), 80.0m, WeightTrend.Down),
                    Log(today.AddDays(-2), 80.2m, WeightTrend.Neutral)
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 30
            });
        weightLogs.GetStatsAsync(UserId, Arg.Any<ControlPeso.Application.Filters.DateRange>(), Arg.Any<CancellationToken>())
            .Returns(new WeightStatsDto { UserId = UserId, StartDate = today, EndDate = today });

        var users = Substitute.For<IUserService>();
        users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(new UserDto
        {
            Id = UserId,
            GoogleId = "g",
            Name = "Test",
            Email = "t@example.com",
            Role = UserRole.User,
            MemberSince = DateTime.UtcNow,
            Height = 175,
            UnitSystem = UnitSystem.Metric,
            Language = "es-AR",
            Status = UserStatus.Active,
            GoalWeight = 75m,
            StartingWeight = 85m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, UserId.ToString())], "test");
        var auth = Substitute.For<AuthenticationStateProvider>();
        auth.GetAuthenticationStateAsync().Returns(new AuthenticationState(new ClaimsPrincipal(identity)));

        var notification = new NotificationService(
            Substitute.For<ISnackbar>(),
            Substitute.For<IUserPreferencesService>(),
            Substitute.For<IUserNotificationService>(),
            auth,
            NullLogger<NotificationService>.Instance);

        Services.AddSharedResourcesLocalization();
        Services.AddLogging();
        Services.AddSingleton(weightLogs);
        Services.AddSingleton(users);
        Services.AddSingleton(auth);
        Services.AddSingleton(notification);
        Services.AddSingleton(_userState);
        Services.AddMudServices();
    }

    private static WeightLogDto Log(DateOnly date, decimal weight, WeightTrend trend) => new()
    {
        Id = Guid.NewGuid(),
        UserId = UserId,
        Date = date,
        Time = new TimeOnly(8, 0),
        Weight = weight,
        DisplayUnit = WeightUnit.Kg,
        Trend = trend,
        CreatedAt = DateTime.UtcNow
    };

    private IRenderedComponent<Dashboard> RenderDashboard(string culture, UnitSystem unit)
    {
        var info = CultureInfo.GetCultureInfo(culture);
        CultureInfo.CurrentCulture = info;
        CultureInfo.CurrentUICulture = info;
        _userState.SetCurrentUnitSystem(unit);

        RenderComponent<MudPopoverProvider>();
        var cut = RenderComponent<Dashboard>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("mud-table"));
        return cut;
    }

    [Theory]
    [InlineData("es-AR", UnitSystem.Metric, "PESO (kg)", "kg")]
    [InlineData("es-AR", UnitSystem.Imperial, "PESO (lb)", "lb")]
    [InlineData("en-US", UnitSystem.Metric, "WEIGHT (kg)", "kg")]
    [InlineData("en-US", UnitSystem.Imperial, "WEIGHT (lb)", "lb")]
    public void Labels_ShouldFollowTheUsersDisplayUnit(string culture, UnitSystem unit, string header, string unitLabel)
    {
        var cut = RenderDashboard(culture, unit);

        cut.Markup.Should().Contain(header);
        cut.Markup.ToUpperInvariant().Should().NotContain("LBS", because: "the unit is never hard-coded to pounds");
        if (unit == UnitSystem.Metric)
        {
            cut.Markup.Should().NotContain("(lb)");
        }
        cut.Markup.Should().Contain(unitLabel);
    }

    [Fact]
    public void GoalRemainingLabel_ShouldShowTheUnitExactlyOnce()
    {
        var cut = RenderDashboard("es-AR", UnitSystem.Metric);

        // 80.4 kg current - 75 kg goal = 5,4 kg remaining
        cut.Markup.Should().Contain("PARA META: 5,4 kg RESTANTES");
    }

    [Theory]
    [InlineData("es-AR", "+0,4", "-0,2")]
    [InlineData("en-US", "+0.4", "-0.2")]
    public void TrendDelta_ShouldBeComputedFromThePreviousRecordWithTheCurrentCulture(string culture, string up, string down)
    {
        // Rows: 80.4 (+0.4 vs 80.0), 80.0 (-0.2 vs 80.2), 80.2 (no previous record). The weekly card shows +0.2.
        var cut = RenderDashboard(culture, UnitSystem.Metric);

        cut.Markup.Should().Contain(up);
        cut.Markup.Should().Contain(down);
    }
}
