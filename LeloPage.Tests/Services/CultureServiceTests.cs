namespace LeloPage.Tests.Services;

public class CultureServiceTests : IDisposable
{
    private readonly TestContext _ctx;

    public CultureServiceTests()
    {
        _ctx = new TestContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _ctx.Dispose();

    // ── Helpers ────────────────────────────────────────────────

    private void SetupStoredCulture(string? value)
        => _ctx.JSInterop.Setup<string?>("localStorage.getItem", _ => true).SetResult(value);

    private void SetupBrowserLanguage(string? value)
        => _ctx.JSInterop.Setup<string?>("eval", _ => true).SetResult(value);

    private CultureService CreateService() => new(_ctx.JSInterop.JSRuntime);

    // ── InitializeAsync ────────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_WithStoredSupportedCulture_SetsThatCulture()
    {
        SetupStoredCulture("sk");
        var svc = CreateService();

        await svc.InitializeAsync();

        Assert.Equal("sk", svc.CurrentCulture.Name);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("sk")]
    [InlineData("cs")]
    public async Task InitializeAsync_EachSupportedCulture_SetsCorrectly(string code)
    {
        SetupStoredCulture(code);
        var svc = CreateService();

        await svc.InitializeAsync();

        Assert.Equal(code, svc.CurrentCulture.Name);
    }

    [Fact]
    public async Task InitializeAsync_WithUnsupportedStoredCulture_FallsBackToBrowserLanguage()
    {
        SetupStoredCulture("fr"); // not supported
        SetupBrowserLanguage("sk-SK");
        var svc = CreateService();

        await svc.InitializeAsync();

        Assert.Equal("sk", svc.CurrentCulture.Name);
    }

    [Fact]
    public async Task InitializeAsync_WithNullStoredCulture_UsesBrowserLanguage()
    {
        SetupStoredCulture(null);
        SetupBrowserLanguage("cs-CZ");
        var svc = CreateService();

        await svc.InitializeAsync();

        Assert.Equal("cs", svc.CurrentCulture.Name);
    }

    [Fact]
    public async Task InitializeAsync_WithUnsupportedBrowserCulture_DefaultsToEnglish()
    {
        SetupStoredCulture(null);
        SetupBrowserLanguage("fr-FR"); // not supported
        var svc = CreateService();

        await svc.InitializeAsync();

        Assert.Equal("en", svc.CurrentCulture.Name);
    }

    [Fact]
    public async Task InitializeAsync_WithNoBrowserCulture_DefaultsToEnglish()
    {
        SetupStoredCulture(null);
        SetupBrowserLanguage(null);
        var svc = CreateService();

        await svc.InitializeAsync();

        Assert.Equal("en", svc.CurrentCulture.Name);
    }

    [Fact]
    public async Task InitializeAsync_WhenJSThrows_DefaultsToEnglish()
    {
        // Default JSRuntimeMode.Strict throws on any unconfigured call.
        // CultureService.InitializeAsync() catches Exception and defaults to English.
        using var strictCtx = new TestContext(); // Strict mode by default
        var svc = new CultureService(strictCtx.JSInterop.JSRuntime);

        await svc.InitializeAsync();

        Assert.Equal("en", svc.CurrentCulture.Name);
    }

    [Fact]
    public async Task InitializeAsync_WithStoredCulture_FiresOnCultureChangedEvent()
    {
        SetupStoredCulture("sk");
        var svc = CreateService();
        int callCount = 0;
        svc.OnCultureChanged += () => callCount++;

        await svc.InitializeAsync();

        Assert.True(callCount > 0, "OnCultureChanged should fire at least once");
    }

    // ── SetCultureAsync ────────────────────────────────────────

    [Fact]
    public async Task SetCultureAsync_WithSupportedCulture_SavestoLocalStorage()
    {
        var svc = CreateService();

        await svc.SetCultureAsync("sk");

        Assert.Contains(
            _ctx.JSInterop.Invocations,
            i => i.Identifier == "localStorage.setItem"
                 && i.Arguments.OfType<string>().Contains("sk"));
    }

    [Fact]
    public async Task SetCultureAsync_WithSupportedCulture_TriggersPageReload()
    {
        var svc = CreateService();

        await svc.SetCultureAsync("en");

        Assert.Contains(
            _ctx.JSInterop.Invocations,
            i => i.Identifier == "location.reload");
    }

    [Fact]
    public async Task SetCultureAsync_WithUnsupportedCulture_DoesNotCallJS()
    {
        var svc = CreateService();

        await svc.SetCultureAsync("fr"); // not supported

        Assert.Empty(_ctx.JSInterop.Invocations);
    }

    // ── GetSupportedCultures ───────────────────────────────────

    [Fact]
    public void GetSupportedCultures_ReturnsExactlyThreeCultures()
    {
        var svc = CreateService();

        var cultures = svc.GetSupportedCultures();

        Assert.Equal(3, cultures.Count);
    }

    [Fact]
    public void GetSupportedCultures_ContainsEnSkCs()
    {
        var svc = CreateService();

        var codes = svc.GetSupportedCultures().Select(c => c.Code).ToList();

        Assert.Contains("en", codes);
        Assert.Contains("sk", codes);
        Assert.Contains("cs", codes);
    }
}
