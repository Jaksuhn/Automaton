using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace Automaton.IPC
{
    internal static class PandorasBoxIPC
    {
        internal static string Name = "PandorasBox";
        internal static ICallGateSubscriber<string, bool?> GetFeatureEnabled;
        internal static ICallGateSubscriber<string, bool, object> SetFeatureEnabled;
        internal static ICallGateSubscriber<string, string, bool?> GetConfigEnabled;
        internal static ICallGateSubscriber<string, string, bool, object> SetConfigEnabled;

        internal static void Init()
        {
            GetFeatureEnabled = Svc.PluginInterface.GetIpcSubscriber<string, bool?>($"PandorasBox.GetFeatureEnabled");
            SetFeatureEnabled = Svc.PluginInterface.GetIpcSubscriber<string, bool, object>($"PandorasBox.SetFeatureEnabled");
            GetConfigEnabled = Svc.PluginInterface.GetIpcSubscriber<string, string, bool?>($"PandorasBox.GetConfigEnabled");
            SetConfigEnabled = Svc.PluginInterface.GetIpcSubscriber<string, string, bool, object>($"PandorasBox.SetConfigEnabled");
        }

        internal static void Dispose()
        {
            GetFeatureEnabled = null;
            GetConfigEnabled = null;
            SetFeatureEnabled = null;
            SetConfigEnabled = null;
        }
    }
}
