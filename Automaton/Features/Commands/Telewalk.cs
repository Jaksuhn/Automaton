using Dalamud.Game.ClientState.Keys;
using ECommons.DalamudServices;
using Automaton.FeaturesSetup;
using System.Collections.Generic;
using System.Numerics;
using System;
using static Automaton.Helpers.Structs;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Automaton.Features.Debugging;

namespace Automaton.Features.Commands
{
    public unsafe class Telewalk : CommandFeature
    {
        public override string Name => "Telewalk";
        public override string Command { get; set; } = "/telewalk";
        public override string[] Alias => new string[] { "/tw" };
        public override string Description => "";
        public override List<string> Parameters => new() { "<displacement factor>" };
        public override bool isDebug => true;

        public override FeatureType FeatureType => FeatureType.Commands;

        private bool active;
        private float displacementFactor = 0.10f;

        protected override void OnCommand(List<string> args)
        {
            float.TryParse(args[0], out displacementFactor);
            if (!active)
            {
                active = true;
                Svc.Framework.Update += ModifyPOS;
                Svc.Log.Info("Enabling Telewalk");
            }
            else
            {
                active = false;
                Svc.Framework.Update -= ModifyPOS;
                Svc.Log.Info("Disabling Telewalk");
            }
        }

        private void ModifyPOS(IFramework framework)
        {
            var camera = (CameraEx*)CameraManager.Instance()->GetActiveCamera();
            var xDisp = -Math.Sin(camera->DirH);
            var zDisp = -Math.Cos(camera->DirH);
            var yDisp = Math.Sin(camera->DirV);

            if (Svc.ClientState.LocalPlayer != null)
            {
                var curPos = Svc.ClientState.LocalPlayer.Position;
                var newPos = Vector3.Multiply(displacementFactor, new Vector3((float)xDisp, Svc.ClientState.LocalPlayer.Position.Y, (float)zDisp));
                if (Svc.KeyState[VirtualKey.W])
                {
                    Svc.Log.Info("telewalking forwards");
                    PositionDebug.SetPos(curPos + newPos);
                }
                if (Svc.KeyState[VirtualKey.SPACE])
                {
                    if (!Svc.KeyState[VirtualKey.SHIFT])
                        PositionDebug.SetPos(curPos + new Vector3(0, Svc.ClientState.LocalPlayer.Position.Y * displacementFactor, 0));
                    else
                        PositionDebug.SetPos(curPos + new Vector3(0, Svc.ClientState.LocalPlayer.Position.Y * -displacementFactor, 0));
                }
            }
        }
    }
}
