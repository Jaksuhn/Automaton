using ECommons.DalamudServices;
using System;
using System.Threading.Tasks;

namespace Automaton.Helpers.Faloop;

public class FaloopSession : IDisposable
{
    private readonly FaloopApiClient client = new();
    public readonly FaloopEmbedData EmbedData;

    public FaloopSession() => EmbedData = new FaloopEmbedData(client);

    public bool IsLoggedIn { get; private set; }

    public string? SessionId { get; private set; }

    public async Task<bool> LoginAsync(string username, string password)
    {
        Logout();

        var initialSession = await client.RefreshAsync();
        if (initialSession is not { Success: true })
        {
            Svc.Log.Debug("LoginAsync: initialSession is not success");
            return false;
        }

        var login = await client.LoginAsync(username, password, initialSession.SessionId, initialSession.Token);
        if (login is not { Success: true })
        {
            Svc.Log.Debug("LoginAsync: login is not success");
            return false;
        }

        try
        {
            await EmbedData.Initialize();
        }
        catch (Exception exception)
        {
            Svc.Log.Error(exception, "LoginAsync: EmbedData.Initialize failed");
            return false;
        }

        IsLoggedIn = true;
        SessionId = login.SessionId;
        return true;
    }

    private void Logout()
    {
        IsLoggedIn = false;
        SessionId = default;
    }

    public void Dispose() => client.Dispose();
}
