using Dalamud.Game.ClientState.Keys;
using ECommons.DalamudServices;
using Automaton.FeaturesSetup;
using System.Collections.Generic;
using System.Numerics;
using System;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Automaton.Features.Debugging;
using Automaton.Helpers;

namespace Automaton.Features.Commands
{
    public unsafe class Telewalk : CommandFeature
    {
        public override string Name => "Telewalk";
        public override string Command { get; set; } = "/telewalk";
        public override string[] Alias => new string[] { "/tw" };
        public override string Description => "Replaces regular movement with teleporting. Works relative to your camera facing like normal movement.";
        public override List<string> Parameters => new() { "<displacement factor>" };
        public override bool isDebug => true;

        public override FeatureType FeatureType => FeatureType.Commands;

        private bool active;
        private float displacementFactor = 0.10f;

        protected override void OnCommand(List<string> args)
        {
            float.TryParse(args[0], out displacementFactor);
            displacementFactor = displacementFactor == 0 ? 0.10f : displacementFactor;
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
            if (!active) return;

            var camera = (Structs.CameraEx*)CameraManager.Instance()->GetActiveCamera();
            var xDisp = -Math.Sin(camera->DirH);
            var zDisp = -Math.Cos(camera->DirH);
            var yDisp = Math.Sin(camera->DirV);

            if (Svc.ClientState.LocalPlayer != null)
            {
                var curPos = Svc.ClientState.LocalPlayer.Position;
                if (Svc.KeyState[VirtualKey.W])
                    PositionDebug.SetPos(curPos + Vector3.Multiply(displacementFactor, new Vector3((float)xDisp, 0, (float)zDisp)));
                if (Svc.KeyState[VirtualKey.A])
                    PositionDebug.SetPos(curPos + Vector3.Multiply(displacementFactor, new Vector3((float)xDisp, 0, (float)zDisp)));
                if (Svc.KeyState[VirtualKey.S])
                    PositionDebug.SetPos(curPos + Vector3.Multiply(displacementFactor, new Vector3((float)xDisp, 0, (float)zDisp)));
                if (Svc.KeyState[VirtualKey.D])
                    PositionDebug.SetPos(curPos + -Vector3.Multiply(displacementFactor, new Vector3(-(float)xDisp, 0, -(float)zDisp)));

                if (Svc.KeyState[VirtualKey.SPACE] && !Svc.KeyState[VirtualKey.LSHIFT])
                    PositionDebug.SetPos(curPos + new Vector3(0, displacementFactor, 0));
                if (Svc.KeyState[VirtualKey.SPACE] && Svc.KeyState[VirtualKey.LSHIFT])
                    PositionDebug.SetPos(curPos + new Vector3(0, -displacementFactor, 0));
            }
        }
    }
}
