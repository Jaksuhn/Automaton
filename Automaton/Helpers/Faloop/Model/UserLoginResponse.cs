using System.Text.Json.Serialization;

namespace Automaton.Helpers.Faloop.Model;

public record UserLoginResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("sessionId")] string SessionId,
    [property: JsonPropertyName("token")] string Token);
