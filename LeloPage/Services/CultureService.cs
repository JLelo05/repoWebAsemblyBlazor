using System.Globalization;
using Microsoft.JSInterop;

namespace LeloPage.Services
{
    public class CultureService
    {
        private readonly IJSRuntime _jsRuntime;
        private CultureInfo _currentCulture = new CultureInfo("en");

        public event Action? OnCultureChanged;

        public CultureService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            private set
            {
                if (_currentCulture != value)
                {
                    _currentCulture = value;
                    CultureInfo.DefaultThreadCurrentCulture = value;
                    CultureInfo.DefaultThreadCurrentUICulture = value;
                    CultureInfo.CurrentCulture = value;
                    CultureInfo.CurrentUICulture = value;
                    OnCultureChanged?.Invoke();
                }
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Try to get culture from localStorage
                var storedCulture = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "selectedCulture");
                Console.WriteLine($"[CultureService] Stored culture from localStorage: {storedCulture ?? "null"}");

                if (!string.IsNullOrEmpty(storedCulture) && IsSupportedCulture(storedCulture))
                {
                    Console.WriteLine($"[CultureService] Setting culture to: {storedCulture}");
                    CurrentCulture = new CultureInfo(storedCulture);
                    return;
                }

                // Fallback to browser language
                var browserCulture = await _jsRuntime.InvokeAsync<string?>("eval", "navigator.language || navigator.userLanguage");
                Console.WriteLine($"[CultureService] Browser culture: {browserCulture ?? "null"}");
                if (!string.IsNullOrEmpty(browserCulture))
                {
                    var culturePart = browserCulture.Split('-')[0].ToLower();
                    if (IsSupportedCulture(culturePart))
                    {
                        Console.WriteLine($"[CultureService] Setting culture to browser language: {culturePart}");
                        CurrentCulture = new CultureInfo(culturePart);
                        return;
                    }
                }

                // Default to English
                Console.WriteLine($"[CultureService] Defaulting to English");
                CurrentCulture = new CultureInfo("en");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CultureService] Error: {ex.Message}");
                // If anything fails, default to English
                CurrentCulture = new CultureInfo("en");
            }
        }

        public async Task SetCultureAsync(string culture)
        {
            if (!IsSupportedCulture(culture))
                return;

            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "selectedCulture", culture);

            // Force page reload to apply the new culture
            await _jsRuntime.InvokeVoidAsync("location.reload");
        }

        private bool IsSupportedCulture(string culture)
        {
            var supportedCultures = new[] { "en", "sk", "cs" };
            return supportedCultures.Contains(culture.ToLower());
        }

        public List<(string Code, string DisplayName)> GetSupportedCultures()
        {
            return new List<(string Code, string DisplayName)>
            {
                ("en", "English"),
                ("sk", "Slovenčina"),
                ("cs", "Čeština")
            };
        }
    }
}
