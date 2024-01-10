using Automaton.Debugging;
using ImGuiNET;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.STD;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;
using FFXIVClientStructs.Interop;
using AtkValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ECommons.ImGuiMethods;

namespace Automaton.Features.Debugging;

public unsafe class IslandDebug : DebugHelper
{
    public override string Name => $"{nameof(IslandDebug).Replace("Debug", "")} Debugging";

    public AgentMJICraftSchedule* Agent = (AgentMJICraftSchedule*)AgentModule.Instance()->GetAgentByInternalId(AgentId.MJICraftSchedule);
    public AgentMJICraftSchedule.AgentData* AgentData => Agent != null ? Agent->Data : null;
    private static byte[] R1 = new byte[2] { 0, 0 };
    private static byte[] R2 = new byte[2] { 0, 0 };
    private static byte[] R3 = new byte[2] { 0, 0 };
    private static byte[] R4 = new byte[2] { 0, 0 };
    private readonly List<byte[]> rests = new() { R1, R2, R3, R4 };

    public override void Draw()
    {
        ImGui.Text($"{Name}");
        ImGui.Separator();

        ImGui.Text($"OnIsland State: {MJIManager.Instance()->IsPlayerInSanctuary}");
        ImGui.Text($"Current Rank: {MJIManager.Instance()->IslandState.CurrentRank}");
        ImGui.Text($"Total Farm Slots: {MJIManager.Instance()->GetFarmSlotCount()}");
        ImGui.Text($"Total Pasture Slots: {MJIManager.Instance()->GetPastureSlotCount()}");
        ImGui.Text($"Current Mode: {MJIManager.Instance()->CurrentMode}");

        ImGui.Separator();

        ImGuiEx.TextV($"Rest Cycles: {string.Join(", ", GetCurrentRestDays())}");
        ImGui.SameLine();
        if (ImGui.Button($"Void Second Rest")) SetRestCycles(8321u);
        if (AgentData != null)
            ImGui.Text($"Rest Mask: {AgentData->RestCycles} || {AgentData->RestCycles:X}");
    }

    private List<int> GetCurrentRestDays()
    {
        var restDays1 = MJIManager.Instance()->CraftworksRestDays[0];
        var restDays2 = MJIManager.Instance()->CraftworksRestDays[1];
        var restDays3 = MJIManager.Instance()->CraftworksRestDays[2];
        var restDays4 = MJIManager.Instance()->CraftworksRestDays[3];

        return new List<int> { restDays1, restDays2, restDays3, restDays4 };
    }

    public void SetRestCycles(uint mask)
    {
        Svc.Log.Info($"Setting rest: {mask:X}");
        AgentData->NewRestCycles = mask;
        SynthesizeEvent(5, new AtkValue[] { new() { Type = AtkValueType.Int, Int = 0 } });
    }

    private void SynthesizeEvent(ulong eventKind, Span<AtkValue> args)
    {
        var eventData = stackalloc int[] { 0, 0, 0 };
        Agent->AgentInterface.ReceiveEvent(eventData, args.GetPointer(0), (uint)args.Length, eventKind);
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x40)]
    public unsafe partial struct AgentMJICraftSchedule
    {
        [StructLayout(LayoutKind.Explicit, Size = 0x98)]
        public unsafe partial struct ItemData
        {
            [FieldOffset(0x10)] public fixed ushort Materials[4];
            [FieldOffset(0x20)] public ushort ObjectId;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0xC)]
        public unsafe partial struct EntryData
        {
            [FieldOffset(0x0)] public ushort CraftObjectId;
            [FieldOffset(0x2)] public ushort u2;
            [FieldOffset(0x4)] public uint u4;
            [FieldOffset(0x8)] public byte StartingSlot;
            [FieldOffset(0x9)] public byte Duration;
            [FieldOffset(0xA)] public byte Started;
            [FieldOffset(0xB)] public byte Efficient;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x54)]
        public unsafe partial struct WorkshopData
        {
            [FieldOffset(0x00)] public byte NumScheduleEntries;
            [FieldOffset(0x08)] public fixed byte EntryData[6 * 0xC];
            [FieldOffset(0x50)] public uint UsedTimeSlots;

            public Span<EntryData> Entries => new(Unsafe.AsPointer(ref EntryData[0]), 6);
        }

        [StructLayout(LayoutKind.Explicit, Size = 0xB60)]
        public unsafe partial struct AgentData
        {
            [FieldOffset(0x000)] public int InitState;
            [FieldOffset(0x004)] public int SettingAddonId;
            [FieldOffset(0x0D0)] public StdVector<ItemData> Items;
            [FieldOffset(0x400)] public fixed byte WorkshopData[4 * 0x54];
            [FieldOffset(0x5A8)] public uint CurScheduleSettingObjectIndex;
            [FieldOffset(0x5AC)] public int CurScheduleSettingWorkshop;
            [FieldOffset(0x5B0)] public int CurScheduleSettingStartingSlot;
            [FieldOffset(0x7E8)] public byte CurScheduleSettingNumMaterials;
            [FieldOffset(0x810)] public uint RestCycles;
            [FieldOffset(0x814)] public uint NewRestCycles;
            [FieldOffset(0xB58)] public byte CurrentCycle; // currently viewed
            [FieldOffset(0xB59)] public byte CycleInProgress;
            [FieldOffset(0xB5A)] public byte CurrentIslandRank; // incorrect!

            public Span<WorkshopData> Workshops => new(Unsafe.AsPointer(ref WorkshopData[0]), 4);
        }

        [FieldOffset(0)] public AgentInterface AgentInterface;
        [FieldOffset(0x28)] public AgentData* Data;
    }
}
