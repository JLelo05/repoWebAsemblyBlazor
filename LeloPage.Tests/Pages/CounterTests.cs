using LeloPage.Pages;

namespace LeloPage.Tests.Pages;

public class CounterTests : IDisposable
{
    private readonly TestContext _ctx;

    public CounterTests()
    {
        _ctx = new TestContext();

        // Allow any JS call without explicit setup (localStorage, location.reload, etc.)
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _ctx.Services.AddSingleton(new CultureService(_ctx.JSInterop.JSRuntime));
        _ctx.Services.AddSingleton<IStringLocalizer<SharedResources>>(
            new TestStringLocalizer<SharedResources>());
    }

    public void Dispose() => _ctx.Dispose();

    // ── Rendering ──────────────────────────────────────────────

    [Fact]
    public void Counter_InitialRender_ShowsZeroCount()
    {
        var cut = _ctx.RenderComponent<Counter>();

        var status = cut.Find("[role='status']");
        Assert.Contains("0", status.TextContent);
    }

    [Fact]
    public void Counter_InitialRender_HasIncrementButton()
    {
        var cut = _ctx.RenderComponent<Counter>();

        Assert.NotNull(cut.Find("button"));
    }

    // ── Interactions ───────────────────────────────────────────

    [Fact]
    public void Counter_ButtonClick_IncrementsToOne()
    {
        var cut = _ctx.RenderComponent<Counter>();

        cut.Find("button").Click();

        Assert.Contains("1", cut.Find("[role='status']").TextContent);
    }

    [Fact]
    public void Counter_ThreeClicks_ShowsThree()
    {
        var cut = _ctx.RenderComponent<Counter>();
        var button = cut.Find("button");

        button.Click();
        button.Click();
        button.Click();

        Assert.Contains("3", cut.Find("[role='status']").TextContent);
    }

    [Fact]
    public void Counter_TenClicks_ShowsTen()
    {
        var cut = _ctx.RenderComponent<Counter>();
        var button = cut.Find("button");

        for (int i = 0; i < 10; i++) button.Click();

        Assert.Contains("10", cut.Find("[role='status']").TextContent);
    }
}
