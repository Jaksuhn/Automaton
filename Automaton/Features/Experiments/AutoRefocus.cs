using Automaton.FeaturesSetup;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace Automaton.Features.Experiments
{
    public class AutoRefocus : Feature
    {
        public override string Name => "Auto Refocus";
        public override string Description => "Keeps your focus target persistent between zones.";
        public override FeatureType FeatureType => FeatureType.Disabled;

        private unsafe delegate void SetFocusTargetByObjectIDDelegate(TargetSystem* targetSystem, long objectID);
        [Signature("E8 ?? ?? ?? ?? BA 0C 00 00 00 48 8D 0D", DetourName = nameof(SetFocusTargetByObjectID))]
        private readonly Hook<SetFocusTargetByObjectIDDelegate> setFocusTargetByObjectIDHook;

        private static ulong? FocusTarget;

        public override void Enable()
        {
            base.Enable();
            Svc.Hook.InitializeFromAttributes(this);
            setFocusTargetByObjectIDHook?.Enable();

            Svc.ClientState.TerritoryChanged += OnZoneChange;
        }

        public override void Disable()
        {
            base.Disable();
            setFocusTargetByObjectIDHook.Dispose();
            Svc.ClientState.TerritoryChanged -= OnZoneChange;
            Svc.Framework.Update -= OnUpdate;
        }

        private void OnZoneChange(ushort obj)
        {
            FocusTarget = null;
            Svc.Framework.Update += OnUpdate;
        }

        private void OnUpdate(IFramework framework)
        {
            if (FocusTarget != null && Svc.Targets.FocusTarget == null)
                unsafe { setFocusTargetByObjectIDHook.Original(TargetSystem.StaticAddressPointers.pInstance, (long)FocusTarget); }
            else
                Svc.Framework.Update -= OnUpdate;
        }

        private unsafe void SetFocusTargetByObjectID(TargetSystem* targetSystem, long objectID)
        {
            if (objectID == 0xE0000000)
            {
                objectID = Svc.Targets.Target?.ObjectId ?? 0xE0000000;
                FocusTarget = Svc.Targets.Target?.ObjectId;
            }
            else
            {
                FocusTarget = Svc.Targets.Target.ObjectId;
            }
            setFocusTargetByObjectIDHook.Original(targetSystem, objectID);
        }
    }
}
