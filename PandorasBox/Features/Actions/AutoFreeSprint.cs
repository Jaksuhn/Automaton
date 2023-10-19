using ECommons.DalamudServices;
using Automaton.FeaturesSetup;
using System;
using Automaton.Helpers;
using ImGuiNET;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Dalamud.Game.ClientState.Keys;
using System.Linq.Expressions;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace Automaton.Features.Actions
{
    public unsafe class AutoFreeSprint : Feature
    {
        public override string Name => "Auto Free Sprint";

        public override string Description => "What cooldown";

        public override FeatureType FeatureType => FeatureType.Disabled;

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
        {
        };
        private readonly OverrideMovement movement = new();

        public override void Enable()
        {
            Svc.Framework.Update += Sprint;
            base.Enable();
        }

        public override void Disable()
        {
            Svc.Framework.Update -= Sprint;
            base.Disable();
        }

        private void Sprint(IFramework framework)
        {
            try
            {
                ActionManager.Instance()->UseActionLocation(ActionType.GeneralAction, 4);
            }
            catch (Exception ex) { return; }
        }
    }
}
