using ControlPeso.Web.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ControlPeso.Web.Tests.Extensions;

/// <summary>S2: keys are persisted to DataProtection:Path when configured; nothing changes when it is unset.</summary>
public sealed class DataProtectionExtensionsTests : IDisposable
{
    private readonly string _scratch = Directory.CreateTempSubdirectory("controlpeso-dp-").FullName;

    public void Dispose() => Directory.Delete(_scratch, recursive: true);

    private static IConfiguration Config(string? path) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(path is null
                ? []
                : new Dictionary<string, string?> { ["DataProtection:Path"] = path })
            .Build();

    [Fact]
    public void WithPath_PersistsKeysToThatDirectory()
    {
        var keys = Path.Combine(_scratch, "keys");
        var services = new ServiceCollection().AddLogging();

        services.AddConfiguredDataProtection(Config(keys));
        using var provider = services.BuildServiceProvider();
        var protectedText = provider.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("test").Protect("secret");

        Directory.GetFiles(keys, "key-*.xml").Should().NotBeEmpty();
        protectedText.Should().NotBe("secret");
    }

    [Fact]
    public void WithPath_PayloadsSurviveARestart()
    {
        var keys = Path.Combine(_scratch, "keys");
        string protectedText;
        using (var first = new ServiceCollection().AddLogging().AddConfiguredDataProtection(Config(keys)).BuildServiceProvider())
        {
            protectedText = first.GetRequiredService<IDataProtectionProvider>().CreateProtector("test").Protect("secret");
        }

        using var second = new ServiceCollection().AddLogging().AddConfiguredDataProtection(Config(keys)).BuildServiceProvider();

        second.GetRequiredService<IDataProtectionProvider>().CreateProtector("test").Unprotect(protectedText)
            .Should().Be("secret");
    }

    [Fact]
    public void WithoutPath_LeavesKeyStorageAtTheFrameworkDefault()
    {
        var services = new ServiceCollection().AddLogging();

        services.AddConfiguredDataProtection(Config(null));
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository.Should().BeNull();
        services.Should().NotContain(d => d.ServiceType == typeof(IDataProtectionBuilder));
    }

    [Fact]
    public void WithPath_KeepsTheDefaultApplicationDiscriminator()
    {
        // Setting an application name would change the purpose chain and invalidate cookies already issued.
        var services = new ServiceCollection().AddLogging();

        services.AddConfiguredDataProtection(Config(Path.Combine(_scratch, "keys")));
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<DataProtectionOptions>>().Value.ApplicationDiscriminator.Should().BeNull();
    }
}
