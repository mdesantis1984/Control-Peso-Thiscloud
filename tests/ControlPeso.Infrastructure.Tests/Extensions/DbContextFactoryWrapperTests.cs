using ControlPeso.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Reflection;

namespace ControlPeso.Infrastructure.Tests.Extensions;

/// <summary>
/// Tests para DbContextFactoryWrapper.
/// Valida que el wrapper convierte correctamente de IDbContextFactory&lt;ControlPesoDbContext&gt;
/// a IDbContextFactory&lt;DbContext&gt; (genérico).
/// </summary>
public sealed class DbContextFactoryWrapperTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IDbContextFactory<DbContext> _factoryWrapper;

    public DbContextFactoryWrapperTests()
    {
        // Setup DI container con servicios de Infrastructure
        var services = new ServiceCollection();

        // Configuration fake con connection string SQLite en memoria
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:"
            })
            .Build();

        // Mock IHostEnvironment como Development
        var environment = new Mock<IHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns("Development");

        // Registrar servicios de Infrastructure
        services.AddInfrastructureServices(configuration, environment.Object);

        _serviceProvider = services.BuildServiceProvider();

        // Obtener factory wrapper desde DI
        _factoryWrapper = _serviceProvider.GetRequiredService<IDbContextFactory<DbContext>>();
    }

    [Fact]
    public void Constructor_WhenInnerFactoryIsNull_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<TargetInvocationException>(() =>
        {
            // Acceder al constructor interno via reflection (porque es internal)
            var wrapperType = typeof(ServiceCollectionExtensions).Assembly
                .GetTypes()
                .First(t => t.Name == "DbContextFactoryWrapper");

            Activator.CreateInstance(wrapperType, new object?[] { null });
        });

        // Verificar que la inner exception es ArgumentNullException
        Assert.IsType<ArgumentNullException>(ex.InnerException);
    }

    [Fact]
    public void CreateDbContext_WhenCalled_ReturnsValidDbContext()
    {
        // Act
        using var context = _factoryWrapper.CreateDbContext();

        // Assert
        Assert.NotNull(context);
        Assert.IsAssignableFrom<DbContext>(context);
        Assert.IsType<ControlPesoDbContext>(context);
    }

    [Fact]
    public async Task CreateDbContextAsync_WhenCalled_ReturnsValidDbContext()
    {
        // Act
        await using var context = await _factoryWrapper.CreateDbContextAsync();

        // Assert
        Assert.NotNull(context);
        Assert.IsAssignableFrom<DbContext>(context);
        Assert.IsType<ControlPesoDbContext>(context);
    }

    [Fact]
    public async Task CreateDbContextAsync_WithCancellationToken_RespectsToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        // Act
        await using var context = await _factoryWrapper.CreateDbContextAsync(cts.Token);

        // Assert
        Assert.NotNull(context);
        Assert.False(cts.Token.IsCancellationRequested);
    }

    [Fact]
    public void CreateDbContext_CalledMultipleTimes_ReturnsDistinctInstances()
    {
        // Act
        using var context1 = _factoryWrapper.CreateDbContext();
        using var context2 = _factoryWrapper.CreateDbContext();

        // Assert
        Assert.NotNull(context1);
        Assert.NotNull(context2);
        Assert.NotSame(context1, context2); // Instancias diferentes
    }

    [Fact]
    public async Task CreateDbContextAsync_CalledMultipleTimes_ReturnsDistinctInstances()
    {
        // Act
        await using var context1 = await _factoryWrapper.CreateDbContextAsync();
        await using var context2 = await _factoryWrapper.CreateDbContextAsync();

        // Assert
        Assert.NotNull(context1);
        Assert.NotNull(context2);
        Assert.NotSame(context1, context2); // Instancias diferentes
    }

    [Fact]
    public void CreateDbContext_WhenDisposed_CanBeCalledAgain()
    {
        // Arrange
        using (var context1 = _factoryWrapper.CreateDbContext())
        {
            Assert.NotNull(context1);
        }

        // Act - crear otro contexto después de dispose del primero
        using var context2 = _factoryWrapper.CreateDbContext();

        // Assert
        Assert.NotNull(context2);
    }

    [Fact]
    public async Task CreateDbContextAsync_ConcurrentCalls_HandlesMultipleThreads()
    {
        // Arrange
        var tasks = new List<Task<DbContext>>();
        var contextCount = 10;

        // Act - crear múltiples contextos concurrentemente
        for (int i = 0; i < contextCount; i++)
        {
            tasks.Add(_factoryWrapper.CreateDbContextAsync());
        }

        var contexts = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(contextCount, contexts.Length);
        Assert.All(contexts, ctx => Assert.NotNull(ctx));

        // Verificar que todas son instancias distintas
        var distinctContexts = contexts.Distinct().Count();
        Assert.Equal(contextCount, distinctContexts);

        // Cleanup
        foreach (var ctx in contexts)
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public void CreateDbContext_ReturnsContextWithCorrectDatabase()
    {
        // Act
        using var context = _factoryWrapper.CreateDbContext();

        // Assert
        Assert.NotNull(context.Database);
        Assert.True(context.Database.IsSqlite() || context.Database.IsSqlServer());
    }

    [Fact]
    public async Task CreateDbContextAsync_ReturnsContextWithWorkingConnection()
    {
        // Act
        await using var context = await _factoryWrapper.CreateDbContextAsync();

        // Assert
        Assert.NotNull(context.Database);
        Assert.True(await context.Database.CanConnectAsync());
    }

    [Fact]
    public void ServiceCollectionExtensions_RegistersFactoryWrapperCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:"
            })
            .Build();

        // Act
        services.AddInfrastructureServices(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert - verificar que se puede resolver IDbContextFactory<DbContext>
        var factory = provider.GetService<IDbContextFactory<DbContext>>();
        Assert.NotNull(factory);

        // Verificar que también existe IDbContextFactory<ControlPesoDbContext>
        var concreteFactory = provider.GetService<IDbContextFactory<ControlPesoDbContext>>();
        Assert.NotNull(concreteFactory);
    }

    [Fact]
    public void ServiceCollectionExtensions_BackwardCompatibility_RegistersDbContextScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:"
            })
            .Build();

        // Act
        services.AddInfrastructureServices(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert - verificar backward compatibility con DbContext scoped
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<DbContext>();
        Assert.NotNull(dbContext);

        var controlPesoDbContext = scope.ServiceProvider.GetService<ControlPesoDbContext>();
        Assert.NotNull(controlPesoDbContext);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
