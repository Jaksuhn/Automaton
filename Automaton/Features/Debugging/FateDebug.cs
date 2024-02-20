using Automaton.Debugging;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using ImGuiNET;
using System.Linq;
using System.Numerics;

namespace Automaton.Features.Debugging;
internal unsafe class FateDebug : DebugHelper
{
    public override string Name => $"{nameof(FateDebug).Replace("Debug", "")} Debugging";

    FateManager* fm = FateManager.Instance();

    public override void Draw()
    {
        ImGui.Text($"{Name}");
        ImGui.Separator();

        if (fm == null) return;
        var active = FateManager.Instance()->Fates.Span.ToArray()
            .Where(f => f.Value is not null)
            .OrderBy(f => Vector3.Distance(Svc.ClientState.LocalPlayer!.Position, f.Value->Location))
            .Select(f => f.Value->FateId)
            .ToList();

        if (fm->CurrentFate != null)
            ImGui.Text($"Current Fate: [{fm->CurrentFate->FateId}] {fm->CurrentFate->Name} ({fm->CurrentFate->Duration}) {fm->CurrentFate->Progress}%% <{fm->CurrentFate->State}>");

        ImGui.Separator();

        foreach (var fate in active)
        {
            ImGui.Text($"[{fate}] {fm->GetFateById(fate)->Name} ({fm->GetFateById(fate)->Duration}) {fm->GetFateById(fate)->Progress}%% <{fm->GetFateById(fate)->State}>");
            ImGui.SameLine();
            var loc = fm->GetFateById(fate)->Location;
            var cmd = $"/vnavmesh moveto {loc.X} {loc.Y} {loc.Z}";
            Svc.Log.Info($"executing command {cmd}");
            if (ImGui.Button("path"))
                ECommons.Automation.Chat.Instance.SendMessage($"{cmd}");
        }
    }
}
