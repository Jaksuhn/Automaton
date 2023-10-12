using Dalamud;
using Dalamud.Logging;
using ECommons.DalamudServices;
using PandorasBox.FeaturesSetup;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PandorasBox.Features.Commands
{
    public unsafe class MoveSpeed : CommandFeature
    {
        public override string Name => "Modify Movement Speed";
        public override string Command { get; set; } = "/movespeed";
        public override string[] Alias => new string[] { "/move", "/speed" };
        public override string Description => "";
        public override List<string> Parameters => new() { "[<speed>]" };

        public override FeatureType FeatureType => FeatureType.Commands;

        // why is this not normalised to 1?!?!
        internal static float offset = 6;

        protected override void OnCommand(List<string> args)
        {
            try
            {
                if (args.Count == 0) { SetSpeed(offset); return; }

                var speed = float.Parse(args[0]);
                SetSpeed(speed * offset);
                PluginLog.Log($"Setting move speed to {speed}");
            }
            catch { }
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
    }
}
