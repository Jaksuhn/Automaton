using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ECommons.Logging;
using Automaton.Helpers.Faloop.Model;
using SocketIOClient;
using SocketIOClient.Transport;

namespace Automaton.Helpers.Faloop;

#nullable enable
public class FaloopSocketIOClient : IDisposable
{
    private readonly SocketIO client;

    public event Action? OnConnected;
    public event Action<string>? OnDisconnected;
    public event Action<string>? OnError;
    public event Action<MobReportData>? OnMobReport;
    public event Action<SocketIOResponse>? OnMessage;
    public event Action<string, SocketIOResponse>? OnAny;
    public event Action<int>? OnReconnected;
    public event Action<Exception>? OnReconnectError;
    public event Action<int>? OnReconnectAttempt;
    public event Action? OnReconnectFailed;
    public event Action? OnPing;
    public event Action<TimeSpan>? OnPong;

    public FaloopSocketIOClient()
    {
        client = new SocketIO(
            "https://comms.faloop.app/mobStatus",
            new SocketIOOptions
            {
                EIO = EngineIO.V4,
                Transport = TransportProtocol.Polling,
                ExtraHeaders = new Dictionary<string, string>
                {
                    { "Accept", "*/*" },
                    { "Accept-Language", "ja" },
                    { "Referer", "https://faloop.app/" },
                    { "Origin", "https://faloop.app" },
                    { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36" },
                },
            });

        client.OnConnected += (_, __) =>
        {
            OnConnected?.Invoke();
            client.EmitAsync("ack");
        };

        client.OnDisconnected += (_, cause) => OnDisconnected?.Invoke(cause);
        client.OnError += (_, error) => OnError?.Invoke(error);
        client.On("message", HandleOnMessage);
        client.OnAny(HandleOnAny);
        client.OnReconnected += (_, count) => OnReconnected?.Invoke(count);
        client.OnReconnectError += (_, exception) => OnReconnectError?.Invoke(exception);
        client.OnReconnectAttempt += (_, count) => OnReconnectAttempt?.Invoke(count);
        client.OnReconnectFailed += (_, __) => OnReconnectFailed?.Invoke();
        client.OnPing += (_, __) => OnPing?.Invoke();
        client.OnPong += (_, span) => OnPong?.Invoke(span);
    }

    private void HandleOnMessage(SocketIOResponse response)
    {
        for (var index = 0; index < response.Count; index++)
        {
            var payload = response.GetValue(index).Deserialize<FaloopEventPayload>();
            if (payload is { Type: "mob", SubType: "report" })
            {
                var data = payload.Data.Deserialize<MobReportData>();
                if (data != default)
                {
                    OnMobReport?.Invoke(data);
                }
            }
        }

        OnMessage?.Invoke(response);
    }

    private void HandleOnAny(string name, SocketIOResponse response) => OnAny?.Invoke(name, response);

    public async Task Connect(FaloopSession session)
    {
        if (!session.IsLoggedIn)
            throw new ApplicationException("session is not authenticated.");

        if (client.Connected)
            await client.DisconnectAsync();

        client.Options.Auth = new Dictionary<string, string?>
        {
            {"sessionid", session.SessionId},
        };

        await client.ConnectAsync();
    }

    public void Dispose()
    {
        client.DisconnectAsync();
        client.Dispose();
    }
}
