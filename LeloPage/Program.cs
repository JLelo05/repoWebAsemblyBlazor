using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LeloPage;
using LeloPage.Services;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add localization services
builder.Services.AddLocalization();

// Add culture service
builder.Services.AddScoped<CultureService>();

var host = builder.Build();

// Initialize culture
var cultureService = host.Services.GetRequiredService<CultureService>();
await cultureService.InitializeAsync();

Console.WriteLine($"Current Culture: {CultureInfo.CurrentCulture.Name}");
Console.WriteLine($"Current UI Culture: {CultureInfo.CurrentUICulture.Name}");

await host.RunAsync();

