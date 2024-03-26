using Automaton.FeaturesSetup;
using Automaton.Helpers;
using Automaton.IPC;
using ECommons;
using ECommons.DalamudServices;
using ImGuiNET;
using System.Numerics;
using System.Windows.Forms;

namespace Automaton.Features.Other;

public unsafe class ClickToMove : Feature
{
    public override string Name => "Click to Move";
    public override string Description => "Like those other games.";
    public override FeatureType FeatureType => FeatureType.Other;

    private readonly OverrideMovement movement = new();

    public Configs Config { get; private set; }
    public override bool UseAutoConfig => true;
    public class Configs : FeatureConfig
    {
        [FeatureConfigOption("Pathfind", HelpText = "Requires vnavmesh to be installed.")]
        public bool Pathfind = false;
    }

    public override void Enable()
    {
        base.Enable();
        Config = LoadConfig<Configs>() ?? new Configs();
        Svc.Framework.Update += MoveTo;
    }

    public override void Disable()
    {
        base.Disable();
        SaveConfig(Config);
        Svc.Framework.Update -= MoveTo;
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
                    if (Config.Pathfind)
                    {
                        if (!NavmeshIPC.PathIsRunning())
                            NavmeshIPC.PathfindAndMoveTo(pos, false);
                        else
                        {
                            NavmeshIPC.PathStop();
                            NavmeshIPC.PathfindAndMoveTo(pos, false);
                        }
                        return;
                    }
                    movement.Enabled = true;
                    movement.DesiredPosition = destination = pos;
                }
            }
        }
    }
}
