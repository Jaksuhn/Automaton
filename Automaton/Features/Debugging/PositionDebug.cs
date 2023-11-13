using Automaton.Debugging;
using Automaton.Helpers;
using Dalamud;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;

namespace Automaton.Features.Debugging;

public unsafe class PositionDebug : DebugHelper
{
    public override string Name => $"{nameof(PositionDebug).Replace("Debug", "")} Debugging";

    private float playerPositionX;
    private float playerPositionY;
    private float playerPositionZ;

    private bool noclip;
    private float displacementFactor = 0.10f;
    private readonly float cameriaH;
    private readonly float cameriaV;

    private Vector3 lastTargetPos;

    private readonly PlayerController* playerController = (PlayerController*)Svc.SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 3C 01 75 1E 48 8D 0D");
    private float speedMultiplier = 1;

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct PlayerController
    {
        [FieldOffset(0x10)] public PlayerMoveControllerWalk MoveControllerWalk;
        [FieldOffset(0x150)] public PlayerMoveControllerFly MoveControllerFly;
        [FieldOffset(0x559)] public byte ControlMode;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x140)]
    public unsafe struct PlayerMoveControllerWalk
    {
        [FieldOffset(0x10)] public Vector3 MovementDir;
        [FieldOffset(0x58)] public float BaseMovementSpeed;
        [FieldOffset(0x90)] public float MovementDirRelToCharacterFacing;
        [FieldOffset(0x94)] public byte Forced;
        [FieldOffset(0xA0)] public Vector3 MovementDirWorld;
        [FieldOffset(0xB0)] public float RotationDir;
        [FieldOffset(0x110)] public uint MovementState;
        [FieldOffset(0x114)] public float MovementLeft;
        [FieldOffset(0x118)] public float MovementFwd;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0xB0)]
    public unsafe struct PlayerMoveControllerFly
    {
        [FieldOffset(0x66)] public byte IsFlying;
        [FieldOffset(0x9C)] public float AngularAscent;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x2B0)]
    public unsafe struct CameraEx
    {
        [FieldOffset(0x130)] public float DirH; // 0 is north, increases CW
        [FieldOffset(0x134)] public float DirV; // 0 is horizontal, positive is looking up, negative looking down
        [FieldOffset(0x138)] public float InputDeltaHAdjusted;
        [FieldOffset(0x13C)] public float InputDeltaVAdjusted;
        [FieldOffset(0x140)] public float InputDeltaH;
        [FieldOffset(0x144)] public float InputDeltaV;
        [FieldOffset(0x148)] public float DirVMin; // -85deg by default
        [FieldOffset(0x14C)] public float DirVMax; // +45deg by default
    }

    public override void Draw()
    {
        ImGui.Text($"{Name}");
        ImGui.Separator();

        if (Svc.ClientState.LocalPlayer != null)
        {
            var curPos = Svc.ClientState.LocalPlayer.Position;
            playerPositionX = curPos.X;
            playerPositionY = curPos.Y;
            playerPositionZ = curPos.Z;

            ImGui.Text($"Your Position:");

            ImGui.PushItemWidth(75);
            ImGui.InputFloat("X", ref playerPositionX);
            ImGui.SameLine();
            DrawPositionModButtons("x");

            ImGui.PushItemWidth(75);
            ImGui.InputFloat("Y", ref playerPositionY);
            ImGui.SameLine();
            DrawPositionModButtons("y");

            ImGui.PushItemWidth(75);
            ImGui.InputFloat("Z", ref playerPositionZ);
            ImGui.SameLine();
            DrawPositionModButtons("z");

            if (ImGui.Checkbox("No Clip Mode", ref noclip))
            {
                if (noclip)
                    Svc.Framework.Update += NoClipMode;
                else
                    Svc.Framework.Update -= NoClipMode;
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Hold CTRL");
            ImGui.SameLine();
            ImGui.InputFloat("Displacement Factor", ref displacementFactor);

            ImGui.Separator();
        }

        var camera = (CameraEx*)CameraManager.Instance()->GetActiveCamera();
        ImGui.Text($"Camera H: {camera->DirH:f3}");
        ImGui.Text($"Camera V: {camera->DirV:f3}");

        ImGui.Separator();

        ImGui.Text($"Movement Speed: {playerController->MoveControllerWalk.BaseMovementSpeed}");
        ImGui.PushItemWidth(150);
        ImGui.SliderFloat("Speed Multiplier", ref speedMultiplier, 0, 20);
        ImGui.SameLine();
        if (ImGui.Button("Set")) SetSpeed(speedMultiplier * 6);
        ImGui.SameLine();
        if (ImGui.Button("Reset")) { speedMultiplier = 1; SetSpeed(speedMultiplier * 6); }

        ImGui.Separator();


        if (Svc.Targets.Target != null || Svc.Targets.PreviousTarget != null)
        {
            var targetPos = Svc.Targets.Target != null ? Svc.Targets.Target.Position : Svc.Targets.PreviousTarget.Position;
            var str = Svc.Targets.Target != null ? "Target" : "Last Target";

            ImGui.Text($"{str} Position: x: {targetPos.X:f3}, y: {targetPos.Y:f3}, z: {targetPos.Z:f3}");
            if (ImGui.Button($"TP to {str}")) SetPos(targetPos);
            ImGui.Text($"Distance to {str}: {Vector3.Distance(Svc.ClientState.LocalPlayer.Position, targetPos)}");

            ImGui.Separator();
        }

        Svc.GameGui.ScreenToWorld(ImGui.GetIO().MousePos, out var pos, 100000f);
        ImGui.Text($"Mouse Position: x: {pos.X:f3}, y: {pos.Y:f3}, z: {pos.Z:f3}");
        
        ImGui.Separator();

        var territoryID = Svc.ClientState.TerritoryType;
        var map = Svc.Data.GetExcelSheet<TerritoryType>()!.GetRow(territoryID);
        ImGui.Text($"Territory ID: {territoryID}");
        ImGui.Text($"Territory Name: {map!.PlaceName.Value?.Name}");
        
        if (Svc.ClientState.LocalPlayer != null)
            ImGui.Text($"Nearest Aetheryte: {CoordinatesHelper.GetNearestAetheryte(Svc.ClientState.LocalPlayer.Position, map)}");
    }

    private void NoClipMode(IFramework framework)
    {
        if (!noclip) return;

        var camera = (CameraEx*)CameraManager.Instance()->GetActiveCamera();
        var xDisp = -Math.Sin(camera->DirH);
        var zDisp = -Math.Cos(camera->DirH);
        var yDisp = Math.Sin(camera->DirV);

        if (Svc.ClientState.LocalPlayer != null)
        {
            var curPos = Svc.ClientState.LocalPlayer.Position;
            var newPos = Vector3.Multiply(displacementFactor, new Vector3((float)xDisp, (float)yDisp, (float)zDisp));
            if (ImGui.GetIO().KeyAlt)
                SetPos(curPos + newPos);
        }
    }

    private static void DrawPositionModButtons(string coord)
    {
        float[] buttonValues = { 1, 3, 5, 10 };

        foreach (var value in buttonValues)
        {
            var v = value;
            if (ImGui.Button($"+{value}###{coord}+{value}"))
            {
                var offset = Vector3.Zero;

                switch (coord)
                {
                    case "x":
                        offset.X = v;
                        break;
                    case "y":
                        offset.Y = v;
                        break;
                    case "z":
                        offset.Z = v;
                        break;
                };

                SetPos(Svc.ClientState.LocalPlayer.Position + offset);
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                v = -v;
                var offset = Vector3.Zero;

                switch (coord)
                {
                    case "x":
                        offset.X = v;
                        break;
                    case "y":
                        offset.Y = v;
                        break;
                    case "z":
                        offset.Z = v;
                        break;
                };

                SetPos(Svc.ClientState.LocalPlayer.Position + offset);
            }

            if (Array.IndexOf(buttonValues, value) < buttonValues.Length - 1)
            {
                ImGui.SameLine();
            }
        }
    }

    public static void SetSpeed(float speedBase)
    {
        Svc.SigScanner.TryScanText("f3 ?? ?? ?? ?? ?? ?? ?? e8 ?? ?? ?? ?? 48 ?? ?? ?? ?? ?? ?? 0f ?? ?? e8 ?? ?? ?? ?? f3 ?? ?? ?? ?? ?? ?? ?? f3 ?? ?? ?? ?? ?? ?? ?? f3 ?? ?? ?? f3", out var address);
        address = address + 4 + Marshal.ReadInt32(address + 4) + 4;
        SafeMemory.Write(address + 20, speedBase);
        SetMoveControlData(speedBase);
    }

    private static unsafe void SetMoveControlData(float speed) =>
        SafeMemory.Write(((delegate* unmanaged[Stdcall]<byte, nint>)Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 ?? ?? 74 ?? 83 ?? ?? 75 ?? 0F ?? ?? ?? 66"))(1) + 8, speed);

    public static void SetPosToMouse()
    {
        if (Svc.ClientState.LocalPlayer == null) return;

        var mousePos = ImGui.GetIO().MousePos;
        Svc.GameGui.ScreenToWorld(mousePos, out var pos, 100000f);
        Svc.Log.Info($"Moving from {pos.X}, {pos.Z}, {pos.Y}");
        if (pos != Vector3.Zero)
            SetPos(pos);
    }

    public static void SetPos(Vector3 pos) => SetPos(pos.X, pos.Z, pos.Y);

    public static unsafe void SetPos(float x, float y, float z)
    {
        if (SetPosFunPtr != IntPtr.Zero && Svc.ClientState.LocalPlayer != null)
        {
            ((delegate* unmanaged[Stdcall]<long, float, float, float, long>)SetPosFunPtr)(Svc.ClientState.LocalPlayer.Address, x, z, y);
        }
    }

    private static nint SetPosFunPtr => Svc.SigScanner.TryScanText("E8 ?? ?? ?? ?? 83 4B 70 01", out var ptr) ? ptr : IntPtr.Zero;
}
