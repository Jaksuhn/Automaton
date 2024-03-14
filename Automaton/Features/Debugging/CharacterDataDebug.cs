using Automaton.Debugging;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
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
            ImGui.TextUnformatted($"Free aetheryte: {PlayerState.Instance()->FreeAetheryteId}");
            ImGui.TextUnformatted($"Security token: {PlayerState.Instance()->IsPlayerStateFlagSet(PlayerStateFlag.IsLoginSecurityToken)}");
            ImGui.TextUnformatted($"flag 1: {PlayerState.Instance()->PlayerStateFlags1}");
            ImGui.TextUnformatted($"flag 2: {PlayerState.Instance()->PlayerStateFlags2}");
            ImGui.TextUnformatted($"flag 3: {PlayerState.Instance()->PlayerStateFlags3}");
        }

        if (ImGui.Button("test"))
            PlayerState.Instance()->FreeAetheryteId = 183;
        if (ImGui.Button("test2"))
            PlayerState.Instance()->FreeAetheryteId = 0;

        if (Svc.ClientState.LocalPlayer.StatusList is not null)
        {
            foreach (var status in Svc.ClientState.LocalPlayer.StatusList)
            {
                ImGui.TextUnformatted($"[{status.StatusId}] {status.GameData.Name} param:{status.Param} so:{status.SourceObject} sid:{status.SourceId}");
            }
        }
    }
}
