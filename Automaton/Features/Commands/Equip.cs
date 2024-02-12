using Automaton.FeaturesSetup;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Automaton.Features.Commands;

public unsafe class Equip : CommandFeature
{
    public override string Name => "Equip";
    public override string Command { get; set; } = "/equip";
    public override string Description => "Equip an item via id";
    public override List<string> Parameters => new() { "" };

    public override FeatureType FeatureType => FeatureType.Commands;

    private static int EquipAttemptLoops = 0;

    protected override void OnCommand(List<string> args)
    {
        try
        {
            if (uint.TryParse(args[0], out var itemID))
                EquipItem(itemID);
            //else
            //{
            //    var parsedID = GetItemIDFromString(string.Join(" ", args));
            //    Svc.Log.Info(parsedID.ToString());
            //    if (parsedID != 0)
            //        EquipItem(parsedID);
            //}
        }
        catch (Exception e) { e.Log(); }
    }

    private static uint GetItemIDFromString(string arg) => Svc.Data.GetExcelSheet<Item>(Svc.ClientState.ClientLanguage).FirstOrDefault(x => x.Name == arg).RowId;

    private static void EquipItem(uint itemId)
    {
        var pos = FindItemInInventory(itemId, [InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3, InventoryType.Inventory4, InventoryType.ArmoryMainHand, InventoryType.ArmoryOffHand, InventoryType.ArmoryHead, InventoryType.ArmoryBody, InventoryType.ArmoryHands, InventoryType.ArmoryLegs, InventoryType.ArmoryFeets, InventoryType.ArmoryEar, InventoryType.ArmoryNeck, InventoryType.ArmoryWrist, InventoryType.ArmoryRings, InventoryType.ArmorySoulCrystal]);
        if (pos == null)
        {
            DuoLog.Error($"Failed to find item {Svc.Data.GetExcelSheet<Item>().GetRow(itemId).Name} (ID: {itemId}) in inventory");
            return;
        }

        var agentId = pos.Value.inv is InventoryType.ArmoryMainHand or InventoryType.ArmoryOffHand or InventoryType.ArmoryHead or InventoryType.ArmoryBody or InventoryType.ArmoryHands or InventoryType.ArmoryLegs or InventoryType.ArmoryFeets or InventoryType.ArmoryEar or InventoryType.ArmoryNeck or InventoryType.ArmoryWrist or InventoryType.ArmoryRings or InventoryType.ArmorySoulCrystal ? AgentId.ArmouryBoard : AgentId.Inventory;
        var addonId = AgentModule.Instance()->GetAgentByInternalId(agentId)->GetAddonID();
        var ctx = AgentInventoryContext.Instance();
        ctx->OpenForItemSlot(pos.Value.inv, pos.Value.slot, addonId);

        var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextMenu");
        if (contextMenu != null)
        {
            for (var i = 0; i < contextMenu->AtkValuesCount; i++)
            {
                var firstEntryIsEquip = ctx->EventIdSpan[i] == 25; // i'th entry will fire eventid 7+i; eventid 25 is 'equip'
                if (firstEntryIsEquip)
                {
                    Svc.Log.Debug($"Equipping item #{itemId} from {pos.Value.inv} @ {pos.Value.slot}, index {i}");
                    Callback.Fire(contextMenu, true, 0, i - 7, 0, 0, 0); // p2=-1 is close, p2=0 is exec first command
                }
            }
            Callback.Fire(contextMenu, true, 0, -1, 0, 0, 0);
            EquipAttemptLoops++;

            if (EquipAttemptLoops >= 5)
            {
                DuoLog.Error($"Equip option not found after 5 attempts. Aborting.");
                return;
            }
        }
    }

    private static (InventoryType inv, int slot)? FindItemInInventory(uint itemId, IEnumerable<InventoryType> inventories)
    {
        foreach (var inv in inventories)
        {
            var cont = InventoryManager.Instance()->GetInventoryContainer(inv);
            for (var i = 0; i < cont->Size; ++i)
            {
                if (cont->GetInventorySlot(i)->ItemID == itemId)
                {
                    return (inv, i);
                }
            }
        }
        return null;
    }
}
