using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Application.Services;
using ControlPeso.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace ControlPeso.Infrastructure.Tests.Postgres;

/// <summary>
/// WeightLogService against a real PostgreSQL 15 database created by the EF Core migrations.
/// </summary>
public sealed class PostgresWeightLogServiceTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public PostgresWeightLogServiceTests(PostgresFixture fixture) => _fixture = fixture;

    private async Task<(WeightLogService Service, Guid UserId)> CreateServiceWithUserAsync()
    {
        var connectionString = await _fixture.CreateMigratedDatabaseAsync();

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var users = new UserService(
            new PostgresDbContextFactory(connectionString),
            cache,
            NullLogger<UserService>.Instance);
        var user = await users.CreateOrUpdateFromGoogleAsync(new GoogleUserInfo
        {
            GoogleId = $"google-{Guid.NewGuid():N}",
            Name = "Weight Tester",
            Email = $"{Guid.NewGuid():N}@example.com"
        });

        // The service receives a DbContext today; if it moves to IDbContextFactory this is the only line to adapt.
        var service = new WeightLogService(
            PostgresFixture.CreateContext(connectionString),
            NullLogger<WeightLogService>.Instance);
        return (service, user.Id);
    }

    private static CreateWeightLogDto NewLog(Guid userId, DateOnly date, decimal weight) => new()
    {
        UserId = userId,
        Date = date,
        Time = new TimeOnly(8, 30),
        Weight = weight,
        DisplayUnit = WeightUnit.Kg,
        Note = $"log {date:yyyy-MM-dd}"
    };

    [PostgresFact]
    public async Task CreateAsync_WithValidData_ShouldPersistAndBeReadable()
    {
        var (service, userId) = await CreateServiceWithUserAsync();

        var created = await service.CreateAsync(NewLog(userId, new DateOnly(2026, 1, 10), 80.5m));
        var loaded = await service.GetByIdAsync(created.Id);

        loaded.Should().NotBeNull();
        loaded!.Weight.Should().Be(80.5m);
        loaded.Date.Should().Be(new DateOnly(2026, 1, 10));
        loaded.Time.Should().Be(new TimeOnly(8, 30));
    }

    [PostgresFact]
    public async Task GetByUserAsync_WithPagination_ShouldReturnPageAndTotalCount()
    {
        var (service, userId) = await CreateServiceWithUserAsync();
        for (var day = 1; day <= 5; day++)
        {
            await service.CreateAsync(NewLog(userId, new DateOnly(2026, 2, day), 80m - day));
        }

        var page = await service.GetByUserAsync(userId, new WeightLogFilter { UserId = userId, Page = 2, PageSize = 2 });

        page.TotalCount.Should().Be(5);
        page.Items.Should().HaveCount(2);
        page.Items.Select(i => i.Date).Should().Equal(new DateOnly(2026, 2, 3), new DateOnly(2026, 2, 2));
    }

    [PostgresFact]
    public async Task GetByUserAsync_WithDateRangeAndSearchTerm_ShouldFilterRecords()
    {
        var (service, userId) = await CreateServiceWithUserAsync();
        await service.CreateAsync(NewLog(userId, new DateOnly(2026, 3, 1), 80m));
        await service.CreateAsync(NewLog(userId, new DateOnly(2026, 3, 15), 79m));
        await service.CreateAsync(NewLog(userId, new DateOnly(2026, 4, 1), 78m));

        var ranged = await service.GetByUserAsync(userId, new WeightLogFilter
        {
            UserId = userId,
            DateRange = new DateRange { StartDate = new DateOnly(2026, 3, 1), EndDate = new DateOnly(2026, 3, 31) }
        });
        var searched = await service.GetByUserAsync(userId, new WeightLogFilter { UserId = userId, SearchTerm = "2026-04" });

        ranged.TotalCount.Should().Be(2);
        searched.TotalCount.Should().Be(1);
    }
}
