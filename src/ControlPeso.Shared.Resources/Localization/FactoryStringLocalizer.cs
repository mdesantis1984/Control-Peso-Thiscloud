using Microsoft.Extensions.Localization;

namespace ControlPeso.Shared.Resources.Localization;

/// <summary>
/// Generic string localizer that delegates to IStringLocalizerFactory.
/// This is the bridge that allows DI to resolve IStringLocalizer&lt;T&gt; by calling factory.Create(typeof(T)).
/// </summary>
/// <remarks>
/// This class is registered as Transient in DI, and it creates a new localizer instance
/// by calling the registered IStringLocalizerFactory.Create(typeof(T)).
/// This is the standard pattern used by Microsoft.Extensions.Localization but renamed to avoid conflict.
/// </remarks>
public sealed class FactoryStringLocalizer<TResourceSource> : IStringLocalizer<TResourceSource>
{
    private readonly IStringLocalizer _localizer;

    public FactoryStringLocalizer(IStringLocalizerFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _localizer = factory.Create(typeof(TResourceSource));
    }

    public LocalizedString this[string name] => _localizer[name];

    public LocalizedString this[string name, params object[] arguments] => _localizer[name, arguments];

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => _localizer.GetAllStrings(includeParentCultures);
}
