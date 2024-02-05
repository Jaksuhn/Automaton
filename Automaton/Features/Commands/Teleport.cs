using Automaton.Features.Debugging;
using Automaton.FeaturesSetup;
using ECommons;
using ECommons.DalamudServices;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Automaton.Features.Commands;

public unsafe class Teleport : CommandFeature
{
    public override string Name => "Teleport";
    public override string Command { get; set; } = "/ateleport";
    public override string[] Alias => new string[] { "/atp" };
    public override string Description => "";
    public override List<string> Parameters => new() { "<x offset>, <z offset>, <y offset>" };
    public override bool isDebug => true;

    public override FeatureType FeatureType => FeatureType.Commands;

    protected override void OnCommand(List<string> args)
    {
        try
        {
            var curPos = Svc.ClientState.LocalPlayer.Position;
            Svc.Log.Info($"Moving from {curPos.X}, {curPos.Y}, {curPos.Z}");

            if (args[0].IsNullOrEmpty())
            {
                PositionDebug.SetPosToMouse();
                return;
            }

            float.TryParse(args.ElementAtOrDefault(0), out var x);
            float.TryParse(args.ElementAtOrDefault(1), out var z);
            float.TryParse(args.ElementAtOrDefault(2), out var y);

            var newPos = curPos + new Vector3(x, z, y);
            Svc.Log.Info($"Moving to {newPos.X}, {newPos.Y}, {newPos.Z}");
            PositionDebug.SetPos(newPos);
        }
        catch { }
    }
}
