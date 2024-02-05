using Automaton.Debugging;
using Dalamud.Game;
using ECommons.DalamudServices;
using ImGuiNET;
using System;
using static Automaton.Features.Debugging.HousingDebug;

namespace Automaton.Features.Debugging;

public unsafe class HousingDebug : DebugHelper
{
    public override string Name => $"{nameof(HousingDebug).Replace("Debug", "")} Debugging";

    public const string PositionInfo = "40 ?? 48 83 ?? ?? 33 DB 48 39 ?? ?? ?? ?? ?? 75 ?? 45";

    public override void Draw()
    {
        ImGui.Text($"{Name}");
        ImGui.Separator();

        var pia = new PositionInfoAddress(Svc.SigScanner);
        ImGui.Text($"District: {pia.Zone}");
        ImGui.Text($"Ward: {pia.Ward}");
        ImGui.Text($"House: {pia.House}");
        ImGui.Text($"Subdivision: {pia.Subdivision}");
        ImGui.Text($"Plot: {pia.Plot}");
        ImGui.Text($"Floor: {pia.Floor}");
    }

    public enum HousingZone : byte
    {
        Unknown = 0,
        Mist = 83,
        Goblet = 85,
        LavenderBeds = 84,
        Shirogane = 129,
        Firmament = 211,
    }

    public enum Floor : byte
    {
        Unknown = 0xFF,
        Ground = 0,
        First = 1,
        Cellar = 0x0A,
    }

    public class SeAddressBase
    {
        public readonly IntPtr Address;

        public SeAddressBase(ISigScanner sigScanner, string signature, int offset = 0)
        {
            Address = sigScanner.GetStaticAddressFromSig(signature);
            if (Address != IntPtr.Zero)
                Address += offset;
            var baseOffset = (ulong)Address.ToInt64() - (ulong)sigScanner.Module.BaseAddress.ToInt64();
            Svc.Log.Debug($"{GetType().Name} address 0x{Address.ToInt64():X16}, baseOffset 0x{baseOffset:X16}.");
        }
    }

    public sealed class PositionInfoAddress(ISigScanner sigScanner) : SeAddressBase(sigScanner, "40 ?? 48 83 ?? ?? 33 DB 48 39 ?? ?? ?? ?? ?? 75 ?? 45")
    {
        private readonly unsafe struct PositionInfo
        {
            private readonly byte* address;

            private PositionInfo(byte* address)
                => this.address = address;

            public static implicit operator PositionInfo(IntPtr ptr)
                => new((byte*)ptr);

            public static implicit operator PositionInfo(byte* ptr)
                => new(ptr);

            public static implicit operator bool(PositionInfo ptr)
                => ptr.address != null;

            public ushort House
                => (ushort)(address == null || !InHouse ? 0 : *(ushort*)(address + 0x96A0) + 1);

            public ushort Ward
                => (ushort)(address == null ? 0 : *(ushort*)(address + 0x96A2) + 1);

            public bool Subdivision
                => address != null && *(address + 0x96A9) == 2;

            public HousingZone Zone
                => address == null ? HousingZone.Unknown : *(HousingZone*)(address + 0x96A4);

            public byte Plot
                => (byte)(address == null || InHouse ? 0 : *(address + 0x96A8) + 1);

            public Floor Floor
                => address == null ? Floor.Unknown : *(Floor*)(address + 0x9704);

            private bool InHouse
                => *(address + 0x96A9) == 0;
        }

        private unsafe PositionInfo Info
        {
            get
            {
                var intermediate = *(byte***)Address;
                return intermediate == null ? null : *intermediate;
            }
        }

        public ushort Ward
            => Info.Ward;

        public HousingZone Zone
            => Info.Zone;

        public ushort House
            => Info.House;

        public bool Subdivision
            => Info.Subdivision;

        public byte Plot
            => Info.Plot;

        public Floor Floor
            => Info.Floor;
    }
}

public static class HousingZoneExtensions
{
    public static string ToName(this HousingZone z)
    {
        return z switch
        {
            HousingZone.Unknown => "Unknown",
            HousingZone.Mist => "Mist",
            HousingZone.Goblet => "The Goblet",
            HousingZone.LavenderBeds => "Lavender Beds",
            HousingZone.Shirogane => "Shirogane",
            HousingZone.Firmament => "Firmament",
            _ => throw new ArgumentOutOfRangeException(nameof(z), z, null)
        };
    }
}
