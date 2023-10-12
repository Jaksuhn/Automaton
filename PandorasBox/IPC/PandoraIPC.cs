using ECommons.DalamudServices;
using ECommons.Reflection;
using PandorasBox.Features;
using PandorasBox.Helpers;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandorasBox.IPC
{
    internal static class PandoraIPC
    {
        internal static void SetConfigEnabled(string featureName, string configPropName, bool state) => Svc.PluginInterface.GetIpcSubscriber<bool>("PandorasBox.SetConfigEnabled").InvokeFunc();
        internal static bool GetConfigEnabled(string featureName, string configPropName) => Svc.PluginInterface.GetIpcSubscriber<bool>("PandorasBox.GetConfigEnabled").InvokeFunc();
        internal static void SetFeatureEnabled(string featureName, bool state) => Svc.PluginInterface.GetIpcSubscriber<bool>("PandorasBox.SetFeatureEnabled").InvokeFunc();
        internal static bool GetFeatureEnabled(string featureName) => Svc.PluginInterface.GetIpcSubscriber<bool>("PandorasBox.GetFeatureEnabled").InvokeFunc();
    }
}
