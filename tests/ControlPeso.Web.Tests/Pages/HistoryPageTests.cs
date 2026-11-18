using System.Security.Claims;
using Bunit;
using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Enums;
using ControlPeso.Web.Pages;
using ControlPeso.Web.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;

namespace ControlPeso.Web.Tests.Pages;

/// <summary>
/// S9: the History stats panel must show the real total and avg/min/max over ALL filtered records,
/// not just the current page, and must refresh after each grid load.
/// </summary>
public sealed class HistoryPageTests : TestContext
{
    private static readonly Guid UserId = Guid.NewGuid();
    private readonly IWeightLogService _weightLogService = Substitute.For<IWeightLogService>();
    private readonly List<WeightLogFilter> _filters = [];

    public HistoryPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var localizer = Substitute.For<IStringLocalizer<History>>();
        localizer[Arg.Any<string>()].Returns(c => new LocalizedString(c.Arg<string>(), c.Arg<string>()));
        localizer[Arg.Any<string>(), Arg.Any<object[]>()].Returns(c => new LocalizedString(c.Arg<string>(), c.Arg<string>()));

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, UserId.ToString())], "test");
        var auth = Substitute.For<AuthenticationStateProvider>();
        auth.GetAuthenticationStateAsync().Returns(new AuthenticationState(new ClaimsPrincipal(identity)));

        var notification = new NotificationService(
            Substitute.For<ISnackbar>(),
            Substitute.For<IUserPreferencesService>(),
            Substitute.For<IUserNotificationService>(),
            auth,
            NullLogger<NotificationService>.Instance);

        Services.AddSingleton(localizer);
        Services.AddSingleton(_weightLogService);
        Services.AddSingleton(auth);
        Services.AddSingleton(notification);
        Services.AddSingleton(new UserStateService(NullLogger<UserStateService>.Instance));
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<History>>(NullLogger<History>.Instance);
        Services.AddMudServices();
    }

    private static WeightLogDto Log(int dayOffset, decimal weight) => new()
    {
        Id = Guid.NewGuid(),
        UserId = UserId,
        Date = new DateOnly(2026, 4, 1).AddDays(dayOffset),
        Time = new TimeOnly(8, 0),
        Weight = weight,
        DisplayUnit = WeightUnit.Kg,
        Trend = WeightTrend.Neutral,
        CreatedAt = DateTime.UtcNow
    };

    private void SetupService(IReadOnlyList<WeightLogDto> all)
    {
        _weightLogService
            .GetByUserAsync(UserId, Arg.Any<WeightLogFilter>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var filter = call.ArgAt<WeightLogFilter>(1);
                _filters.Add(filter);
                var items = all.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();
                return new PagedResult<WeightLogDto>
                {
                    Items = items,
                    TotalCount = all.Count,
                    Page = filter.Page,
                    PageSize = filter.PageSize
                };
            });
    }

    private static string StatValue(IRenderedComponent<History> cut, string labelKey) =>
        cut.FindAll("p, h6")
            .SkipWhile(e => e.TextContent.Trim() != labelKey)
            .Skip(1)
            .First()
            .TextContent.Trim();

    [Fact]
    public void StatsPanel_ShouldShowTotalAndAggregatesOverAllRecords_NotJustCurrentPage()
    {
        // 14 records: weights 80.0 .. 93.0 (+ one 70.0) -> page size of the grid is 10
        var all = Enumerable.Range(0, 13).Select(i => Log(i, 80m + i)).Append(Log(13, 70m)).ToList();
        SetupService(all);

        RenderComponent<MudPopoverProvider>();
        var cut = RenderComponent<History>();

        cut.WaitForAssertion(() => StatValue(cut, "TotalRecordsLabel").Should().Be("14"));
        StatValue(cut, "MinWeightLabel").Should().StartWith(70m.ToString("F1"));
        StatValue(cut, "MaxWeightLabel").Should().StartWith(92m.ToString("F1"));
        var expectedAverage = all.Average(x => x.Weight);
        StatValue(cut, "AverageWeightLabel").Should().StartWith(expectedAverage.ToString("F1"));
    }

    [Fact]
    public void StatsPanel_ShouldApplyCurrentFiltersToTheAggregateQuery()
    {
        var all = Enumerable.Range(0, 14).Select(i => Log(i, 80m + i)).ToList();
        SetupService(all);

        RenderComponent<MudPopoverProvider>();
        var cut = RenderComponent<History>();
        cut.WaitForAssertion(() => StatValue(cut, "TotalRecordsLabel").Should().Be("14"));

        // The aggregate query must cover every record matching the filter (no paging truncation).
        _filters.Should().Contain(f => f.PageSize >= 14 && f.Page == 1 && f.UserId == UserId);
    }
}
