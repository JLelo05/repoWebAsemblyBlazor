using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace LeloPage.Services;

public class ReservationService(HttpClient http, IConfiguration config)
{
    private readonly string _apiUrl = (config["ApiUrl"] ?? "https://lelopage.eu/api") + "/users.php";

    public async Task<List<UserDto>> GetReservationsForDateAsync(DateTime date)
    {
        try
        {
            var url = $"{_apiUrl}?date={date:yyyy-MM-dd}";
            return await http.GetFromJsonAsync<List<UserDto>>(url) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<UserDto?> PostReservationAsync(string name, DateTime term, int lessons, string phone)
    {
        var response = await http.PostAsJsonAsync(_apiUrl, new
        {
            name,
            term    = term.ToString("yyyy-MM-dd HH:mm:ss"),
            lessons,
            phone
        });
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<UserDto>();
    }
}
