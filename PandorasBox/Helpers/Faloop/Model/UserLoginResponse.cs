﻿namespace Automaton.Helpers.Faloop.Model;

public record UserLoginResponse(bool Success, string SessionId, string Token);
