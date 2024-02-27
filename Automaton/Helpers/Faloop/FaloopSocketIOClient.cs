using Automaton.Helpers.Faloop.Model;
using ECommons.DalamudServices;
using SocketIOClient;
using SocketIOClient.Transport;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Automaton.Helpers.Faloop;

#nullable enable
public class FaloopSocketIOClient : IDisposable
{
    private readonly SocketIO client = new("https://faloop.app",
        new SocketIOOptions
        {
            EIO = EngineIO.V4,
            Transport = TransportProtocol.Polling,
            Path = "/comms/socket.io",
            ExtraHeaders = new Dictionary<string, string>
            {
                {
                    "Accept",
                    "*/*"
                },
                {
                    "Referer",
                    "https://faloop.app/"
                },
                {
                    "Origin",
                    "https://faloop.app"
                },
                {
                    "User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36 Edg/122.0.0.0"
                },
            },
        });

    public delegate void ConnectedHandler();
    public event ConnectedHandler? OnConnected;

    public delegate void DisconnectedHandler(string cause);
    public event DisconnectedHandler? OnDisconnected;

    public delegate void ErrorHandler(string error);
    public event ErrorHandler? OnError;

    public delegate void MobReportHandler(MobReportData data);
    public event MobReportHandler? OnMobReport;

    public delegate void MessageHandler(SocketIOResponse response);
    public event MessageHandler? OnMessage;

    public delegate void AnyHandler(string name, SocketIOResponse response);
    public event AnyHandler? OnAny;

    public delegate void ReconnectedHandler(int count);
    public event ReconnectedHandler? OnReconnected;

    public delegate void ReconnectErrorHandler(Exception exception);
    public event ReconnectErrorHandler? OnReconnectError;

    public delegate void ReconnectAttemptHandler(int count);
    public event ReconnectAttemptHandler? OnReconnectAttempt;

    public delegate void ReconnectFailedHandler();
    public event ReconnectFailedHandler? OnReconnectFailed;

    public delegate void PingHandler();
    public event PingHandler? OnPing;

    public delegate void PongHandler(TimeSpan span);
    public event PongHandler? OnPong;

    public FaloopSocketIOClient()
    {
        client.OnConnected += HandleOnConnected;
        client.OnDisconnected += HandleOnDisconnected;
        client.OnError += HandleOnError;
        client.On("message", HandleOnMessage);
        client.OnAny(HandleOnAny);
        client.OnReconnected += HandleReconnected;
        client.OnReconnectError += HandleOnReconnectError;
        client.OnReconnectAttempt += HandleOnReconnectAttempt;
        client.OnReconnectFailed += HandleOnReconnectFailed;
        client.OnPing += HandleOnPing;
        client.OnPong += HandleOnPong;
    }

    public async Task Connect(FaloopSession session)
    {
        if (!session.IsLoggedIn)
        {
            throw new ApplicationException("session is not authenticated.");
        }

        if (client.Connected)
        {
            await client.DisconnectAsync();
        }

        client.Options.Auth = new Dictionary<string, string?>
        {
            {"sessionid", session.SessionId},
        };

        await client.ConnectAsync();
    }

    private void HandleOnConnected(object? _, EventArgs __)
    {
        try
        {
            OnConnected?.Invoke();
        }
        catch (Exception exception)
        {
            Svc.Log.Error(exception, nameof(HandleOnConnected));
        }
        finally
        {
            client.EmitAsync("ack");
        }
    }

    private void HandleOnDisconnected(object? _, string cause)
    {
        try
        {
            OnDisconnected?.Invoke(cause);
        }
        catch (Exception exception)
        {
            Svc.Log.Error(exception, nameof(HandleOnDisconnected));
        }
    }

    private void HandleOnError(object? _, string error)
    {
        try
        {
            OnError?.Invoke(error);
        }
        catch (Exception exception)
        {
            Svc.Log.Error(exception, nameof(HandleOnError));
        }
    }

    private void HandleOnMessage(SocketIOResponse response)
    {
        for (var index = 0; index < response.Count; index++)
        {
            var payload = response.GetValue(index).Deserialize<FaloopEventPayload>();
            if (payload is not { Type: "mob", SubType: "report" }) continue;

            var data = payload.Data.Deserialize<MobReportData>();
            if (data == default) continue;

            try
            {
                OnMobReport?.Invoke(data);
            }
            catch (Exception exception)
            {
                Svc.Log.Error(exception, nameof(HandleOnMessage));
            }
        }

        try
        {
            OnMessage?.Invoke(response);
        }
        catch (Exception exception)
        {
            Svc.Log.Error(exception, nameof(HandleOnMessage));
        }
    }

    private void HandleOnAny(string name, SocketIOResponse response)
    {
        try
        {
            OnAny?.Invoke(name, response);
        }
        catch (Exception exception)
        {
            Svc.Log.Error(exception, nameof(HandleOnAny));
        }
    }

    private void HandleReconnected(object? _, int count)
    {
        try
        {
            OnReconnected?.Invoke(count);
        }
        catch (Exception exception)
        {
            Svc.Log.Error(exception, nameof(HandleReconnected));
        }
    }

    private void HandleOnReconnectError(object? _, Exception exception)
    {
        try
        {
            OnReconnectError?.Invoke(exception);
        }
        catch (Exception e)
        {
            Svc.Log.Error(e, nameof(HandleOnReconnectError));
        }
    }

    private void HandleOnReconnectAttempt(object? _, int count)
    {
        try
        {
            OnReconnectAttempt?.Invoke(count);
        }
        catch (Exception exception)
        {
            Svc.Log.Error(exception, nameof(HandleOnReconnectAttempt));
        }
    }

    private void HandleOnReconnectFailed(object? _, EventArgs __)
    {
        try
        {
            OnReconnectFailed?.Invoke();
        }
        catch (Exception exception)
        {
            Svc.Log.Error(exception, nameof(HandleOnReconnectFailed));
        }
    }

    private void HandleOnPing(object? _, EventArgs __)
    {
        try
        {
            OnPing?.Invoke();
        }
        catch (Exception exception)
        {
            Svc.Log.Error(exception, nameof(HandleOnPing));
        }
    }

    private void HandleOnPong(object? _, TimeSpan span)
    {
        try
        {
            OnPong?.Invoke(span);
        }
        catch (Exception exception)
        {
            Svc.Log.Error(exception, nameof(HandleOnPong));
        }
    }

    public void Dispose()
    {
        client.DisconnectAsync();
        client.Dispose();
    }
}
