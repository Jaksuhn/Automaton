using Automaton.FeaturesSetup;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Automaton.Features.Experiments;

public unsafe class NoDozeSnap : CommandFeature
{
    public override string Name => "No Doze Snap";
    public override string Command { get; set; } = "/dozehere";
    public override string Description => "Dozers without Borders";

    public override FeatureType FeatureType => FeatureType.Disabled;

    [Signature("E8 ?? ?? ?? ?? 4C 8B 74 24 ?? 48 8B CE E8")]
    private readonly delegate* unmanaged<nint, ushort, nint, byte, byte, void> useEmote = null!;

    private delegate byte ShouldSnap(Character* a1, SnapPosition* a2);

    [Signature("E8 ?? ?? ?? ?? 84 C0 74 44 4C 8D 6D C7", DetourName = nameof(ShouldSnapDetour))]
    private Hook<ShouldSnap> ShouldSnapHook { get; init; } = null;

    private static byte ShouldSnapDetour(Character* a1, SnapPosition* a2) => 0;

    [StructLayout(LayoutKind.Explicit, Size = 0x38)]
    public struct SnapPosition
    {
        [FieldOffset(0x00)]
        public Vector3 PositionA;

        [FieldOffset(0x10)]
        public float RotationA;

        [FieldOffset(0x20)] public Vector3 PositionB;

        [FieldOffset(0x30)]
        public float RotationB;
    }

    public override void Enable()
    {
        ShouldSnapHook?.Enable();
        base.Enable();
    }

    public override void Dispose()
    {
        ShouldSnapHook?.Dispose();
        base.Dispose();
    }

    protected override void OnCommand(List<string> args)
    {
        var agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.Emote);
        useEmote(new nint(agent), 88, nint.Zero, 0, 0);
    }
}
