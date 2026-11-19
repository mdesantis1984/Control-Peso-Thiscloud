using ControlPeso.Application.DTOs;
using ControlPeso.Application.Services;
using ControlPeso.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace ControlPeso.Infrastructure.Tests.Postgres;

/// <summary>
/// UserService against a real PostgreSQL 15 database created by the EF Core migrations.
/// </summary>
public sealed class PostgresUserServiceTests : IClassFixture<PostgresFixture>, IDisposable
{
    private readonly PostgresFixture _fixture;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public PostgresUserServiceTests(PostgresFixture fixture) => _fixture = fixture;

    public void Dispose() => _cache.Dispose();

    private async Task<(UserService Service, string ConnectionString)> CreateServiceAsync()
    {
        var connectionString = await _fixture.CreateMigratedDatabaseAsync();
        var service = new UserService(
            new PostgresDbContextFactory(connectionString),
            _cache,
            NullLogger<UserService>.Instance);
        return (service, connectionString);
    }

    [PostgresFact]
    public async Task CreateOrUpdateFromGoogleAsync_WithNewUser_ShouldPersistActiveUser()
    {
        var (service, connectionString) = await CreateServiceAsync();

        var created = await service.CreateOrUpdateFromGoogleAsync(new GoogleUserInfo
        {
            GoogleId = "google-1",
            Name = "Ada Lovelace",
            Email = "ada@example.com"
        });

        created.Id.Should().NotBe(Guid.Empty);
        await using var context = PostgresFixture.CreateContext(connectionString);
        var stored = await context.Set<Users>().SingleAsync();
        stored.Id.Should().Be(created.Id);
        stored.GoogleId.Should().Be("google-1");
        stored.Email.Should().Be("ada@example.com");
    }

    [PostgresFact]
    public async Task CreateOrUpdateFromOAuthAsync_WithSameExternalIdAndEmailInOtherCase_ShouldUpdateSameUser()
    {
        var (service, connectionString) = await CreateServiceAsync();
        var first = await service.CreateOrUpdateFromOAuthAsync(new OAuthUserInfo
        {
            Provider = "Google",
            ExternalId = "google-2",
            Name = "Grace Hopper",
            Email = "grace@example.com"
        });

        var second = await service.CreateOrUpdateFromOAuthAsync(new OAuthUserInfo
        {
            Provider = "Google",
            ExternalId = "google-2",
            Name = "Grace B. Hopper",
            Email = "GRACE@Example.com"
        });

        second.Id.Should().Be(first.Id);
        second.Name.Should().Be("Grace B. Hopper");
        await using var context = PostgresFixture.CreateContext(connectionString);
        (await context.Set<Users>().CountAsync()).Should().Be(1);
    }

    [PostgresFact]
    public async Task MigratedUsersTable_ShouldNotHaveLinkedInIdColumn()
    {
        var (_, connectionString) = await CreateServiceAsync();

        var columns = await GetUsersColumnsAsync(connectionString);

        columns.Should().NotContain("LinkedInId");
        columns.Should().Contain("GoogleId");
    }

    [PostgresFact]
    public async Task RemoveLinkedInIdMigration_DownReaddsNullableColumn_AndUpDropsItAgain()
    {
        var (_, connectionString) = await CreateServiceAsync();
        await using var context = PostgresFixture.CreateContext(connectionString);
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync("20261118214026_InitialPostgreSql");
        (await GetUsersColumnsAsync(connectionString)).Should().Contain("LinkedInId");

        await migrator.MigrateAsync();
        (await GetUsersColumnsAsync(connectionString)).Should().NotContain("LinkedInId");
    }

    private static async Task<List<string>> GetUsersColumnsAsync(string connectionString)
    {
        var columns = new List<string>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT column_name FROM information_schema.columns WHERE table_name = 'Users'", connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    [PostgresFact]
    public async Task GetByEmailAsync_WithEmailInDifferentCase_ShouldReturnSameUser()
    {
        var (service, _) = await CreateServiceAsync();
        var created = await service.CreateOrUpdateFromOAuthAsync(new OAuthUserInfo
        {
            Provider = "Google",
            ExternalId = "google-linus-1",
            Name = "Linus Torvalds",
            Email = "Linus@Example.com"
        });

        var found = await service.GetByEmailAsync("LINUS@EXAMPLE.COM");

        found.Should().NotBeNull();
        found!.Id.Should().Be(created.Id);
    }

    [PostgresFact]
    public async Task Users_WithSameEmailInDifferentCase_ShouldRejectSecondRowAsDuplicate()
    {
        var (service, connectionString) = await CreateServiceAsync();
        await service.CreateOrUpdateFromGoogleAsync(new GoogleUserInfo
        {
            GoogleId = "google-3",
            Name = "First",
            Email = "dup@example.com"
        });

        var act = async () => await service.CreateOrUpdateFromGoogleAsync(new GoogleUserInfo
        {
            GoogleId = "google-4",
            Name = "Second",
            Email = "DUP@example.com"
        });

        await act.Should().ThrowAsync<DbUpdateException>();
        await using var context = PostgresFixture.CreateContext(connectionString);
        (await context.Set<Users>().CountAsync()).Should().Be(1);
    }
}
