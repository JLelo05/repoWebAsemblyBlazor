using Microsoft.Extensions.Localization;

namespace LeloPage.Tests.Helpers;

/// <summary>
/// Returns the resource key as its own value — good enough for testing component structure.
/// </summary>
public class TestStringLocalizer<T> : IStringLocalizer<T>
{
    public LocalizedString this[string name] => new(name, name);

    public LocalizedString this[string name, params object[] arguments]
        => new(name, string.Format(name, arguments));

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => Enumerable.Empty<LocalizedString>();
}
