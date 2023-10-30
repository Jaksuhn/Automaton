using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace Automaton
{
    [Serializable]
    public partial class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public List<string> EnabledFeatures = new();

        public bool showDebugFeatures = false;

        [NonSerialized]
        private DalamudPluginInterface pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            pluginInterface!.SavePluginConfig(this);
        }
    }
}
