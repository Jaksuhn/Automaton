using Automaton.FeaturesSetup;

namespace Automaton.Features.Actions
{
    public unsafe class InstantGroundTarget : Feature
    {
        public override string Name => "Instant Ground Target";

        public override string Description => "";

        public override FeatureType FeatureType => FeatureType.Disabled;

        //public override void Enable()
        //{
        //    Svc.Framework.Update += CheckForAbility;
        //    base.Enable();
        //}

        //public override void Disable()
        //{
        //    Svc.Framework.Update -= CheckForAbility;
        //    base.Disable();
        //}

        //private static ulong queuedGroundTargetObjectID = 0;

        //private void CheckForAbility(IFramework framework)
        //{
        //    if (!succeeded && queuedGroundTargetObjectID == 0)
        //        SetInstantGroundTarget(actionType, useType);
        //}

        //private static void SetInstantGroundTarget(uint actionType, uint useType)
        //{
        //    if ((ReAction.Config.EnableBlockMiscInstantGroundTargets && actionType == 11) || useType == 2 && actionType == 1 || actionType == 15) return;

        //    DalamudApi.LogDebug($"Making ground target instant {actionType}, {useType}");

        //    Common.ActionManager->activateGroundTarget = 1;

        //}
    }
}
