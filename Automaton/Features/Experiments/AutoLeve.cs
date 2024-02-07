using Automaton.FeaturesSetup;
using Automaton.Helpers.NPCLocations;
using Automaton.UI;
using ClickLib;
using ClickLib.Clicks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Utility.Raii;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Data.Files;
using Lumina.Data.Parsing.Layer;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Automaton.Features.Experiments;

public class AutoLeve : Feature
{
    public override string Name => "Auto Leve";
    public override string Description => "Hand in leves on repeat";
    public override FeatureType FeatureType => FeatureType.Disabled;

    private Overlays overlay;

    private static Dictionary<uint, (string, uint)> LeveQuests = [];
    private static readonly HashSet<uint> QualifiedLeveCategories = [9, 10, 11, 12, 13, 14, 15, 16];

    private static (uint, string, uint)? SelectedLeve; // Leve ID - Leve Name - Leve Job Category

    private static uint LeveMeteDataId;
    private static uint LeveReceiverDataId;
    private static int Allowances;
    private static string SearchString = string.Empty;

    private readonly Dictionary<uint, NpcLocation> npcLocations = [];
    private readonly List<uint> leveNPCs = [];
    private ExcelSheet<ENpcResident> eNpcResidents;
    private ExcelSheet<Map> maps;
    private ExcelSheet<TerritoryType> territoryType;

    private static bool IsOnProcessing;

    public override void Enable()
    {
        base.Enable();
        overlay = new Overlays(this);

        eNpcResidents = Svc.Data.GetExcelSheet<ENpcResident>();
        maps = Svc.Data.GetExcelSheet<Map>();
        territoryType = Svc.Data.GetExcelSheet<TerritoryType>();
        BuildLeveNPCs();
        BuildNpcLocation();
        FilterLocations();
        Svc.ClientState.TerritoryChanged += OnZoneChanged;
    }

    protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
    {
        if (ImGui.Button("debug"))
            try
            {
                foreach (var npc in npcLocations)
                    Svc.Log.Info($"{npc.Key}: {eNpcResidents.GetRow(npc.Key).Singular} - {npc.Value.TerritoryType}");
            }
            catch (Exception e)
            {
                Svc.Log.Error(e.ToString());
            }
    };

    public override void Disable()
    {
        base.Disable();
        P.Ws.RemoveWindow(overlay);
        Svc.ClientState.TerritoryChanged -= OnZoneChanged;
        EndProcessHandler();
    }

    private void OnZoneChanged(ushort obj) => LeveQuests.Clear();

    private static float GetDistanceToNpc(int npcId, out GameObject? o)
    {
        foreach (var obj in Svc.Objects)
        {
            if (obj.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventNpc && obj is Character c)
            {
                if (Marshal.ReadInt32(obj.Address + 128) == npcId)
                {
                    o = obj;
                    return Vector3.Distance(Svc.ClientState.LocalPlayer?.Position ?? Vector3.Zero, c.Position);
                }
            }
        }

        o = null;
        return float.MaxValue;
    }

    public override unsafe void Draw()
    {
        if (GameMain.Instance()->CurrentContentFinderConditionId != 0) return;
        if (npcLocations.Values.ToList().All(x => x.TerritoryType != Svc.ClientState.TerritoryType)) return;
        foreach (var npc in npcLocations.Where(x => x.Value.TerritoryType == Svc.ClientState.TerritoryType).ToDictionary(x => x.Key, x => x.Value))
            if (GetDistanceToNpc((int)npc.Key, out var o) > 5f) return;

        try
        {
            if (ImGui.Begin("AutoLeve"))
            {
                using (ImRaii.Disabled(IsOnProcessing))
                {
                    ImGui.Text($"SelectedLeve");

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(400f);
                    using (ImRaii.Combo("##SelectedLeve", SelectedLeve == null ? "" : $"{SelectedLeve.Value.Item1} | {SelectedLeve.Value.Item2}"))
                    {
                        if (ImGui.Button("GetAreaLeveData")) GetRecentLeveQuests();

                        ImGui.SetNextItemWidth(-1f);
                        ImGui.SameLine();
                        ImGui.InputText("##AutoLeveQuests-SearchLeveQuest", ref SearchString, 100);

                        ImGui.Separator();
                        if (LeveQuests.Any())
                        {
                            foreach (var leveToSelect in LeveQuests)
                            {
                                if (!string.IsNullOrEmpty(SearchString) &&
                                    !leveToSelect.Value.Item1.Contains(SearchString, StringComparison.OrdinalIgnoreCase) &&
                                    !leveToSelect.Key.ToString().Contains(SearchString, StringComparison.OrdinalIgnoreCase))
                                    continue;
                                if (ImGui.Selectable($"{leveToSelect.Key} | {leveToSelect.Value.Item1}"))
                                    SelectedLeve = (leveToSelect.Key, leveToSelect.Value.Item1, leveToSelect.Value.Item2);
                                if (SelectedLeve != null && ImGui.IsWindowAppearing() &&
                                    SelectedLeve.Value.Item1 == leveToSelect.Key)
                                    ImGui.SetScrollHereY();
                            }
                        }
                    }

                    ImGui.SameLine();
                    using (ImRaii.Disabled(SelectedLeve == null || LeveMeteDataId == LeveReceiverDataId || LeveMeteDataId == 0 || LeveReceiverDataId == 0))
                    {
                        if (ImGui.Button("Start"))
                        {
                            IsOnProcessing = true;
                            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "SelectYesno", AlwaysYes);

                            TaskManager.Enqueue(InteractWithMete);
                        }
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button("Stop")) EndProcessHandler();

                using (ImRaii.Disabled(IsOnProcessing))
                {
                    if (ImGui.Button("ObtainLevemeteID"))
                        GetCurrentTargetDataID(out LeveMeteDataId);

                    ImGui.SameLine();
                    ImGui.Text(LeveMeteDataId.ToString());

                    ImGui.SameLine();
                    ImGui.Spacing();

                    ImGui.SameLine();
                    if (ImGui.Button("ObtainLeveClientID"))
                        GetCurrentTargetDataID(out LeveReceiverDataId);

                    ImGui.SameLine();
                    ImGui.Text(LeveReceiverDataId.ToString());
                }
            }
            ImGui.End();
        }
        catch (Exception e)
        {
            Svc.Log.Error(e.ToString());
        }
    }

    private void EndProcessHandler()
    {
        TaskManager?.Abort();
        Svc.AddonLifecycle.UnregisterListener(AlwaysYes);
        IsOnProcessing = false;
    }

    private static void AlwaysYes(AddonEvent type, AddonArgs args) => Click.SendClick("select_yes");

    private static void GetRecentLeveQuests()
    {
        var currentTerritoryPlaceNameId = Svc.Data.GetExcelSheet<TerritoryType>()
                                                 .FirstOrDefault(y => y.RowId == Svc.ClientState.TerritoryType)?
                                                 .PlaceName.RawRow.RowId;

        if (currentTerritoryPlaceNameId.HasValue)
        {
            LeveQuests = Svc.Data.GetExcelSheet<Leve>()
                .Where(x => !string.IsNullOrEmpty(x.Name.RawString) && QualifiedLeveCategories
                    .Contains(x.ClassJobCategory.RawRow.RowId) && x.PlaceNameIssued.RawRow.RowId == currentTerritoryPlaceNameId.Value)
                .ToDictionary(x => x.RowId, x => (x.Name.RawString, x.ClassJobCategory.RawRow.RowId));

            Svc.Log.Debug($"Obtained {LeveQuests.Count} leve quests");
        }
    }

    private static void GetCurrentTargetDataID(out uint targetDataId)
    {
        var currentTarget = Svc.Targets.Target;
        targetDataId = currentTarget == null ? 0 : currentTarget.DataId;
    }

    private unsafe bool? InteractWithMete()
    {
        if (GenericHelpers.TryGetAddonByName<AddonSelectString>("SelectString", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
        {
            var i = 1;
            for (; i < 8; i++)
            {
                var text = addon->PopupMenu.PopupMenu.List->AtkComponentBase.UldManager.NodeList[i]->GetAsAtkComponentNode()->
                        Component->UldManager.NodeList[3]->GetAsAtkTextNode()->NodeText.ExtractText();
                if (text.Contains("Finish")) break;
            }

            var handler = new ClickSelectString();
            handler.SelectItem((ushort)(i - 1));
        }

        if (GenericHelpers.IsOccupied()) return false;
        if (FindObjectToInteractWith(LeveMeteDataId, out var foundObject))
        {
            TargetSystem.Instance()->InteractWithObject(foundObject);

            TaskManager.Enqueue(ClickCraftingLeve);
            return true;
        }

        return false;
    }

    private static unsafe bool FindObjectToInteractWith(uint dataId, out FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* foundObject)
    {
        foreach (var obj in Svc.Objects.Where(o => o.DataId == dataId))
            if (obj.IsTargetable)
            {
                foundObject = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)obj.Address;
                return true;
            }

        foundObject = null;
        return false;
    }

    private unsafe bool? ClickCraftingLeve()
    {
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("SelectString", out var addon) && GenericHelpers.IsAddonReady(addon))
        {
            var handler = new ClickSelectString();
            handler.SelectItem2();

            TaskManager.Enqueue(ClickLeveQuest);

            return true;
        }

        return false;
    }

    private unsafe bool? ClickLeveQuest()
    {
        if (SelectedLeve == null) return false;
        if (GenericHelpers.TryGetAddonByName<AddonGuildLeve>("GuildLeve", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
        {
            Allowances = int.TryParse(addon->AtkComponentBase290->UldManager.NodeList[2]->GetAsAtkTextNode()->NodeText.ExtractText(), out var result) ? result : 0;
            if (Allowances <= 0) EndProcessHandler();

            if (GenericHelpers.TryGetAddonByName<AddonJournalDetail>("JournalDetail", out var addon1) && GenericHelpers.IsAddonReady(&addon1->AtkUnitBase))
            {
                Callback.Fire(&addon->AtkUnitBase, true, 3, (int)SelectedLeve.Value.Item1);
                TaskManager.Enqueue(ClickExit);
                return true;
            }
        }
        return false;
    }

    internal unsafe bool? ClickExit()
    {
        if (GenericHelpers.TryGetAddonByName<AddonGuildLeve>("GuildLeve", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
        {
            var ui = &addon->AtkUnitBase;
            Callback.Fire(ui, true, -2);
            Callback.Fire(ui, true, -1);

            TaskManager.Enqueue(ClickSelectStringExit);

            ui->Close(true);

            return true;
        }

        return false;
    }

    private unsafe bool? ClickSelectStringExit()
    {
        if (SelectedLeve == null) return false;
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("SelectString", out var addon) && GenericHelpers.IsAddonReady(addon))
        {
            var i = 1;
            for (; i < 8; i++)
            {
                var text =
                    ((AddonSelectString*)addon)->PopupMenu.PopupMenu.List->AtkComponentBase.UldManager.NodeList[i]->
                    GetAsAtkComponentNode()->
                    Component->UldManager.NodeList[3]->GetAsAtkTextNode()->NodeText.ExtractText();
                if (text.Contains("取消")) break;
            }

            var handler = new ClickSelectString();
            handler.SelectItem((ushort)(i - 1));

            TaskManager.Enqueue(InteractWithReceiver);

            addon->Close(true);

            return true;
        }

        return false;
    }

    private unsafe bool? InteractWithReceiver()
    {
        if (GenericHelpers.IsOccupied()) return false;
        if (FindObjectToInteractWith(LeveReceiverDataId, out var foundObject))
        {
            TargetSystem.Instance()->InteractWithObject(foundObject);

            var levesSpan = QuestManager.Instance()->LeveQuestsSpan;
            var qualifiedCount = 0;

            for (var i = 0; i < levesSpan.Length; i++)
                if (LeveQuests.ContainsKey(levesSpan[i].LeveId))
                    qualifiedCount++;

            TaskManager.Enqueue(qualifiedCount > 1 ? ClickSelectQuest : InteractWithMete);

            return true;
        }

        return false;
    }

    private unsafe bool? ClickSelectQuest()
    {
        if (SelectedLeve == null) return false;
        if (GenericHelpers.TryGetAddonByName<AddonSelectIconString>("SelectIconString", out var addon) &&
            GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
        {
            var i = 1;
            for (; i < 8; i++)
            {
                var text =
                    addon->PopupMenu.PopupMenu.List->AtkComponentBase.UldManager.NodeList[i]->GetAsAtkComponentNode()->
                        Component->UldManager.NodeList[4]->GetAsAtkTextNode()->NodeText.ExtractText();
                if (text == SelectedLeve.Value.Item2) break;
            }

            var handler = new ClickSelectIconString();
            handler.SelectItem((ushort)(i - 1));

            TaskManager.Enqueue(InteractWithMete);
            return true;
        }

        return false;
    }

    private void BuildLeveNPCs()
    {
        Parallel.ForEach(Svc.Data.GetExcelSheet<GuildleveAssignment>(), assigner =>
        {
            if (Svc.Data.Excel.GetSheet<ENpcBase>().TryGetFirst(x => x.ENpcData.Any(y => y == assigner.RowId), out var enpc))
            {
                lock (leveNPCs)
                {
                    leveNPCs.Add(enpc.RowId);
                }
            }
        });
    }

    private void FilterLocations() => npcLocations.Keys.ToList().ForEach(key => { if (!leveNPCs.Contains(key)) npcLocations.Remove(key); });

    // https://github.com/Nukoooo/ItemVendorLocation/blob/fc138fc71e98fef65e1a9e8bd530d56e1b536258/ItemVendorLocation/ItemLookup.cs
    private void BuildNpcLocation()
    {
        foreach (var sTerritoryType in territoryType)
        {
            var bg = sTerritoryType.Bg.ToString();
            if (string.IsNullOrEmpty(bg))
                continue;

            var lgbFileName = "bg/" + bg[..(bg.IndexOf("/level/", StringComparison.Ordinal) + 1)] + "level/planevent.lgb";
            var sLgbFile = Svc.Data.GetFile<LgbFile>(lgbFileName);
            if (sLgbFile == null)
                continue;

            ParseLgbFile(sLgbFile, sTerritoryType);
        }

        var levels = Svc.Data.GetExcelSheet<Level>();
        foreach (var level in levels)
        {
            // NPC
            if (level.Type != 8)
                continue;

            // NPC Id
            if (npcLocations.ContainsKey(level.Object))
                continue;

            if (level.Territory.Value == null)
                continue;

            npcLocations.Add(level.Object, new NpcLocation(level.X, level.Z, level.Territory.Value));
        }

        // https://github.com/ufx/GarlandTools/blob/7b38def8cf0ab553a2c3679aec86480c0e4e9481/Garland.Data/Modules/NPCs.cs#L59-L66
        var corrected = territoryType.GetRow(698);
        foreach (var key in new uint[] { 1004418, 1006747, 1002299, 1002281, 1001766, 1001945, 1001821 })
            if (npcLocations.ContainsKey(key))
                npcLocations[key].TerritoryExcel = corrected;

        ManualItemCorrections.ApplyCorrections(npcLocations);
    }

    public void ParseLgbFile(LgbFile lgbFile, TerritoryType sTerritoryType, uint? npcId = null)
    {
        foreach (var sLgbGroup in lgbFile.Layers)
        {
            foreach (var instanceObject in sLgbGroup.InstanceObjects)
            {
                if (instanceObject.AssetType != LayerEntryType.EventNPC)
                    continue;

                var eventNpc = (LayerCommon.ENPCInstanceObject)instanceObject.Object;
                var npcRowId = eventNpc.ParentData.ParentData.BaseId;
                if (npcRowId == 0)
                    continue;

                if (npcId != null && npcRowId != npcId)
                    continue;

                if (npcId == null && npcLocations.ContainsKey(npcRowId))
                    continue;

                var mapId = eNpcResidents.GetRow(npcRowId).Map;
                try
                {
                    var map = maps.First(i => i.TerritoryType.Value == sTerritoryType && i.MapIndex == mapId);
                    npcLocations.Add(npcRowId, new NpcLocation(instanceObject.Transform.Translation.X, instanceObject.Transform.Translation.Z, sTerritoryType, map.RowId));
                }
                catch (InvalidOperationException)
                {
                    npcLocations.Add(npcRowId, new NpcLocation(instanceObject.Transform.Translation.X, instanceObject.Transform.Translation.Z, sTerritoryType));
                }
            }
        }
    }
}
