using Automaton.Features.Debugging;
using Automaton.FeaturesSetup;
using ECommons.DalamudServices;
using ImGuiNET;
using System.Collections.Generic;

namespace Automaton.Features.Experiments;

public unsafe class ClickToTP : CommandFeature
{
    public override string Name => "Click to TP";
    public override string Command { get; set; } = "/tpclick";
    public override string[] Alias => new string[] { "/tpc" };
    public override string Description => "";
    public override List<string> Parameters => new() { "" };
    public override bool isDebug => true;

    public override FeatureType FeatureType => FeatureType.Disabled;

    private bool active;

    protected override void OnCommand(List<string> args)
    {
        if (!active)
        {
            active = true;
            Svc.Framework.Update += ModifyPOS;
            Svc.Log.Info($"Enabling {nameof(ClickToTP)}");
        }
        else
        {
            active = false;
            Svc.Framework.Update -= ModifyPOS;
            Svc.Log.Info($"Disabling {nameof(ClickToTP)}");
        }
    }

    private void ModifyPOS(IFramework framework)
    {
        if (!active) return;

        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            PositionDebug.SetPosToMouse();
    }
}
