using Automaton.FeaturesSetup;
using Automaton.Helpers;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Automaton.Features.Commands
{
    public unsafe class Test : CommandFeature
    {
        public override string Name => "Testing";
        public override string Command { get; set; } = "/atest";
        public override string Description => "";
        public override bool isDebug => true;

        public override FeatureType FeatureType => FeatureType.Disabled;

        protected override void OnCommand(List<string> args)
        {
            if (ECommons.GenericHelpers.TryGetAddonByName<AtkUnitBase>("RetainerList", out var addon))
            {
                addon->GetButtonNodeById(45)->ClickAddonButton((AtkComponentBase*)addon, 26);
            }
        }
    }
}
