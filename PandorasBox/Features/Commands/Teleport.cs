using Dalamud.Logging;
using ECommons;
using ECommons.DalamudServices;
using ImGuiNET;
using Automaton.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Automaton.Features.Commands
{
    public unsafe class Teleport : CommandFeature
    {
        public override string Name => "Teleport";
        public override string Command { get; set; } = "/ateleport";
        public override string[] Alias => new string[] { "/atp" };
        public override string Description => "";
        public override List<string> Parameters => new() { "<x offset>, <z offset>, <y offset>" };
        public override bool isDebug => true;

        public override FeatureType FeatureType => FeatureType.Commands;

        protected override void OnCommand(List<string> args)
        {
            try
            {
                var curPos = Svc.ClientState.LocalPlayer.Position;
                PluginLog.Log($"Moving from {curPos.X}, {curPos.Y}, {curPos.Z}");

                if (args[0].IsNullOrEmpty())
                {
                    SetPosToMouse();
                    return;
                }

                float.TryParse(args.ElementAtOrDefault(0), out var x);
                float.TryParse(args.ElementAtOrDefault(1), out var z);
                float.TryParse(args.ElementAtOrDefault(2), out var y);

                var newPos = curPos + new Vector3(x, z, y);
                PluginLog.Log($"Moving to {newPos.X}, {newPos.Y}, {newPos.Z}");
                SetPos(newPos);
            }
            catch { }
        }

        public static void SetPosToMouse()
        {
            if (Svc.ClientState.LocalPlayer == null) return;

            var mousePos = ImGui.GetIO().MousePos;
            Svc.GameGui.ScreenToWorld(mousePos, out var pos, 100000f);
            PluginLog.Log($"Moving from {pos.X}, {pos.Z}, {pos.Y}");
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
}
