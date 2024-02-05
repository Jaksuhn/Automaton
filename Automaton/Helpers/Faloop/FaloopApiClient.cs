using Automaton.Helpers.Faloop.Model;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Automaton.Helpers.Faloop;

public class FaloopApiClient : IDisposable
{
    private readonly HttpClient client = new();

    public async Task<UserRefreshResponse?> RefreshAsync()
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://faloop.app/api/auth/user/refresh"),
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                sessionId = null as string,
            }), Encoding.UTF8, "application/json"),
            Headers =
            {
                {"Origin", "https://faloop.app"},
                {"Referer", "https://faloop.app/"},
                {
                    "User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36"
                },
            },
        };

        using var response = await client.SendAsync(request);
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<UserRefreshResponse>(stream, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
    }

    public async Task<UserLoginResponse?> LoginAsync(string username, string password, string sessionId, string token)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://faloop.app/api/auth/user/login"),
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                username,
                password,
                rememberMe = false,
                sessionId,
            }), Encoding.UTF8, "application/json"),
            Headers =
            {
                {"Authorization", token},
                {"Origin", "https://faloop.app"},
                {"Referer", "https://faloop.app/login/"},
                {
                    "User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36"
                },
            },
        };

        using var response = await client.SendAsync(request);
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<UserLoginResponse>(stream, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
    }

    public async Task<string> DownloadText(Uri uri)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = uri,
            Headers =
            {
                {"Origin", "https://faloop.app"},
                {"Referer", "https://faloop.app/"},
                {
                    "User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36"
                },
            },
        };

        using var response = await client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    public void Dispose() => client.Dispose();
}
