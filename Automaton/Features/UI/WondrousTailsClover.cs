using Automaton.FeaturesSetup;
using Automaton.Helpers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Automaton.Helpers.WondrousTailsHelpers;

namespace Automaton.Features.UI
{
    public unsafe class WondrousTailsClover : Feature
    {
        public override string Name => "Wondrous Tails Clovers";

        public override string Description => "Adds a clover next to duties in the Duty Finder that are part of your Wondrous Tails.";

        public override FeatureType FeatureType => FeatureType.Disabled;

        public override void Enable()
        {
            var contentFinderData = Svc.Data.GetExcelSheet<ContentFinderCondition>().Where(cfc => cfc.Name != string.Empty);
            foreach (var cfc in contentFinderData)
            {
                var simplifiedString = TextHelper.FilterNonAlphanumeric(cfc.Name.ToString().ToLower());
                Duties.Add(new DutyFinderSearchResult(simplifiedString, cfc.TerritoryType.Row));
            }

            AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "ContentsFinder", OnUpdate);
            AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "ContentsFinder", OnRefresh);
            AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "ContentsFinder", OnDraw);
            AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "ContentsFinder", OnFinalize);
            wondrousTailsStatus = WondrousTailsBook.GetAllTaskData();
            base.Enable();
        }

        public override void Disable()
        {
            AddonLifecycle.UnregisterListener(OnUpdate);
            AddonLifecycle.UnregisterListener(OnRefresh);
            AddonLifecycle.UnregisterListener(OnDraw);
            AddonLifecycle.UnregisterListener(OnFinalize);
            base.Disable();
        }

        private IEnumerable<WondrousTailsTask> wondrousTailsStatus;

        private const uint GoldenCloverNodeId = 29;
        private const uint EmptyCloverNodeId = 30;

        public record DutyFinderSearchResult(string SearchKey, uint TerritoryType);
        public readonly List<DutyFinderSearchResult> Duties = new();

        private void OnRefresh(AddonEvent type, AddonArgs args)
        {
            if (!Enabled) return;

            wondrousTailsStatus = WondrousTailsBook.GetAllTaskData();
        }

        private void OnUpdate(AddonEvent type, AddonArgs args)
        {
            foreach (var listItem in GetDutyListItems(args.Addon))
            {
                var taskState = IsWondrousTailsDuty(listItem);

                if (taskState == null || !WondrousTailsBook.PlayerHasBook || !Enabled)
                {
                    SetCloverNodesVisibility(listItem, CloverState.Hidden);
                }
                else if (taskState == PlayerState.WeeklyBingoTaskStatus.Claimed)
                {
                    SetCloverNodesVisibility(listItem, CloverState.Dark);
                }
                else if (taskState is PlayerState.WeeklyBingoTaskStatus.Open or PlayerState.WeeklyBingoTaskStatus.Claimable)
                {
                    SetCloverNodesVisibility(listItem, CloverState.Golden);
                }
            }
        }

        private void OnDraw(AddonEvent type, AddonArgs args)
        {
            if (!Enabled) return;

            foreach (var listItem in GetDutyListItems(args.Addon))
            {
                var goldenNode = GetListItemNode<AtkImageNode>(listItem, GoldenCloverNodeId);
                if (goldenNode is null)
                {
                    MakeCloverNode(listItem, GoldenCloverNodeId);
                }

                var emptyNode = GetListItemNode<AtkImageNode>(listItem, EmptyCloverNodeId);
                if (emptyNode is null)
                {
                    MakeCloverNode(listItem, EmptyCloverNodeId);
                }

                var moogleNode = GetListItemNode<AtkResNode>(listItem, 6);
                if (moogleNode is not null && moogleNode->X is not 285)
                {
                    moogleNode->X = 285;
                }

                var levelSyncNode = GetListItemNode<AtkResNode>(listItem, 10);
                if (levelSyncNode is not null && levelSyncNode->X is not 305)
                {
                    levelSyncNode->X = 305;
                }
            }
        }

        private void OnFinalize(AddonEvent type, AddonArgs args)
        {
            foreach (var listItem in GetDutyListItems(args.Addon))
            {
                var goldenNode = GetListItemNode<AtkImageNode>(listItem, GoldenCloverNodeId);
                if (goldenNode is not null)
                {
                    ImageNode.FreeImageNode(goldenNode);
                }

                var emptyNode = GetListItemNode<AtkImageNode>(listItem, EmptyCloverNodeId);
                if (emptyNode is not null)
                {
                    ImageNode.FreeImageNode(emptyNode);
                }
            }
        }

        private PlayerState.WeeklyBingoTaskStatus? IsWondrousTailsDuty(nint item)
        {
            var nodeString = GetListItemString(item);
            var nodeRegexString = GetListItemFilteredString(item);

            var containsEllipsis = nodeString.Contains("...");

            foreach (var result in Duties)
            {
                if (containsEllipsis)
                {
                    var nodeStringLength = nodeRegexString.Length;

                    if (result.SearchKey.Length <= nodeStringLength) continue;

                    if (result.SearchKey[..nodeStringLength] == nodeRegexString)
                    {
                        return GetWondrousTailsTaskState(result.TerritoryType);
                    }
                }
                else if (result.SearchKey == nodeRegexString)
                {
                    return GetWondrousTailsTaskState(result.TerritoryType);
                }
            }

            return null;
        }

        private PlayerState.WeeklyBingoTaskStatus? GetWondrousTailsTaskState(uint duty) => wondrousTailsStatus.FirstOrDefault(task => task.DutyList.Contains(duty))?.TaskState;

        private void SetCloverNodesVisibility(nint listItem, CloverState state)
        {
            var goldenClover = GetListItemNode<AtkImageNode>(listItem, GoldenCloverNodeId);
            var emptyClover = GetListItemNode<AtkImageNode>(listItem, EmptyCloverNodeId);

            switch (state)
            {
                case CloverState.Hidden:
                    goldenClover->AtkResNode.ToggleVisibility(false);
                    emptyClover->AtkResNode.ToggleVisibility(false);
                    break;

                case CloverState.Golden:
                    goldenClover->AtkResNode.ToggleVisibility(true);
                    emptyClover->AtkResNode.ToggleVisibility(false);
                    break;

                case CloverState.Dark:
                    goldenClover->AtkResNode.ToggleVisibility(false);
                    emptyClover->AtkResNode.ToggleVisibility(true);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void MakeCloverNode(nint listItem, uint id)
        {
            if (listItem == nint.Zero) return;
            var listItemNode = (AtkComponentNode*)listItem;

            var textNode = (AtkResNode*)GetListItemTextNode(listItem);
            if (textNode is null) return;

            var textureCoordinates = id == GoldenCloverNodeId ? new Vector2(97, 65) : new Vector2(75, 63);

            var imageNode = ImageNode.MakeNode(id, textureCoordinates, new Vector2(20.0f, 20.0f));

            imageNode->LoadTexture("ui/uld/WeeklyBingo.tex");

            imageNode->AtkResNode.ToggleVisibility(true);

            imageNode->AtkResNode.SetWidth(20);
            imageNode->AtkResNode.SetHeight(20);

            var positionOffset = Vector2.Zero;

            var xPosition = (short)(325 + positionOffset.X);
            var yPosition = (short)(2 + positionOffset.Y);

            imageNode->AtkResNode.SetPositionShort(xPosition, yPosition);

            ImageNode.LinkNode(listItemNode, textNode, imageNode);
        }
    }
}
