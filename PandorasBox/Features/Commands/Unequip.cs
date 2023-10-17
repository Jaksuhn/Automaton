using Dalamud.Logging;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Automaton.FeaturesSetup;
using System;
using System.Collections.Generic;
using Lumina.Excel.GeneratedSheets;
using System.Linq;

namespace Automaton.Features.Commands
{
    public unsafe class Unequip : CommandFeature
    {
        public override string Name => "Unequip";
        public override string Command { get; set; } = "/unequip";
        public override string[] Alias => new string[] { "/ada" };
        public override string Description => "Call any action directly.";
        public override List<string> Parameters => new() { "[<head/body/arms/legs/feet/earring/necklace/bracelet/Lring/Rring>]", "[<destination>]" };
        public override bool isDebug => true;

        public override FeatureType FeatureType => FeatureType.Disabled;

        private enum EquippedSlots
        {
            ArmoryMainHand,
            ArmoryOffHand,
            ArmoryHead,
            ArmoryBody,
            ArmoryHands,
            ArmoryWaist,
            ArmoryLegs,
            ArmoryFeet,
            ArmoryEar,
            ArmoryNeck,
            ArmoryWrist,
            ArmoryRingsL,
            ArmoryRingsR = 11,
            ArmorySoulCrystal = 13
        }

        protected override void OnCommand(List<string> args)
        {
            try
            {
                var im = InventoryManager.Instance();
                var c = im->GetInventoryContainer(InventoryType.EquippedItems);
                for (var i = 0; i < c->Size; i++)
                {
                    Svc.Log.Info($"{c->Items[i].ItemID} : {c->Items[i].Slot}");
                }
                

                var slot = Svc.Data.GetExcelSheet<ItemUICategory>(Svc.ClientState.ClientLanguage).First(x => x.Name.RawString.Contains(args[0], StringComparison.CurrentCultureIgnoreCase));
                //foreach (var container in im->Inventories)
                var dest = args[1] == "i" ? InventoryType.Inventory1 : InventoryType.ArmoryBody;
                //InventoryManager.Instance()->MoveItemSlot(InventoryType.EquippedItems, 0, InventoryType.ArmoryHead, 0);
            }
            catch (Exception e) { e.Log(); }
        }

        private static InventoryContainer* GetFreeInventoryContainer()
        {
            var im = InventoryManager.Instance();
            var inv1 = im->GetInventoryContainer(InventoryType.Inventory1);
            var inv2 = im->GetInventoryContainer(InventoryType.Inventory2);
            var inv3 = im->GetInventoryContainer(InventoryType.Inventory3);
            var inv4 = im->GetInventoryContainer(InventoryType.Inventory4);

            InventoryContainer*[] container = { inv1, inv2, inv3, inv4 };

            foreach (var c in container)
            {
                for (var i = 0; i < c->Size; i++)
                {
                    if (c->Items[i].ItemID == 0)
                        return c;
                }
            }
            return null;
        }
    }
}
