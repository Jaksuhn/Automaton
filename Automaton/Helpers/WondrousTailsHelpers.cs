using Automaton.Helpers;
using Dalamud;
using Dalamud.Utility.Numerics;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
namespace Automaton.Helpers
{
    internal static class WondrousTailsHelpers
    {
        public record WondrousTailsTask(PlayerState.WeeklyBingoTaskStatus TaskState, List<uint> DutyList);

        public unsafe class WondrousTailsBook
        {
            private static WondrousTailsBook? _instance;
            public static WondrousTailsBook Instance => _instance ??= new WondrousTailsBook();

            public int Stickers => PlayerState.Instance()->WeeklyBingoNumPlacedStickers;
            public uint SecondChance => PlayerState.Instance()->WeeklyBingoNumSecondChancePoints;
            public static bool PlayerHasBook => PlayerState.Instance()->HasWeeklyBingoJournal;
            public bool NewBookAvailable => DateTime.Now > Deadline - TimeSpan.FromDays(7);
            public bool IsComplete => Stickers == 9;
            public bool NeedsNewBook => NewBookAvailable && IsComplete;
            private DateTime Deadline => DateTimeOffset.FromUnixTimeSeconds(PlayerState.Instance()->GetWeeklyBingoExpireUnixTimestamp()).ToLocalTime().DateTime;

            public static WondrousTailsTask? GetTaskForDuty(uint instanceID) => GetAllTaskData().FirstOrDefault(task => task.DutyList.Contains(instanceID));

            public static IEnumerable<WondrousTailsTask> GetAllTaskData() =>
                (from index in Enumerable.Range(0, 16)
                 let taskButtonState = PlayerState.Instance()->GetWeeklyBingoTaskStatus(index)
                 let instances = TaskLookup.GetInstanceListFromID(PlayerState.Instance()->WeeklyBingoOrderData[index])
                 select new WondrousTailsTask(taskButtonState, instances))
                .ToList();
        }

        public enum CloverState
        {
            Hidden,
            Golden,
            Dark
        }

        public unsafe struct CloverNode
        {
            public AtkImageNode* GoldenCloverNode;
            public AtkImageNode* EmptyCloverNode;

            public CloverNode(AtkImageNode* golden, AtkImageNode* dark)
            {
                GoldenCloverNode = golden;
                EmptyCloverNode = dark;
            }

            public readonly void SetVisibility(CloverState state)
            {
                if (GoldenCloverNode == null || EmptyCloverNode == null) return;

                switch (state)
                {
                    case CloverState.Hidden:
                        GoldenCloverNode->AtkResNode.ToggleVisibility(false);
                        EmptyCloverNode->AtkResNode.ToggleVisibility(false);
                        break;

                    case CloverState.Golden:
                        GoldenCloverNode->AtkResNode.ToggleVisibility(true);
                        EmptyCloverNode->AtkResNode.ToggleVisibility(false);
                        break;

                    case CloverState.Dark:
                        GoldenCloverNode->AtkResNode.ToggleVisibility(false);
                        EmptyCloverNode->AtkResNode.ToggleVisibility(true);
                        break;
                }
            }
        }

        public static unsafe IEnumerable<nint> GetDutyListItems(nint addonBase) => GetDutyListItems((AtkUnitBase*)addonBase);
        public static unsafe IEnumerable<nint> GetDutyListItems(AtkUnitBase* addonBase)
        {
            var treeListNode = (AtkComponentNode*)addonBase->GetNodeById(52);
            if (treeListNode is null) return new List<nint>();

            var treeListNodeComponent = treeListNode->Component;
            if (treeListNodeComponent is null) return new List<nint>();

            return Enumerable.Range(61001, 15).Append(6).Select(index => (nint)Node.GetNodeByID<AtkComponentNode>(treeListNodeComponent->UldManager, (uint)index));
        }

        public static unsafe T* GetListItemNode<T>(nint listItem, uint nodeId) where T : unmanaged
        {
            if (listItem == nint.Zero) return null;

            var listItemNode = (AtkComponentNode*)listItem;
            var listItemComponent = listItemNode->Component;
            if (listItemComponent is null) return null;

            return Node.GetNodeByID<T>(listItemComponent->UldManager, nodeId);
        }

        public static unsafe AtkTextNode* GetListItemTextNode(nint listItem) => GetListItemNode<AtkTextNode>(listItem, 5);

        public static string GetListItemFilteredString(nint listItem)
        {
            var nodeText = GetListItemString(listItem);

            return TextHelper.FilterNonAlphanumeric(nodeText).ToLower();
        }

        public static unsafe string GetListItemString(nint listItem)
        {
            if (listItem == nint.Zero) return string.Empty;

            var textNode = GetListItemTextNode(listItem);
            if (textNode is null) return string.Empty;

            return textNode->NodeText.ToString();
        }
    }

    public static unsafe class Node
    {
        public static T* GetNodeByID<T>(AtkUldManager uldManager, uint nodeId) where T : unmanaged
        {
            foreach (var index in Enumerable.Range(0, uldManager.NodeListCount))
            {
                var currentNode = uldManager.NodeList[index];
                if (currentNode->NodeID != nodeId) continue;

                return (T*)currentNode;
            }

            return null;
        }

        public static void LinkNodeAtEnd(AtkResNode* resNode, AtkUnitBase* parent)
        {
            var node = parent->RootNode->ChildNode;
            while (node->PrevSiblingNode != null) node = node->PrevSiblingNode;

            node->PrevSiblingNode = resNode;
            resNode->NextSiblingNode = node;
            resNode->ParentNode = node->ParentNode;

            node->ChildCount++;

            parent->UldManager.UpdateDrawNodeList();
        }

        public static void UnlinkNodeAtEnd(AtkResNode* resNode, AtkUnitBase* parent)
        {
            if (resNode->PrevSiblingNode is not null)
            {
                resNode->PrevSiblingNode->NextSiblingNode = resNode->NextSiblingNode;
            }

            if (resNode->NextSiblingNode is not null)
            {
                resNode->NextSiblingNode->PrevSiblingNode = resNode->PrevSiblingNode;
            }

            parent->UldManager.UpdateDrawNodeList();
        }

        public static void LinkNodeAtStart(AtkResNode* resNode, AtkUnitBase* parent)
        {
            var rootNode = parent->RootNode;

            resNode->ParentNode = rootNode;
            resNode->PrevSiblingNode = rootNode->ChildNode;
            resNode->NextSiblingNode = null;

            if (rootNode->ChildNode->NextSiblingNode is not null)
            {
                rootNode->ChildNode->NextSiblingNode = resNode;
            }

            rootNode->ChildNode = resNode;

            parent->UldManager.UpdateDrawNodeList();
        }

        public static void UnlinkNodeAtStart(AtkResNode* resNode, AtkUnitBase* parent)
        {
            if (!IsAddonReady(parent)) return;
            if (parent->RootNode->ChildNode->NodeID != resNode->NodeID) return;

            var rootNode = parent->RootNode;

            if (resNode->PrevSiblingNode is not null)
            {
                resNode->PrevSiblingNode->NextSiblingNode = null;
            }

            rootNode->ChildNode = resNode->PrevSiblingNode;

            parent->UldManager.UpdateDrawNodeList();
        }

        public static bool IsAddonReady(AtkUnitBase* addon)
        {
            if (addon is null) return false;
            if (addon->RootNode is null) return false;
            if (addon->RootNode->ChildNode is null) return false;

            return true;
        }
    }

    internal static class TaskLookup
    {
        public static List<uint> GetInstanceListFromID(uint id)
        {
            var bingoOrderData = Svc.Data.GetExcelSheet<WeeklyBingoOrderData>().GetRow(id);
            if (bingoOrderData is null) return new List<uint>();

            switch (bingoOrderData.Type)
            {
                // Specific Duty
                case 0:
                    return Svc.Data.GetExcelSheet<ContentFinderCondition>()
                        .Where(c => c.Content == bingoOrderData.Data)
                        .OrderBy(row => row.SortKey)
                        .Select(c => c.TerritoryType.Row)
                        .ToList();

                // Specific Level Dungeon
                case 1:
                    return Svc.Data.GetExcelSheet<ContentFinderCondition>()
                        .Where(m => m.ContentType.Row is 2)
                        .Where(m => m.ClassJobLevelRequired == bingoOrderData.Data)
                        .OrderBy(row => row.SortKey)
                        .Select(m => m.TerritoryType.Row)
                        .ToList();

                // Level Range Dungeon
                case 2:
                    return Svc.Data.GetExcelSheet<ContentFinderCondition>()
                        .Where(m => m.ContentType.Row is 2)
                        .Where(m => m.ClassJobLevelRequired >= bingoOrderData.Data - (bingoOrderData.Data > 50 ? 9 : 49) && m.ClassJobLevelRequired <= bingoOrderData.Data - 1)
                        .OrderBy(row => row.SortKey)
                        .Select(m => m.TerritoryType.Row)
                        .ToList();

                // Special categories
                case 3:
                    return bingoOrderData.Unknown5 switch
                    {
                        // Treasure Map Instances are Not Supported
                        1 => new List<uint>(),

                        // PvP Categories are Not Supported
                        2 => new List<uint>(),

                        // Deep Dungeons
                        3 => Svc.Data.GetExcelSheet<ContentFinderCondition>()
                            .Where(m => m.ContentType.Row is 21)
                            .OrderBy(row => row.SortKey)
                            .Select(m => m.TerritoryType.Row)
                            .ToList(),

                        _ => new List<uint>()
                    };

                // Multi-instance raids
                case 4:
                    var raidIndex = (int)(bingoOrderData.Data - 11) * 2;

                    return bingoOrderData.Data switch
                    {
                        // Binding Coil, Second Coil, Final Coil
                        2 => new List<uint> { 241, 242, 243, 244, 245 },
                        3 => new List<uint> { 355, 356, 357, 358 },
                        4 => new List<uint> { 193, 194, 195, 196 },

                        // Gordias, Midas, The Creator
                        5 => new List<uint> { 442, 443, 444, 445 },
                        6 => new List<uint> { 520, 521, 522, 523 },
                        7 => new List<uint> { 580, 581, 582, 583 },

                        // Deltascape, Sigmascape, Alphascape
                        8 => new List<uint> { 691, 692, 693, 694 },
                        9 => new List<uint> { 748, 749, 750, 751 },
                        10 => new List<uint> { 798, 799, 800, 801 },

                        > 10 => Svc.Data.GetExcelSheet<ContentFinderCondition>(ClientLanguage.English)
                            .Where(row => row.ContentType.Row is 5)
                            .Where(row => row.ContentMemberType.Row is 3)
                            .Where(row => !row.Name.RawString.Contains("Savage"))
                            .Where(row => row.ItemLevelRequired >= 425)
                            .OrderBy(row => row.SortKey)
                            .Select(row => row.TerritoryType.Row)
                            .ToArray()[raidIndex..(raidIndex + 2)]
                            .ToList(),

                        _ => new List<uint>()
                    };
            }

            Svc.Log.Information($"[WondrousTails] Unrecognized ID: {id}");
            return new List<uint>();
        }
    }

    public static unsafe class ImageNode
    {
        private static IMemorySpace* UISpace => IMemorySpace.GetUISpace();

        public static AtkImageNode* MakeNode(uint nodeId, Vector2 textureCoordinates, Vector2 textureSize)
        {
            var customNode = UISpace->Create<AtkImageNode>();
            customNode->AtkResNode.Type = NodeType.Image;
            customNode->AtkResNode.NodeID = nodeId;
            customNode->AtkResNode.NodeFlags = (NodeFlags)8243; // test if this works
            customNode->AtkResNode.DrawFlags = 0;
            customNode->WrapMode = 1;
            customNode->Flags = 0;

            var partsList = MakePartsList(0, 1);
            if (partsList == null)
            {
                FreeImageNode(customNode);
                return null;
            }

            var part = MakePart(textureCoordinates, textureSize);
            if (part == null)
            {
                FreePartsList(partsList);
                FreeImageNode(customNode);
                return null;
            }

            partsList->Parts = part;

            var asset = MakeAsset(0);
            if (asset == null)
            {
                FreePart(part);
                FreePartsList(partsList);
                FreeImageNode(customNode);
                return null;
            }

            part->UldAsset = asset;
            customNode->PartsList = partsList;

            return customNode;
        }

        private static AtkUldPartsList* MakePartsList(uint id, uint partCount)
        {
            var partsList = (AtkUldPartsList*)UISpace->Malloc((ulong)sizeof(AtkUldPartsList), 8);

            if (partsList is not null)
            {
                partsList->Id = id;
                partsList->PartCount = partCount;
                return partsList;
            }

            return null;
        }

        private static AtkUldPart* MakePart(Vector2 textureCoordinates, Vector2 size)
        {
            var part = (AtkUldPart*)UISpace->Malloc((ulong)sizeof(AtkUldPart), 8);

            if (part is not null)
            {
                part->U = (ushort)textureCoordinates.X;
                part->V = (ushort)textureCoordinates.Y;

                part->Width = (ushort)size.X;
                part->Height = (ushort)size.Y;
                return part;
            }

            return null;
        }

        private static AtkUldAsset* MakeAsset(uint id)
        {
            var asset = (AtkUldAsset*)UISpace->Malloc((ulong)sizeof(AtkUldAsset), 8);

            if (asset is not null)
            {
                asset->Id = id;
                asset->AtkTexture.Ctor();
                return asset;
            }

            return null;
        }

        private static void FreePartsList(AtkUldPartsList* partsList)
        {
            if (partsList is not null)
            {
                IMemorySpace.Free(partsList, (ulong)sizeof(AtkUldPartsList));
            }
        }

        private static void FreePart(AtkUldPart* part)
        {
            if (part is not null)
            {
                IMemorySpace.Free(part, (ulong)sizeof(AtkUldPart));
            }
        }

        private static void FreeAsset(AtkUldAsset* asset)
        {
            if (asset is not null)
            {
                asset->AtkTexture.Destroy(true);
                IMemorySpace.Free(asset, (ulong)sizeof(AtkUldAsset));
            }
        }

        public static void FreeImageNode(AtkImageNode* imageNode)
        {
            if (imageNode is not null)
            {
                var partsList = imageNode->PartsList;
                if (partsList is not null)
                {
                    var part = imageNode->PartsList->Parts;
                    if (part is not null)
                    {
                        var asset = imageNode->PartsList->Parts->UldAsset;
                        if (asset is not null)
                        {
                            FreeAsset(asset);
                        }

                        FreePart(part);
                    }

                    FreePartsList(partsList);
                }

                imageNode->AtkResNode.Destroy(true);
                IMemorySpace.Free(imageNode, (ulong)sizeof(AtkImageNode));
            }
        }

        public static void LinkNode(AtkComponentNode* rootNode, AtkResNode* beforeNode, AtkImageNode* newNode)
        {
            var prev = beforeNode->PrevSiblingNode;
            newNode->AtkResNode.ParentNode = beforeNode->ParentNode;

            beforeNode->PrevSiblingNode = (AtkResNode*)newNode;
            prev->NextSiblingNode = (AtkResNode*)newNode;

            newNode->AtkResNode.PrevSiblingNode = prev;
            newNode->AtkResNode.NextSiblingNode = beforeNode;

            rootNode->Component->UldManager.UpdateDrawNodeList();
        }
    }
}
