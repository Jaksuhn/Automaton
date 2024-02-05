using Automaton.Features.Debugging;
using Automaton.FeaturesSetup;
using ECommons.DalamudServices;
using System.Collections.Generic;

namespace Automaton.Features.Commands;

public unsafe class MoveSpeed : CommandFeature
{
    public override string Name => "Modify Movement Speed";
    public override string Command { get; set; } = "/movespeed";
    public override string[] Alias => new string[] { "/move", "/speed" };
    public override string Description => "";
    public override List<string> Parameters => new() { "[<speed>]" };
    public override bool isDebug => true;

    public override FeatureType FeatureType => FeatureType.Commands;

    // why is this not normalised to 1?!?!
    internal static float offset = 6;

    protected override void OnCommand(List<string> args)
    {
        try
        {
            if (args.Count == 0) { PositionDebug.SetSpeed(offset); return; }

            var speed = float.Parse(args[0]);
            PositionDebug.SetSpeed(speed * offset);
            Svc.Log.Info($"Setting move speed to {speed}");
        }
        catch { }
    }
}
