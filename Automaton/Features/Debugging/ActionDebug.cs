using Automaton.Debugging;
using Automaton.Helpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using System.Linq;
using System;
using Dalamud.Interface.Utility.Raii;

namespace Automaton.Features.Debugging;

public unsafe class ActionDebug : DebugHelper
{
    public override string Name => $"{nameof(ActionDebug).Replace("Debug", "")} Debugging";

    private readonly unsafe ActionManager* inst = ActionManager.Instance();
    public unsafe float AnimationLock => AddressHelper.ReadField<float>(inst, 8);

    private ActionType actionType;
    private uint actionID;

    private int selectedChannel = 0;

    public override void Draw()
    {
        ImGui.Text($"{Name}");
        ImGui.Separator();

        ImGui.Text($"Anim lock: {AnimationLock:f3}");

        ActionManager.Instance()->UseActionLocation(actionType, actionID);

        var actionTypes = ((ActionType[])Enum.GetValues(typeof(ActionType))).ToList();

        var prevType = actionTypes[0];
        var selectedType = prevType;
        var selectedTypeIndex = 0; // Initialize selectedTypeIndex with the index of the initial selection

        using (ImRaii.Combo("Action Type", prevType.ToString()))
        {
            for (var i = 0; i < actionTypes.Count; i++)
            {
                if (ImGui.Selectable(actionTypes[i].ToString(), selectedTypeIndex == i))
                {
                    selectedTypeIndex = i; // Update selectedTypeIndex based on user selection
                    selectedType = actionTypes[selectedTypeIndex];
                }
            }
        }
    }
}
