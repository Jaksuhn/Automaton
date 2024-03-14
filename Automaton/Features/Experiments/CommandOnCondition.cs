using Automaton.FeaturesSetup;
using Automaton.IPC;
using Dalamud.Interface.Components;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ECommons;
using ImGuiNET;
using System;
using System.Collections.Generic;
using ECommons.DalamudServices;

namespace Automaton.Features.Experiments;

internal class CommandOnCondition : Feature
{
    public override string Name => "Command on condition";
    public override string Description => "Execute a command when a condition is met.";
    public override FeatureType FeatureType => FeatureType.Disabled;

    public Configs Config { get; private set; }
    public class Configs : FeatureConfig
    {
        public List<CommandCondition> CommandConditions = [];
    }

    protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
    {
        try
        {
            if (ImGui.Button("Add Set"))
            {
                Config.CommandConditions.Add(new CommandCondition());
                hasChanged = true;
            }
            var copy = Config.CommandConditions;
            foreach (var preset in copy)
            {
                ImGui.PushID(preset.Guid);

                DrawPreset(preset);
                ImGui.SameLine();
                if (ImGuiComponents.IconButton($"###{preset.Guid}", FontAwesomeIcon.Trash))
                {
                    Config.CommandConditions.Remove(preset);
                    hasChanged = true;
                }
                var cmd = string.Empty;
                if (ImGui.InputTextWithHint($"###{preset.Guid}", "Command", ref cmd, 100))
                {
                    preset.Command = !cmd.StartsWith("/") ? $"/{cmd}" : cmd;
                    hasChanged = true;
                }

                ImGui.PopID();
            }
        }
        catch (Exception e) { e.Log(); }
    };

    public class CommandCondition()
    {
        public string Guid { get; } = System.Guid.NewGuid().ToString();
        public string Command { get; set; }
        public int ConditionSet = -1;
        public bool HasRun;
        public bool CheckConditionSet() => QoLBarIPC.QoLBarEnabled && QoLBarIPC.CheckConditionSet(ConditionSet);
    }

    public override void Enable()
    {
        Config = LoadConfig<Configs>() ?? new Configs();
        Svc.Framework.Update += CheckSets;
        base.Enable();
    }

    public override void Disable()
    {
        SaveConfig(Config);
        Svc.Framework.Update -= CheckSets;
        base.Disable();
    }

    private void CheckSets(IFramework framework)
    {
        //foreach (var preset in Config.CommandConditions)
        //{
        //    if (preset.CheckConditionSet())
        //    {
        //        preset.HasRun = true;
        //        ECommons.Automation.Chat.Instance.ExecuteCommand(preset.Command);
        //    }
        //    else
        //        preset.HasRun = false;
        //}
    }

    public void DrawPreset(CommandCondition preset)
    {
        try
        {
            var qolBarEnabled = QoLBarIPC.QoLBarEnabled;
            var conditionSets = qolBarEnabled ? QoLBarIPC.QoLBarConditionSets : [];
            var display = preset.ConditionSet >= 0
                ? preset.ConditionSet < conditionSets.Length
                ? $"[{preset.ConditionSet + 1}] {conditionSets[preset.ConditionSet]}"
                : (preset.ConditionSet + 1).ToString()
                : "None";

            using var combo = ImRaii.Combo($"Conditon Set###{preset.Guid}", display);
            if (!combo) return;
            if (ImGui.Selectable($"None##ConditionSet{preset.Guid}", preset.ConditionSet < 0))
            {
                preset.ConditionSet = -1;
                SaveConfig(Config);
            }

            if (qolBarEnabled)
                for (var i = 0; i < conditionSets.Length; i++)
                {
                    var name = conditionSets[i];
                    if (!ImGui.Selectable($"[{i + 1}] {name}", i == preset.ConditionSet)) continue;
                    preset.ConditionSet = i;
                    SaveConfig(Config);
                }
        }
        catch (Exception e) { e.Log(); }
    }
}
