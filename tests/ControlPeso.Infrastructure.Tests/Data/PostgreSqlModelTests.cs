using ControlPeso.Domain.Entities;
using ControlPeso.Infrastructure.Extensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControlPeso.Infrastructure.Tests.Data;

/// <summary>
/// Verifies the EF Core model maps to PostgreSQL (Npgsql) the way production expects.
/// No database connection is opened: only the relational model is inspected.
/// </summary>
public sealed class PostgreSqlModelTests
{
    private const string NpgsqlConnectionString =
        "Host=localhost;Port=5432;Database=controlpeso;Username=controlpeso_app;Password=unused";

    private static ControlPesoDbContext CreateNpgsqlContext() =>
        new(new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseNpgsql(NpgsqlConnectionString)
            .Options);

    private static IRelationalModel RelationalModel(DbContext context) =>
        context.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model.GetRelationalModel();

    [Fact]
    public void AddInfrastructureServices_ShouldUseNpgsql_WhenConnectionStringIsPostgreSql()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = NpgsqlConnectionString
            })
            .Build();
        var services = new ServiceCollection();

        services.AddInfrastructureServices(configuration);
        using var provider = services.BuildServiceProvider();
        using var context = provider.GetRequiredService<IDbContextFactory<ControlPesoDbContext>>().CreateDbContext();

        context.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    [Fact]
    public void Model_ShouldNotUseSqlServerSpecificSql_WhenProviderIsNpgsql()
    {
        using var context = CreateNpgsqlContext();

        var columns = RelationalModel(context).Tables.SelectMany(t => t.Columns).ToList();

        columns.Should().NotContain(c => (c.DefaultValueSql ?? string.Empty).Contains("newid", StringComparison.OrdinalIgnoreCase));
        columns.Should().NotContain(c => (c.DefaultValueSql ?? string.Empty).Contains("getutcdate", StringComparison.OrdinalIgnoreCase));
        RelationalModel(context).Collation.Should().BeNull();
    }

    [Fact]
    public void Model_ShouldGenerateIdsAndTimestampsInPostgreSql_WhenProviderIsNpgsql()
    {
        using var context = CreateNpgsqlContext();
        var users = RelationalModel(context).Tables.Single(t => t.Name == nameof(ControlPesoDbContext.Users));

        users.FindColumn(nameof(Users.Id))!.DefaultValueSql.Should().Be("gen_random_uuid()");
        users.FindColumn(nameof(Users.CreatedAt))!.DefaultValueSql.Should().Be("(now() at time zone 'utc')");
        users.FindColumn(nameof(Users.CreatedAt))!.StoreType.Should().Be("timestamp without time zone");
    }

    [Fact]
    public void Model_ShouldStoreEmailCaseInsensitiveAndUnique_WhenProviderIsNpgsql()
    {
        using var context = CreateNpgsqlContext();
        var users = RelationalModel(context).Tables.Single(t => t.Name == nameof(ControlPesoDbContext.Users));

        users.FindColumn(nameof(Users.Email))!.StoreType.Should().Be("citext");
        users.Indexes.Should().Contain(i => i.IsUnique && i.Columns.Single().Name == nameof(Users.Email));
    }

    [Fact]
    public void Model_ShouldMapAllDateTimesWithoutTimeZone_WhenProviderIsNpgsql()
    {
        using var context = CreateNpgsqlContext();

        var dateTimeColumns = RelationalModel(context).Tables
            .SelectMany(t => t.Columns)
            .Where(c => c.ProviderClrType == typeof(DateTime))
            .ToList();

        dateTimeColumns.Should().NotBeEmpty();
        dateTimeColumns.Should().OnlyContain(c => c.StoreType == "timestamp without time zone");
    }
}
