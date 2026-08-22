using Bunit;
using LeloPage.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace LeloPage.Tests.Pages;

public class AutomatPageTests : IDisposable
{
    private readonly TestContext _ctx;

    public AutomatPageTests()
    {
        _ctx = new TestContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _ctx.Services.AddSingleton(new CultureService(_ctx.JSInterop.JSRuntime));
        _ctx.Services.AddSingleton<IStringLocalizer<SharedResources>>(
            new TestStringLocalizer<SharedResources>());
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void AutomatPage_InitialRender_ShowsYahooConverterActionBar()
    {
        var cut = _ctx.RenderComponent<AutomatPage>();

        Assert.Contains("YahooConverter", cut.Markup);
        Assert.Contains("Source (select .xlsx file)", cut.Markup);
    }
}
