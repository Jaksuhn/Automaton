using ECommons.DalamudServices;
using ECommons.Logging;
using Automaton.FeaturesSetup;
using System;
using Dalamud.Plugin;
using System.Reflection;
using System.Linq;
using System.Reflection.Emit;

namespace Automaton.Features.Testing
{
    public unsafe class PluginUnlocker : Feature
    {
        public override string Name => "Plugin Unlocker";
        public override string Description => "Can't stand plugins not working in PvP areas despite having nothing to do with PvP? Me too.";

        public override FeatureType FeatureType => FeatureType.Disabled;

        public Configs Config { get; private set; }
        public override bool UseAutoConfig => true;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("SortaKinda", "", HelpText = "Makes it work in PvP")]
            public bool SortaKinda = true;
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            if (Config.SortaKinda)
                SortaKindaUnlockPvP();
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            base.Disable();
        }

        internal static void SortaKindaUnlockPvP()
        {
            try
            {
                var plugin = GetPluginByName("sortakinda");
                var openConfigWindowMethod = plugin.GetType().GetMethod("OpenConfigWindow");

                if (openConfigWindowMethod != null)
                {
                    var newOpenConfigWindowMethod = new DynamicMethod("OpenConfigWindow", openConfigWindowMethod.ReturnType, openConfigWindowMethod.GetParameters().Select(p => p.ParameterType).ToArray(), openConfigWindowMethod.DeclaringType);

                    var ilGenerator = newOpenConfigWindowMethod.GetILGenerator();
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Call, plugin.GetType().GetMethod("Toggle"));
                    ilGenerator.Emit(OpCodes.Ret);

                    var openConfigWindowField = plugin.GetType().GetField("OpenConfigWindow", BindingFlags.NonPublic | BindingFlags.Instance);
                    openConfigWindowField.SetValue(plugin, newOpenConfigWindowMethod.CreateDelegate(openConfigWindowMethod.DeclaringType));
                }
            }
            catch (Exception e)
            {
                PluginLog.Error($"{e.Message}\n{e.StackTrace}");
            }
        }

        private static IDalamudPlugin GetPluginByName(string internalName)
        {
            try
            {
                var pluginManager = Svc.PluginInterface.GetType().Assembly.
                    GetType("Dalamud.Service`1", true).MakeGenericType(Svc.PluginInterface.GetType().Assembly.GetType("Dalamud.Plugin.Internal.PluginManager", true)).
                    GetMethod("Get").Invoke(null, BindingFlags.Default, null, Array.Empty<object>(), null);
                var installedPlugins = (System.Collections.IList)pluginManager.GetType().GetProperty("InstalledPlugins").GetValue(pluginManager);

                foreach (var t in installedPlugins)
                {
                    if ((string)t.GetType().GetProperty("Name").GetValue(t) == internalName)
                    {
                        var type = t.GetType().Name == "LocalDevPlugin" ? t.GetType().BaseType : t.GetType();
                        var plugin = (IDalamudPlugin)type.GetField("instance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(t);
                        if ((bool)plugin.GetType().GetField("Init", BindingFlags.Static | BindingFlags.NonPublic).GetValue(plugin))
                        {
                            return plugin;
                        }
                        else
                        {
                            throw new Exception($"{internalName} is not initialized");
                        }
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                PluginLog.Error($"Can't find {internalName} plugin: " + e.Message);
                PluginLog.Error(e.StackTrace);
                return null;
            }
        }
    }
}
