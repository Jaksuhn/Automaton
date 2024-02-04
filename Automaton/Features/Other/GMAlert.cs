using Automaton.FeaturesSetup;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automaton.Features.Other
{
    public class GMAlert : Feature
    {
        public override string Name => "GM Alert";
        public override string Description => "Chat message when a GM is nearby";
        public override FeatureType FeatureType => FeatureType.Other;

        public override void Enable()
        {
            base.Enable();
            Svc.Framework.Update += OnUpdate;
        }

        public override void Disable()
        {
            base.Disable();
            Svc.Framework.Update -= OnUpdate;
        }

        private void OnUpdate(IFramework framework)
        {
            if (Svc.ClientState.LocalPlayer == null) return;

            foreach (var player in Svc.Objects.OfType<PlayerCharacter>().Where(pc => pc.ObjectId != 0xE000000))
                unsafe {
                    if (((Character*)player.Address)->CharacterData.OnlineStatus is <= 3 and > 0)
                        PrintModuleMessage(player.Name);
                }
        }
    }
}
