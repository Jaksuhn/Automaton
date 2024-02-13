using ECommons.DalamudServices;
using ECommons.EzHookManager;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Automaton.Helpers;

[StructLayout(LayoutKind.Explicit, Size = 0x18)]
public unsafe struct PlayerMoveControllerFlyInput
{
    [FieldOffset(0x0)] public float Forward;
    [FieldOffset(0x4)] public float Left;
    [FieldOffset(0x8)] public float Up;
    [FieldOffset(0xC)] public float Turn;
    [FieldOffset(0x10)] public float u10;
    [FieldOffset(0x14)] public byte DirMode;
    [FieldOffset(0x15)] public byte HaveBackwardOrStrafe;
}

public unsafe class OverrideMovement
{
    public bool Enabled
    {
        get => RMIWalkHook.IsEnabled;
        set
        {
            if (value)
            {
                RMIWalkHook.Enable();
                RMIFlyHook.Enable();
            }
            else
            {
                RMIWalkHook.Disable();
                RMIFlyHook.Disable();
            }
        }
    }

    public bool IgnoreUserInput; // if true - override even if user tries to change camera orientation, otherwise override only if user does nothing
    public Vector3 DesiredPosition;
    public float Precision = 0.01f;

    private delegate void RMIWalkDelegate(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk);
    [EzHook("E8 ?? ?? ?? ?? 80 7B 3E 00 48 8D 3D", false)]
    private readonly EzHook<RMIWalkDelegate> RMIWalkHook = null!;

    private delegate void RMIFlyDelegate(void* self, PlayerMoveControllerFlyInput* result);
    [EzHook("E8 ?? ?? ?? ?? 0F B6 0D ?? ?? ?? ?? B8", false)]
    private readonly EzHook<RMIFlyDelegate> RMIFlyHook = null!;

    public OverrideMovement()
    {
        EzSignatureHelper.Initialize(this);
        Svc.Log.Information($"RMIWalk address: 0x{RMIWalkHook.Address:X}");
        Svc.Log.Information($"RMIFly address: 0x{RMIFlyHook.Address:X}");
    }

    private void RMIWalkDetour(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk)
    {
        RMIWalkHook.Original(self, sumLeft, sumForward, sumTurnLeft, haveBackwardOrStrafe, a6, bAdditiveUnk);
        // TODO: we really need to introduce some extra checks that PlayerMoveController::readInput does - sometimes it skips reading input, and returning something non-zero breaks stuff...
        if (bAdditiveUnk == 0 && (IgnoreUserInput || (*sumLeft == 0 && *sumForward == 0)) && DirectionToDestination(false) is var relDir && relDir != null)
        {
            var dir = relDir.Value.h.ToDirection();
            *sumLeft = dir.X;
            *sumForward = dir.Y;
        }
    }

    private void RMIFlyDetour(void* self, PlayerMoveControllerFlyInput* result)
    {
        RMIFlyHook.Original(self, result);
        // TODO: we really need to introduce some extra checks that PlayerMoveController::readInput does - sometimes it skips reading input, and returning something non-zero breaks stuff...
        if ((IgnoreUserInput || (result->Forward == 0 && result->Left == 0 && result->Up == 0)) && DirectionToDestination(true) is var relDir && relDir != null)
        {
            var dir = relDir.Value.h.ToDirection();
            result->Forward = dir.Y;
            result->Left = dir.X;
            result->Up = relDir.Value.v.Rad;
        }
    }

    private (NumberHelper.Angle h, NumberHelper.Angle v)? DirectionToDestination(bool allowVertical)
    {
        var player = Svc.ClientState.LocalPlayer;
        if (player == null)
            return null;

        var dist = DesiredPosition - player.Position;
        if (dist.LengthSquared() <= Precision * Precision)
            return null;

        var dirH = NumberHelper.Angle.FromDirection(dist.X, dist.Z);
        var dirV = allowVertical ? NumberHelper.Angle.FromDirection(dist.Y, new Vector2(dist.X, dist.Z).Length()) : default;

        var camera = (CameraEx*)CameraManager.Instance()->GetActiveCamera();
        var cameraDir = camera->DirH.Radians() + 180.Degrees();
        return (dirH - cameraDir, dirV);
    }
}
