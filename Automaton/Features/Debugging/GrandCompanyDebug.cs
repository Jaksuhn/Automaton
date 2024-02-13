//using Automaton.Debugging;
//using Dalamud.Game.Text.SeStringHandling;
//using ECommons;
//using FFXIVClientStructs.FFXIV.Client.UI.Agent;
//using FFXIVClientStructs.FFXIV.Component.GUI;
//using ImGuiNET;

//namespace Automaton.Features.Debugging;
//internal class GrandCompanyDebug : DebugHelper
//{
//    public override string Name => $"{nameof(GrandCompanyDebug).Replace("Debug", "")} Debugging";

//    public override void Draw()
//    {
//        ImGui.Text($"{Name}");
//        ImGui.Separator();

//        unsafe
//        {
//            if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("GrandCompanySupply", out var addon) && addon == null) return;
//            var agent = AgentGrandCompanySupply.Instance();
//            if (agent == null) return;

//            for (var i = 11; i < 0xA0; i++)
//                ImGui.Text($"[{agent->ItemArray[i].ItemId}] {SeString.Parse(agent->ItemArray[i].ItemName)} @ {agent->ItemArray[i].Position}");
//        }
//    }
//}
