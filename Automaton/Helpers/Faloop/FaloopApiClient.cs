using Automaton.Helpers.Faloop.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
            RequestUri = new Uri("https://faloop.app/api-v2/auth/user/refresh"),
            Content = JsonContent.Create(new Dictionary<string, string?>
                {
                    {"sessionId", null},
                },
                MediaTypeHeaderValue.Parse("application/json")),
            Headers =
            {
                {"Accept", "application/json"},
                {"Origin", "https://faloop.app"},
                {"Referer", "https://faloop.app/"},
                {"Sec-Ch-Ua", "\"Chromium\";v=\"122\", \"Not(A:Brand\";v=\"24\", \"Microsoft Edge\";v=\"122\""},
                {"Sec-Ch-Ua-Mobile", "?0"},
                {"Sec-Ch-Ua-Platform", "\"Windows\""},
                {"Sec-Fetch-Dest", "empty"},
                {"Sec-Fetch-Mode", "cors"},
                {"Sec-Fetch-Site", "same-origin"},
                {
                    "User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36 Edg/122.0.0.0"
                },
            },
        };

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserRefreshResponse>();
    }

    public async Task<UserLoginResponse?> LoginAsync(string username, string password, string sessionId, string token)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://faloop.app/api-v2/auth/user/login"),
            Content = JsonContent.Create(new Dictionary<string, object>
                {
                    {"username", username},
                    {"password", password},
                    {"rememberMe", true},
                    {"sessionId", sessionId},
                },
                MediaTypeHeaderValue.Parse("application/json")),
            Headers =
            {
                {"Accept", "application/json"},
                {"Authorization", token}, // JWT eyJ...
                {"Origin", "https://faloop.app"},
                {"Referer", "https://faloop.app/login"},
                {"Sec-Ch-Ua", "\"Chromium\";v=\"122\", \"Not(A:Brand\";v=\"24\", \"Microsoft Edge\";v=\"122\""},
                {"Sec-Ch-Ua-Mobile", "?0"},
                {"Sec-Ch-Ua-Platform", "\"Windows\""},
                {"Sec-Fetch-Dest", "empty"},
                {"Sec-Fetch-Mode", "cors"},
                {"Sec-Fetch-Site", "same-origin"},
                {
                    "User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36 Edg/122.0.0.0"
                },
            },
        };

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserLoginResponse>();
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
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36 Edg/122.0.0.0"
                },
            },
        };

        using var response = await client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    public void Dispose() => client.Dispose();
}
