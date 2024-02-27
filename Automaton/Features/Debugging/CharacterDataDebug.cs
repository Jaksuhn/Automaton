using Automaton.Debugging;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ImGuiNET;

namespace Automaton.Features.Debugging;

public unsafe class CharacterDataDebug : DebugHelper
{
    public override string Name => $"{nameof(CharacterDataDebug).Replace("Debug", "")} Debugging";

    public override void Draw()
    {
        ImGui.Text($"{Name}");
        ImGui.Separator();

        if (Svc.ClientState.LocalPlayer is not null)
        {
            ImGui.TextUnformatted($"Transformation ID : {Svc.ClientState.LocalPlayer.GetTransformationID()}");
            ImGui.TextUnformatted($"ModelCharaId: {Svc.ClientState.LocalPlayer.Struct()->Character.CharacterData.ModelCharaId}");
            ImGui.TextUnformatted($"ModelSkeletonId: {Svc.ClientState.LocalPlayer.Struct()->Character.CharacterData.ModelSkeletonId}");
            ImGui.TextUnformatted($"ModelCharaId_2: {Svc.ClientState.LocalPlayer.Struct()->Character.CharacterData.ModelCharaId_2}");
            ImGui.TextUnformatted($"ModelSkeletonId_2: {Svc.ClientState.LocalPlayer.Struct()->Character.CharacterData.ModelSkeletonId_2}");
        }

        if (Svc.ClientState.LocalPlayer.StatusList is not null)
        {
            foreach (var status in Svc.ClientState.LocalPlayer.StatusList)
            {
                ImGui.TextUnformatted($"[{status.StatusId}] {status.GameData.Name} param:{status.Param} so:{status.SourceObject} sid:{status.SourceId}");
            }
        }
    }
}
