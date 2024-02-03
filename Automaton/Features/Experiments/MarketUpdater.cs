using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Internal.Notifications;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ECommons.DalamudServices;
using Dalamud.Game.ClientState.Keys;
using ECommons;
using static ECommons.GenericHelpers;
using Automaton.FeaturesSetup;
using System.Collections.Generic;

namespace Automaton.Features.Testing;

public partial class AutoRetainerPriceAdjust : Feature
{
    public override string Name => "Auto Penny Pinch";
    public override string Description => "Adjusts your retainers' items upon opening listings.";
    public override FeatureType FeatureType => FeatureType.UI;

    public bool Initialized { get; set; }

    private static int CurrentItemPrice;
    private static int CurrentMarketLowestPrice;
    private static uint CurrentItemSearchItemID;
    private static bool IsCurrentItemHQ;
    private static unsafe RetainerManager.Retainer* CurrentRetainer;
    private VirtualKey ConflictKey { get; set; } = VirtualKey.SHIFT;

    public override bool UseAutoConfig => true;

    public Configs Config { get; private set; }

    public class Configs : FeatureConfig
    {
        [FeatureConfigOption("Price Reduction", IntMin = 0, IntMax = 600, EditorSize = 300)]
        public int PriceReduction = 1;
        [FeatureConfigOption("Price Reduction", IntMin = 0, IntMax = 600, EditorSize = 300)]
        public int LowestAcceptablePrice = 100;
        [FeatureConfigOption("Separate NQ And HQ")]
        public bool SeparateNQAndHQ = false;
        [FeatureConfigOption("Max Price Reduction", IntMin = 0, IntMax = 600, EditorSize = 300)]
        public int MaxPriceReduction = 0;
    }

    public override void Enable()
    {
        base.Enable();
        Config = LoadConfig<Configs>() ?? new Configs();
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSellList", OnRetainerSellList);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSell", OnRetainerSell);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "RetainerSell", OnRetainerSell);
        Svc.Framework.Update += OnUpdate;
        Initialized = true;
    }

    public override void Disable()
    {
        base.Disable();
        SaveConfig(Config);
        Svc.Framework.Update -= OnUpdate;
        Svc.AddonLifecycle.UnregisterListener(OnRetainerSellList);
        Svc.AddonLifecycle.UnregisterListener(OnRetainerSell);
        TaskManager?.Abort();
        Initialized = false;
    }

    private void OnUpdate(IFramework framework)
    {
        if (!TaskManager.IsBusy) return;

        if (Svc.KeyState[ConflictKey])
        {
            TaskManager.Abort();
            Svc.PluginInterface.UiBuilder.AddNotification("ConflictKey-InterruptMessage", "Daily Routines", NotificationType.Success);
        }
    }

    private void OnRetainerSell(AddonEvent eventType, AddonArgs addonInfo)
    {
        switch (eventType)
        {
            case AddonEvent.PostSetup:
                if (TaskManager.IsBusy) return;
                TaskManager.Enqueue(ClickComparePrice);
                TaskManager.AbortOnTimeout = false;
                TaskManager.DelayNext(500);
                TaskManager.Enqueue(GetLowestPrice);
                TaskManager.AbortOnTimeout = true;
                TaskManager.DelayNext(100);
                TaskManager.Enqueue(FillLowestPrice);
                break;
            case AddonEvent.PreFinalize:
                if (TaskManager.NumQueuedTasks <= 1)
                    TaskManager.Abort();
                break;
        }
    }

    private unsafe void OnRetainerSellList(AddonEvent type, AddonArgs args)
    {
        var activeRetainer = RetainerManager.Instance()->GetActiveRetainer();
        if (CurrentRetainer == null || CurrentRetainer != activeRetainer)
            CurrentRetainer = activeRetainer;
        else
            return;

        GetSellListItems(out var itemCount);
        if (itemCount == 0) return;

        for (var i = 0; i < itemCount; i++)
        {
            EnqueueSingleItem(i);
            CurrentMarketLowestPrice = 0;
        }
    }

    private void EnqueueSingleItem(int index)
    {
        TaskManager.Enqueue(() => ClickSellingItem(index));
        TaskManager.DelayNext(100);
        TaskManager.Enqueue(ClickAdjustPrice);
        TaskManager.DelayNext(100);
        TaskManager.Enqueue(ClickComparePrice);
        TaskManager.DelayNext(500);
        TaskManager.AbortOnTimeout = false;
        TaskManager.Enqueue(GetLowestPrice);
        TaskManager.AbortOnTimeout = true;
        TaskManager.DelayNext(100);
        TaskManager.Enqueue(FillLowestPrice);
        TaskManager.DelayNext(800);
    }

    private static unsafe void GetSellListItems(out uint availableItems)
    {
        availableItems = 0;
        if (TryGetAddonByName<AtkUnitBase>("RetainerSellList", out var addon) && IsAddonReady(addon))
        {
            for (var i = 0; i < 20; i++)
            {
                var slot =
                    InventoryManager.Instance()->GetInventoryContainer(InventoryType.RetainerMarket)->GetInventorySlot(
                        i);
                if (slot->ItemID != 0) availableItems++;
            }
        }
    }

    private static unsafe bool? ClickSellingItem(int index)
    {
        if (TryGetAddonByName<AtkUnitBase>("RetainerSellList", out var addon) && IsAddonReady(addon))
        {
            Callback.Fire(addon, true, 0, index, 1);
            return true;
        }

        return false;
    }

    private static unsafe bool? ClickAdjustPrice()
    {
        if (TryGetAddonByName<AtkUnitBase>("ContextMenu", out var addon) && IsAddonReady(addon))
        {
            Callback.Fire(addon, true, 0, 0, 0, 0, 0);
            return true;
        }

        return false;
    }

    private static unsafe bool? ClickComparePrice()
    {
        if (TryGetAddonByName<AtkUnitBase>("RetainerSell", out var addon) && IsAddonReady(addon))
        {
            CurrentItemPrice = addon->AtkValues[5].Int;
            IsCurrentItemHQ = Marshal.PtrToStringUTF8((nint)addon->AtkValues[1].String).Contains(''); // hq symbol

            Callback.Fire(addon, true, 4);
            return true;
        }

        return false;
    }

    private unsafe bool? GetLowestPrice()
    {
        if (TryGetAddonByName<AtkUnitBase>("ItemSearchResult", out var addon) && IsAddonReady(addon))
        {
            CurrentItemSearchItemID = AgentItemSearch.Instance()->ResultItemID;
            var searchResult = addon->GetTextNodeById(29)->NodeText.ExtractText();
            if (string.IsNullOrEmpty(searchResult)) return false;

            if (int.Parse(AutoRetainerPriceAdjustRegex().Replace(searchResult, "")) == 0)
            {
                CurrentMarketLowestPrice = 0;
                addon->Close(true);
                return true;
            }

            if (Config.SeparateNQAndHQ && IsCurrentItemHQ)
            {
                var foundHQItem = false;
                for (var i = 1; i <= 12 && !foundHQItem; i++)
                {
                    var listing =
                        addon->UldManager.NodeList[5]->GetAsAtkComponentNode()->Component->UldManager.NodeList[i]
                            ->GetAsAtkComponentNode()->Component->UldManager.NodeList;
                    if (listing[13]->GetAsAtkImageNode()->AtkResNode.IsVisible)
                    {
                        var priceText = listing[10]->GetAsAtkTextNode()->NodeText.ExtractText();
                        if (int.TryParse(AutoRetainerPriceAdjustRegex().Replace(priceText, ""),
                                         out CurrentMarketLowestPrice))
                            foundHQItem = true;
                    }
                }

                if (!foundHQItem)
                {
                    var priceText = addon->UldManager.NodeList[5]->GetAsAtkComponentNode()->Component->UldManager
                                .NodeList[1]
                            ->GetAsAtkComponentNode()->Component->UldManager.NodeList[10]->GetAsAtkTextNode()->NodeText
                        .ExtractText();
                    if (!int.TryParse(AutoRetainerPriceAdjustRegex().Replace(priceText, ""),
                                      out CurrentMarketLowestPrice)) return false;
                }
            }
            else
            {
                var priceText =
                    addon->UldManager.NodeList[5]->GetAsAtkComponentNode()->Component->UldManager.NodeList[1]
                            ->GetAsAtkComponentNode()->Component->UldManager.NodeList[10]->GetAsAtkTextNode()->NodeText
                        .ExtractText();
                if (!int.TryParse(AutoRetainerPriceAdjustRegex().Replace(priceText, ""),
                                  out CurrentMarketLowestPrice)) return false;
            }

            addon->Close(true);
            return true;
        }

        return false;
    }

    private unsafe bool? FillLowestPrice()
    {
        if (TryGetAddonByName<AddonRetainerSell>("RetainerSell", out var addon) && IsAddonReady(&addon->AtkUnitBase))
        {
            var ui = &addon->AtkUnitBase;
            var priceComponent = addon->AskingPrice;

            if (CurrentMarketLowestPrice - Config.PriceReduction < Config.LowestAcceptablePrice)
            {
                var message = GetSeString("AutoRetainerPriceAdjust-WarnMessageReachLowestPrice",
                                                       SeString.CreateItemLink(
                                                           CurrentItemSearchItemID,
                                                           IsCurrentItemHQ
                                                               ? ItemPayload.ItemKind.Hq
                                                               : ItemPayload.ItemKind.Normal), CurrentMarketLowestPrice,
                                                       CurrentItemPrice, Config.LowestAcceptablePrice);
                Svc.Chat.Print(message);

                Callback.Fire((AtkUnitBase*)addon, true, 1);
                ui->Close(true);

                return true;
            }

            if (Config.MaxPriceReduction != 0 && CurrentItemPrice - CurrentMarketLowestPrice > Config.LowestAcceptablePrice)
            {
                var message = GetSeString("AutoRetainerPriceAdjust-MaxPriceReductionMessage",
                                                       SeString.CreateItemLink(
                                                           CurrentItemSearchItemID,
                                                           IsCurrentItemHQ
                                                               ? ItemPayload.ItemKind.Hq
                                                               : ItemPayload.ItemKind.Normal), CurrentMarketLowestPrice,
                                                       CurrentItemPrice, Config.MaxPriceReduction);
                Svc.Chat.Print(message);

                Callback.Fire((AtkUnitBase*)addon, true, 1);
                ui->Close(true);

                return true;
            }

            priceComponent->SetValue(CurrentMarketLowestPrice - Config.PriceReduction);
            Callback.Fire((AtkUnitBase*)addon, true, 0);
            ui->Close(true);

            return true;
        }

        return false;
    }

    private readonly Dictionary<string, string>? resourceData;
    private readonly Dictionary<string, string>? fbResourceData;

    public SeString GetSeString(string key, params object[] args)
    {
        var format = resourceData.TryGetValue(key, out var resValue) ? resValue : fbResourceData.GetValueOrDefault(key);
        var ssb = new SeStringBuilder();
        var lastIndex = 0;

        ssb.AddUiForeground("[Daily Routines]", 34);
        foreach (Match match in SeStringRegex().Matches(format))
        {
            ssb.AddUiForeground(format[lastIndex..match.Index], 2);
            lastIndex = match.Index + match.Length;

            if (int.TryParse(match.Groups[1].Value, out var argIndex) && argIndex >= 0 && argIndex < args.Length)
            {
                if (args[argIndex] is SeString @seString)
                {
                    ssb.Append(@seString);
                }
                else
                {
                    ssb.AddUiForeground(args[argIndex].ToString(), 2);
                }
            }
        }

        ssb.AddUiForeground(format[lastIndex..], 2);
        return ssb.Build();
    }

    [GeneratedRegex("\\{(\\d+)\\}")]
    private static partial Regex SeStringRegex();

    [GeneratedRegex("[^0-9]")]
    private static partial Regex AutoRetainerPriceAdjustRegex();
}
