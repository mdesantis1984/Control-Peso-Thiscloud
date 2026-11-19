using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ControlPeso.Infrastructure;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c> to create migrations and SQL scripts.
/// It never connects to a database: the connection string only selects the Npgsql provider.
/// Override it with the CONTROLPESO_DESIGN_CONNECTION environment variable if needed.
/// </summary>
public sealed class ControlPesoDbContextFactory : IDesignTimeDbContextFactory<ControlPesoDbContext>
{
    private const string DefaultDesignConnection = "Host=localhost;Database=controlpeso;Username=postgres";

    public ControlPesoDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CONTROLPESO_DESIGN_CONNECTION")
            ?? DefaultDesignConnection;

        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ControlPesoDbContext(options);
    }
}
