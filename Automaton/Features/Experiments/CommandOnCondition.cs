using Automaton.FeaturesSetup;
using ImGuiNET;
using System;
using System.Collections.Generic;
using Automaton.IPC;

namespace Automaton.Features.Testing
{
    internal class CommandOnCondition : Feature
    {
        public override string Name => "Command on condition";
        public override string Description => "Execute a command when a condition is met.";
        public override FeatureType FeatureType => FeatureType.Other;

        public Configs Config { get; private set; }
        public override bool UseAutoConfig => false;
        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Sets")]
            public List<CommandCondition> CommandConditions { get; set; } = [];
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            base.Disable();
        }

        [Serializable]
        public class CommandCondition(string name = "Unnamed Set")
        {
            public string Name { get; set; } = name;
            public string Command { get; set; }
            public int ConditionSet = -1;

            public bool CheckConditionSet() => ConditionSet < 0 || QoLBarIPC.QoLBarEnabled && QoLBarIPC.CheckConditionSet(ConditionSet);
        }

        public void DrawPreset(CommandCondition preset)
        {
            var qolBarEnabled = QoLBarIPC.QoLBarEnabled;
            var conditionSets = qolBarEnabled ? QoLBarIPC.QoLBarConditionSets : [];
            var display = preset.ConditionSet >= 0
                ? preset.ConditionSet < conditionSets.Length
                ? $"[{preset.ConditionSet + 1}] {conditionSets[preset.ConditionSet]}"
                : (preset.ConditionSet + 1).ToString()
                : "None";

            if (ImGui.BeginCombo("Condition Set", display))
            {
                if (ImGui.Selectable("None##ConditionSet", preset.ConditionSet < 0))
                {
                    preset.ConditionSet = -1;
                    SaveConfig(Config);
                }

                if (qolBarEnabled)
                {
                    for (var i = 0; i < conditionSets.Length; i++)
                    {
                        var name = conditionSets[i];
                        if (!ImGui.Selectable($"[{i + 1}] {name}", i == preset.ConditionSet)) continue;
                        preset.ConditionSet = i;
                        SaveConfig(Config);
                    }
                }

                ImGui.EndCombo();
            }
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
        {
            foreach (var preset in Config.CommandConditions)
            {
                DrawPreset(preset);
            }
        };
    }
}
