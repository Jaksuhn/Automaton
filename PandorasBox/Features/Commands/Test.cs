using Automaton.FeaturesSetup;
using System;
using System.Collections.Generic;
using Automaton.IPC;
using ECommons.DalamudServices;

namespace Automaton.Features.Commands
{
    public unsafe class Test : CommandFeature
    {
        public override string Name => "Testing";
        public override string Command { get; set; } = "/atest";
        public override string Description => "";
        public override bool isDebug => true;

        public override FeatureType FeatureType => FeatureType.Commands;

        protected override void OnCommand(List<string> args)
        {
            return;
        }
    }
}
