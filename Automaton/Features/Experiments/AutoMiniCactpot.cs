using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Addon.Lifecycle;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using Automaton.FeaturesSetup;
using ECommons.DalamudServices;
using ECommons;
using ECommons.Automation;
using Automaton.Helpers;

namespace Automaton.Features.Experiments
{
    internal class AutoMiniCactpot : Feature
    {
        public override string Name => "Auto Mini Cactpot";
        public override string Description => "Auto play the Mini Cactpot minigame in the Gold Saucer. Needs ezMiniCactpot to play well.";
        public override FeatureType FeatureType => FeatureType.Disabled;

        public bool Initialized { get; set; }

        private static readonly Dictionary<uint, uint> BlockNodeIds = new()
        {
            { 30, 0 },
            { 31, 1 },
            { 32, 2 },
            { 33, 3 },
            { 34, 4 },
            { 35, 5 },
            { 36, 6 },
            { 37, 7 },
            { 38, 8 }
        };

        private static readonly uint[] LineNodeIds = [28, 27, 26, 21, 22, 23, 24, 25];

        private static readonly Dictionary<uint, List<uint>> LineToBlocks = new()
        {
            { 28, [36, 37, 38] }, // lower horizontal
            { 27, [33, 34, 35] }, // middle horizontal
            { 26, [30, 31, 32] }, // upper horizontal
            { 21, [30, 34, 38] }, // left diagonal
            { 22, [30, 33, 36] }, // left vertical
            { 23, [31, 34, 37] }, // middle vertical
            { 24, [32, 35, 38] }, // right vertical
            { 25, [32, 34, 36] }  // right diagonal
        };


        public override void Enable()
        {
            base.Enable();
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "LotteryDaily", OnAddonSetup);
            Initialized = true;
        }

        public override void Disable()
        {
            base.Disable();
            Svc.AddonLifecycle.UnregisterListener(OnAddonSetup);
            TaskManager?.Abort();
            Initialized = false;
        }

        private void OnAddonSetup(AddonEvent type, AddonArgs args)
        {
            if (IsEzMiniCactpotInstalled())
            {
                TaskManager.Enqueue(WaitLotteryDailyAddon);
                TaskManager.Enqueue(ClickRecommendBlock);
                TaskManager.Enqueue(ClickRecommendBlock);
                TaskManager.Enqueue(ClickRecommendBlock);
                TaskManager.Enqueue(WaitLotteryDailyAddon);
                TaskManager.Enqueue(ClickRecommendLine);
                TaskManager.Enqueue(WaitLotteryDailyAddon);
                TaskManager.Enqueue(ClickExit);
            }
            else
            {
                TaskManager.Enqueue(WaitLotteryDailyAddon);
                TaskManager.Enqueue(ClickRandomBlocks);
                TaskManager.Enqueue(WaitLotteryDailyAddon);
                TaskManager.Enqueue(ClickRandomLine);
                TaskManager.Enqueue(WaitLotteryDailyAddon);
                TaskManager.Enqueue(ClickExit);
            }
        }

        private static unsafe bool? ClickRandomBlocks()
        {
            if (GenericHelpers.TryGetAddonByName<AddonLotteryDaily>("LotteryDaily", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
            {
                var ui = &addon->AtkUnitBase;
                var rnd = new Random();
                var selectedBlocks = BlockNodeIds.Keys.OrderBy(x => rnd.Next()).Take(4).ToArray();
                foreach (var id in selectedBlocks)
                {
                    var blockButton = ui->GetComponentNodeById(id);
                    if (blockButton == null) continue;

                    Callback.Fire(&addon->AtkUnitBase, true, 1, BlockNodeIds[id]);
                }

                return true;
            }

            return false;
        }

        private static unsafe bool? ClickRandomLine()
        {
            if (GenericHelpers.TryGetAddonByName<AddonLotteryDaily>("LotteryDaily", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
            {
                var ui = &addon->AtkUnitBase;
                var rnd = new Random();
                var selectedLine = LineNodeIds.OrderBy(x => rnd.Next()).LastOrDefault();

                var blocks = LineToBlocks[selectedLine];

                AtkResNodeHelper.ClickAddonCheckBox(ui, (AtkComponentCheckBox*)ui->GetComponentNodeById(blocks[0]), 5);
                AtkResNodeHelper.ClickAddonCheckBox(ui, (AtkComponentCheckBox*)ui->GetComponentNodeById(blocks[1]), 5);
                AtkResNodeHelper.ClickAddonCheckBox(ui, (AtkComponentCheckBox*)ui->GetComponentNodeById(blocks[2]), 5);

                Callback.Fire(&addon->AtkUnitBase, true, 2, 0);
                return true;
            }

            return false;
        }

        private static unsafe bool? ClickExit()
        {
            if (GenericHelpers.TryGetAddonByName<AddonLotteryDaily>("LotteryDaily", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
            {
                Callback.Fire(&addon->AtkUnitBase, true, -1);
                return true;
            }

            return false;
        }

        private static unsafe bool? ClickRecommendBlock()
        {
            if (GenericHelpers.TryGetAddonByName<AddonLotteryDaily>("LotteryDaily", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
            {
                var ui = &addon->AtkUnitBase;
                foreach (var block in BlockNodeIds)
                {
                    var node = ui->GetComponentNodeById(block.Key)->AtkResNode;
                    if (node is { MultiplyBlue: 0, MultiplyRed: 0, MultiplyGreen: 100 })
                    {
                        Callback.Fire(&addon->AtkUnitBase, true, 1, block.Value);
                        break;
                    }
                }

                return true;
            }

            return false;
        }

        private static unsafe bool? ClickRecommendLine()
        {
            if (GenericHelpers.TryGetAddonByName<AddonLotteryDaily>("LotteryDaily", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
            {
                var ui = &addon->AtkUnitBase;
                foreach (var block in LineNodeIds)
                {
                    var node = ui->GetComponentNodeById(block)->AtkResNode;
                    var button = (AtkComponentRadioButton*)ui->GetComponentNodeById(block);
                    if (node is { MultiplyBlue: 0, MultiplyRed: 0, MultiplyGreen: 100 })
                    {
                        var blocks = LineToBlocks[node.NodeID];
                        AtkResNodeHelper.ClickAddonCheckBox(ui, (AtkComponentCheckBox*)ui->GetComponentNodeById(blocks[0]), 5);
                        AtkResNodeHelper.ClickAddonCheckBox(ui, (AtkComponentCheckBox*)ui->GetComponentNodeById(blocks[1]), 5);
                        AtkResNodeHelper.ClickAddonCheckBox(ui, (AtkComponentCheckBox*)ui->GetComponentNodeById(blocks[2]), 5);
                        break;
                    }
                }

                Callback.Fire(&addon->AtkUnitBase, true, 2, 0);
                return true;
            }

            return false;
        }

        internal static bool IsEzMiniCactpotInstalled() => Svc.PluginInterface.InstalledPlugins.Any(plugin => plugin is { Name: "ezMiniCactpot", IsLoaded: true });

        private static unsafe bool? WaitLotteryDailyAddon()
        {
            if (GenericHelpers.TryGetAddonByName<AddonLotteryDaily>("LotteryDaily", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
            {
                var ui = &addon->AtkUnitBase;
                return !ui->GetImageNodeById(4)->AtkResNode.IsVisible && !ui->GetTextNodeById(3)->AtkResNode.IsVisible &&
                       !ui->GetTextNodeById(2)->AtkResNode.IsVisible;
            }

            return false;
        }
    }
}
