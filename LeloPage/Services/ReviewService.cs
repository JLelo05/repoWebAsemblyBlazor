using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace LeloPage.Services;

public class ReviewService(HttpClient http, IConfiguration config)
{
    private readonly string _apiUrl = (config["ApiUrl"] ?? "https://lelopage.eu/api") + "/reviews.php";

    public async Task<List<ReviewDto>> GetReviewsAsync()
    {
        try
        {
            return await http.GetFromJsonAsync<List<ReviewDto>>(_apiUrl) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<ReviewDto?> PostReviewAsync(string name, string text)
    {
        var response = await http.PostAsJsonAsync(_apiUrl, new { name, text });
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ReviewDto>();
    }
}
