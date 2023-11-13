using ECommons.DalamudServices;
using Automaton.FeaturesSetup;
using System;
using Automaton.Helpers;
using System.Linq;
using Dalamud.Game.Command;
using System.Collections.Generic;
using ECommons;
using System.Numerics;
using ImGuiNET;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Components;
using Dalamud.Game.Text.SeStringHandling.Payloads;

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

            [FeatureConfigOption("Don't follow if further than this (yalms)", "", 2, IntMin = 0, IntMax = 30, EditorSize = 300, HelpText = "Set to 0 to disable")]
            public int disableIfFurtherThan = 0;

            [FeatureConfigOption("Function only in duty", "", 3, IntMin = 0, IntMax = 30, EditorSize = 300)]
            public bool OnlyInDuty = false;

            [FeatureConfigOption("Change master on chat message", "", 3, IntMin = 0, IntMax = 30, EditorSize = 300, HelpText = "If a party chat message contains \"autofollow\", the current master will be switched to them.")]
            public bool changeMasterOnChat = false;
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
        {
            if (ImGui.Checkbox("Function Only in Duty", ref Config.OnlyInDuty)) hasChanged = true;
            if (ImGui.Checkbox("Change master on chat message", ref Config.changeMasterOnChat)) hasChanged = true;
            ImGuiComponents.HelpMarker("If a party chat message contains \"autofollow\", the current master will be switched to them.");
            ImGui.PushItemWidth(300);
            if (ImGui.SliderInt("Distance to Keep (yalms)", ref Config.distanceToKeep, 0, 30)) hasChanged = true;
            ImGui.PushItemWidth(300);
            if (ImGui.SliderInt("Disable if Further Than (yalms)", ref Config.disableIfFurtherThan, 0, 300)) hasChanged = true;

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
        private Dalamud.Game.ClientState.Objects.Types.GameObject? master;
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
            Svc.Chat.ChatMessage += OnChatMessage;
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
            Svc.Chat.ChatMessage -= OnChatMessage;
            base.Disable();
        }

        private void SetMaster()
        {
            try
            {
                master = Svc.Targets.Target;
                masterObjectID = Svc.Targets.Target.ObjectId;
            }
            catch { return; }
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
            if (Config.disableIfFurtherThan > 0 && Vector3.Distance(Svc.ClientState.LocalPlayer.Position, master.Position) > Config.disableIfFurtherThan) { movement.Enabled = false; return; }
            if (Config.OnlyInDuty && GameMain.Instance()->CurrentContentFinderConditionId == 0) { movement.Enabled = false; return; }

            movement.Enabled = true;
            movement.DesiredPosition = master.Position;
        }

        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (type != XivChatType.Party) return;
            var player = sender.Payloads.SingleOrDefault(x => x is PlayerPayload) as PlayerPayload;
            if (message.TextValue.ToLowerInvariant().Contains("autofollow", StringComparison.CurrentCultureIgnoreCase))
            {
                foreach (var actor in Svc.Objects)
                {
                    if (actor == null) continue;
                    Svc.Log.Info($"{actor.Name.TextValue} == {player.PlayerName} {actor.Name.TextValue.ToLowerInvariant().Equals(player.PlayerName)}");
                    if (actor.Name.TextValue.Equals(player.PlayerName) &&
                        ((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actor.Address)->GetIsTargetable())
                    {
                        Svc.Targets.Target = actor;
                        SetMaster();
                    }
                }
            }
        }
    }
}
