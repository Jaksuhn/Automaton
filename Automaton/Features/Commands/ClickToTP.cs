using ECommons.DalamudServices;
using Automaton.FeaturesSetup;
using System.Collections.Generic;
using Automaton.Features.Debugging;
using ImGuiNET;

namespace Automaton.Features.Commands
{
    public unsafe class ClickToTP : CommandFeature
    {
        public override string Name => "Click to TP";
        public override string Command { get; set; } = "/tpclick";
        public override string[] Alias => new string[] { "/tpc" };
        public override string Description => "";
        public override List<string> Parameters => new() { "" };
        public override bool isDebug => true;

        public override FeatureType FeatureType => FeatureType.Disabled;

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

        private void ModifyPOS(IFramework framework)
        {
            if (!active) return;

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                PositionDebug.SetPosToMouse();
        }
    }
}
