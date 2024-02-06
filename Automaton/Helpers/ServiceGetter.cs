using Dalamud.Plugin;
using ECommons.DalamudServices;
using System.Reflection;

namespace Automaton.Helpers;
public static class ServiceGetter
{
    public static T GetService<T>()
    {
        Svc.Log.Info($"Requesting {typeof(T)}");
        var service = typeof(IDalamudPlugin).Assembly.GetType("Dalamud.Service`1")!.MakeGenericType(typeof(T));
        var get = service.GetMethod("Get", BindingFlags.Public | BindingFlags.Static)!;
        return (T)get.Invoke(null, null)!;
    }
}
