using System.Numerics;
using System.Runtime.InteropServices;

namespace Automaton.Helpers
{
    public static class Structs
    {
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
    }
}
