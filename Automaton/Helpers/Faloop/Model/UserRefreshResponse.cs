namespace Automaton.Helpers.Faloop.Model;

public record UserRefreshResponse(bool Success, string SessionId, string Token);
