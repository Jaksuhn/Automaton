using Automaton.FeaturesSetup;
using Automaton.Helpers;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using System.Numerics;
using System.Windows.Forms;

namespace Automaton.Features.Experiments;

public unsafe class ClickToMove : Feature
{
    public override string Name => "Click to Move";
    public override string Description => "Like those other games.";
    public override FeatureType FeatureType => FeatureType.Other;

    private readonly OverrideMovement movement = new();

    public override void Enable()
    {
        base.Enable();
        Svc.Framework.Update += MoveTo;
    }

    public override void Disable()
    {
        base.Disable();
        Svc.Framework.Update -= MoveTo;
        movement.Dispose();
    }

    private bool isPressed = false;
    private Vector3 destination = Vector3.Zero;
    private void MoveTo(IFramework framework)
    {
        if (Vector3.DistanceSquared(Svc.ClientState.LocalPlayer.Position, destination) < 0.0025) movement.Enabled = false;

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
                {
                    var mousePos = ImGui.GetIO().MousePos;
                    Svc.GameGui.ScreenToWorld(mousePos, out var pos, 100000f);
                    movement.Enabled = true;
                    movement.DesiredPosition = destination = pos;
                }
            }
        }
    }
}
