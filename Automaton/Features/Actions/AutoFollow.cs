using ECommons.DalamudServices;
using Automaton.FeaturesSetup;
using System;
using Automaton.Helpers;
using System.Linq;
using Dalamud.Game.Command;
using System.Collections.Generic;
using ECommons;
using Dalamud.Game.ClientState.Objects.Types;
using System.Numerics;
using ImGuiNET;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace Automaton.Features.Actions
{
    public unsafe class AutoFollow : Feature
    {
        public override string Name => "Auto Follow";

        public override string Description => "True Auto Follow. Trigger with /autofollow while targeting someone. Use it with no target to wipe the current master.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public Configs Config { get; private set; }
        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Distance to Keep", "", 1, IntMin = 0, IntMax = 30, EditorSize = 300)]
            public int distanceToKeep = 3;

            [FeatureConfigOption("Function only in duty", "", 2, IntMin = 0, IntMax = 30, EditorSize = 300)]
            public bool OnlyInDuty = false;
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
        {
            if (ImGui.Checkbox("Function Only in Duty", ref Config.OnlyInDuty)) hasChanged = true;
            ImGui.PushItemWidth(300);
            if (ImGui.SliderInt("Distance to Keep (yalms)", ref Config.distanceToKeep, 0, 30)) hasChanged = true;

            ImGui.Text($"Current Master: {(master != null ? master.Name : "null")}");

            if (Svc.ClientState.LocalPlayer == null)
                ImGui.Text($"Your Position: x: null, y: null, z: null");
            else
                ImGui.Text($"Your Position: x: {Svc.ClientState.LocalPlayer.Position.X}, y: {Svc.ClientState.LocalPlayer.Position.Y}, z: {Svc.ClientState.LocalPlayer.Position.Z}");

            ImGui.Text($"Master Position: x: {(master != null ? master.Position.X : "null")}, y: {(master != null ? master.Position.Y : "null")}, z: {(master != null ? master.Position.Z : "null")}");
            ImGui.Text($"Distance to Master: {(master != null && Svc.ClientState.LocalPlayer != null ? Vector3.Distance(Svc.ClientState.LocalPlayer.Position, master.Position) : "null")}");

            if (ImGui.Button("Set")) { SetMaster(); }
            ImGui.SameLine();
            if (ImGui.Button("Clear")) { ClearMaster(); }
        };

        public string Command { get; set; } = "/autofollow";
        private readonly List<string> registeredCommands = new();

        private readonly OverrideMovement movement = new();
        private GameObject? master;
        private uint? masterObjectID;

        protected void OnCommand(List<string> args)
        {
            try
            {
                if (Svc.Targets.Target != null)
                    SetMaster();
                else
                    ClearMaster();
            }
            catch (Exception e) { e.Log(); }
        }

        protected virtual void OnCommandInternal(string _, string args)
        {
            args = args.ToLower();
            OnCommand(args.Split(' ').ToList());
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            if (Svc.Commands.Commands.ContainsKey(Command))
            {
                Svc.Log.Error($"Command '{Command}' is already registered.");
            }
            else
            {
                Svc.Commands.AddHandler(Command, new CommandInfo(OnCommandInternal)
                {
                    HelpMessage = "",
                    ShowInHelp = false
                });

                registeredCommands.Add(Command);
            }

            Svc.Framework.Update += Follow;
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            foreach (var c in registeredCommands)
            {
                Svc.Commands.RemoveHandler(c);
            }
            registeredCommands.Clear();

            Svc.Framework.Update -= Follow;
            base.Disable();
        }

        private void SetMaster()
        {
            master = Svc.Targets.Target;
            masterObjectID = Svc.Targets.Target.ObjectId;
        }

        private void ClearMaster()
        {
            master = null;
            masterObjectID = null;
        }

        private void Follow(IFramework framework)
        {
            master = Svc.Objects.FirstOrDefault(x => x.ObjectId == masterObjectID);

            if (master == null) { movement.Enabled = false; return; }
            if (Vector3.Distance(Svc.ClientState.LocalPlayer.Position, master.Position) <= Config.distanceToKeep) { movement.Enabled = false; return; }
            if (Config.OnlyInDuty && GameMain.Instance()->CurrentContentFinderConditionId == 0) { movement.Enabled = false; return; }

            movement.Enabled = true;
            movement.DesiredPosition = master.Position;
        }
    }
}
