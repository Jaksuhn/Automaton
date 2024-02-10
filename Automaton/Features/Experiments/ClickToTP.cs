using Automaton.Features.Debugging;
using Automaton.FeaturesSetup;
using Automaton.Helpers;
using ECommons;
using ECommons.DalamudServices;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Automaton.Features.Experiments;

public unsafe class ClickToTP : CommandFeature
{
    public override string Name => "Click to TP";
    public override string Command { get; set; } = "/tpclick";
    public override string Description => "";
    public override bool isDebug => true;

    public override FeatureType FeatureType => FeatureType.Commands;

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

    private bool isPressed = false;
    private void ModifyPOS(IFramework framework)
    {
        if (!active) return;
        if (GenericHelpers.IsKeyPressed(Keys.LButton) && Misc.IsClickingInGameWorld())
        {
            if (!isPressed)
            {
                isPressed = true;
                //key was just pressed
            }
        }
        else
        {
            if (isPressed)
            {
                isPressed = false;
                //key was just unpressed
                if (Misc.ApplicationIsActivated())
                    PositionDebug.SetPosToMouse();
            }
        }
    }
}
