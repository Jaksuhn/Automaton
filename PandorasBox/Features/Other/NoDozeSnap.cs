using ECommons.DalamudServices;
using Automaton.FeaturesSetup;
using Dalamud.Hooking;

namespace Automaton.Features.Other
{
    public unsafe class NoDozeSnap : Feature
    {
        public override string Name => "No Doze Snap";
        public override string Description => "";

        public override FeatureType FeatureType => FeatureType.Other;

        public override void Enable()
        {
            shouldSnapHook ??= Svc.Hook.HookFromSignature<ShouldSnap>("E8 ?? ?? ?? ?? 84 C0 74 46 4C 8D 6D C7", ShouldSnapDetour);
            shouldSnapHook?.Enable();
            base.Enable();
        }

        public override void Disable()
        {
            shouldSnapHook?.Disable();
            base.Disable();
        }

        private delegate bool ShouldSnap();
        private Hook<ShouldSnap> shouldSnapHook;
        private static bool ShouldSnapDetour() => false;
    }
}
