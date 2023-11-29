using Automaton.Debugging;
using ImGuiNET;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.Interop.Attributes;
using System.Runtime.InteropServices;
using System.Numerics;
using ECommons.DalamudServices;
using System.Linq;
using ECommons.GameFunctions;

namespace Automaton.Features.Debugging;

public unsafe class ObjectDebug : DebugHelper
{
    public override string Name => $"{nameof(ObjectDebug).Replace("Debug", "")} Debugging";

    private float hbr;

    public override void Draw()
    {
        ImGui.Text($"{Name}");
        ImGui.Separator();

        foreach (var obj in Svc.Objects.Where(o => o.IsHostile()))
        {
            ImGui.Text($"{obj.Name} > {Vector3.Distance(Svc.ClientState.LocalPlayer.Position, obj.Position):f1}y");
            ImGui.PushItemWidth(200);
            ImGui.SliderFloat($"Hitbox Radius###{obj.Name}{obj.ObjectId}", ref ((GameObject*)obj.Address)->HitboxRadius, 0, 100);
        }
    }
    [StructLayout(LayoutKind.Explicit, Size = 0x1A0)]
    [VTableAddress("48 8d 05 ?? ?? ?? ?? c7 81 80 00 00 00 00 00 00 00", 3)]
    public unsafe partial struct GameObject
    {
        [FieldOffset(0x10)] public Vector3 DefaultPosition;
        [FieldOffset(0x20)] public float DefaultRotation;
        [FieldOffset(0x30)] public fixed byte Name[64];
        [FieldOffset(0x74)] public uint ObjectID; //TODO: rename to EntityId
        [FieldOffset(0x78)] public uint LayoutID;
        [FieldOffset(0x80)] public uint DataID; //TODO: raname to BaseId
        [FieldOffset(0x84)] public uint OwnerID;
        [FieldOffset(0x88)] public ushort ObjectIndex; // index in object table
        [FieldOffset(0x8C)] public byte ObjectKind;
        [FieldOffset(0x8D)] public byte SubKind;
        [FieldOffset(0x8E)] public byte Gender;
        [FieldOffset(0x90)] public byte YalmDistanceFromPlayerX;
        [FieldOffset(0x91)] public byte TargetStatus; // Goes from 6 to 2 when selecting a target and flashing a highlight
        [FieldOffset(0x92)] public byte YalmDistanceFromPlayerZ;
        [FieldOffset(0x95)] public ObjectTargetableFlags TargetableStatus; // Determines whether the game object can be targeted by the user
        [FieldOffset(0xB0)] public Vector3 Position;
        [FieldOffset(0xC0)] public float Rotation;
        [FieldOffset(0xC4)] public float Scale;
        [FieldOffset(0xC8)] public float Height;
        [FieldOffset(0xCC)] public float VfxScale;
        [FieldOffset(0xD0)] public float HitboxRadius;
        [FieldOffset(0xE0)] public Vector3 DrawOffset;
        [FieldOffset(0xF4)] public EventId EventId;
        [FieldOffset(0xF8)] public uint FateId;
        [FieldOffset(0x100)] public DrawObject* DrawObject;
        [FieldOffset(0x110)] public uint NamePlateIconId;
        [FieldOffset(0x114)] public int RenderFlags;
        [FieldOffset(0x158)] public LuaActor* LuaActor;
    }
}
