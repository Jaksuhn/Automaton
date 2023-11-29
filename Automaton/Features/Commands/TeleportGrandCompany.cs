using ECommons.DalamudServices;
using Automaton.FeaturesSetup;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace Automaton.Features.Commands
{
    public unsafe class TeleportGrandCompany : CommandFeature
    {
        public override string Name => "Teleport to Grand Company";
        public override string Command { get; set; } = "/tpgc";
        public override string[] Alias => new string[] { "" };
        public override string Description => "";
        public override List<string> Parameters => new() { "" };

        public override FeatureType FeatureType => FeatureType.Commands;

        protected override void OnCommand(List<string> args)
        {
            var gc = UIState.Instance()->PlayerState.GrandCompany;
            switch (gc)
            {
                case 1:
                    Svc.Commands.ProcessCommand("/tp limsa");
                    break;
                case 2:
                    Svc.Commands.ProcessCommand("/tp gridania");
                    break;
                case 3:
                    Svc.Commands.ProcessCommand("/tp uldah");
                    break;
            }
        }
    }
}
