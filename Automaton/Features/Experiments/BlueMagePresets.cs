using ECommons.DalamudServices;
using Automaton.FeaturesSetup;
using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System.Linq;
using System.Text.Json;
using Automaton.Helpers;
using ImGuiNET;
using Dalamud.Interface.Components;
using Dalamud.Interface;

namespace Automaton.Features.Testing
{
    public unsafe class BlueMagePresets : Feature
    {
        public override string Name => "Blue Mage Presets";
        public override string Description => "There's a good reason the game only gives us 5 spell loadouts to save. Those reasons are as follows:";

        public override FeatureType FeatureType => FeatureType.Disabled;

        public Configs Config { get; private set; }
        public override bool UseAutoConfig => false;
        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Loadouts")]
            public List<Loadout> Loadouts { get; set; } = new();
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();

            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);

            base.Disable();
        }

        [Serializable]
        public class Loadout(string name = "Unnamed Loadout")
        {
            public string Name { get; set; } = name;
            public uint[] Actions { get; set; } = new uint[24];

            public static Loadout FromPreset(string preset)
            {
                try
                {
                    var bytes = Convert.FromBase64String(preset);
                    var str = System.Text.Encoding.UTF8.GetString(bytes);
                    return JsonSerializer.Deserialize<Loadout>(str);
                }
                catch (Exception)
                {
                    return null;
                }
            }

            public string ToPreset() => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this)));

            public int ActionCount(uint id) => Actions.Count(x => x == id);

            public unsafe bool ActionUnlocked(uint id)
            {
                var normalId = Misc.AozToNormal(id);
                var link = Misc.Action.GetRow(normalId)!.UnlockLink;
                return UIState.Instance()->IsUnlockLinkUnlocked(link);
            }

            public bool CanApply()
            {
                if (Svc.ClientState.LocalPlayer?.ClassJob.Id != 36) return false;
                if (Svc.Condition[ConditionFlag.InCombat]) return false;

                foreach (var action in Actions)
                {
                    if (action > Misc.AozAction.RowCount) return false;

                    if (action != 0)
                    {
                        if (ActionCount(action) > 1) return false;
                        if (!ActionUnlocked(action)) return false;
                    }
                }

                return true;
            }

            public unsafe bool Apply()
            {
                var actionManager = ActionManager.Instance();

                var arr = new uint[24];
                for (var i = 0; i < 24; i++)
                {
                    arr[i] = Misc.AozToNormal(Actions[i]);
                }

                fixed (uint* ptr = arr)
                {
                    var ret = actionManager->SetBlueMageActions(ptr);
                    if (ret == false) return false;
                }

                //if (Config.ApplyToHotbars)
                //{
                //    this.ApplyToHotbar(
                //        Config.HotbarOne,
                //        Actions[..12]
                //    );

                //    this.ApplyToHotbar(
                //        Config.HotbarTwo,
                //        Actions[12..]
                //    );
                //}

                return true;
            }

            private unsafe void ApplyToHotbar(int id, uint[] aozActions)
            {
                var hotbarModule = RaptureHotbarModule.Instance();

                for (var i = 0; i < 12; i++)
                {
                    var aozAction = aozActions[i];
                    var normalAction = Misc.AozToNormal(aozAction);
                    var slot = hotbarModule->GetSlotById((uint)(id - 1), (uint)i);

                    if (normalAction == 0)
                    {
                        // DO NOT SET ACTION 0 YOU WILL GET CURE'D
                        slot->Set(
                            HotbarSlotType.Empty,
                            0
                        );
                    }
                    else
                    {
                        slot->Set(
                            HotbarSlotType.Action,
                            normalAction
                        );
                    }
                }
            }
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.FileImport))
            {
                var loadout = new Loadout();
                var activeActions = new List<uint>();
                for (var i = 0; i < 24; i++)
                    loadout.Actions.SetValue(Misc.NormalToAoz(ActionManager.Instance()->GetActiveBlueMageActionInSlot(i)), i);
                Config.Loadouts.Add(loadout);
                hasChanged = true;
            }

            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Create preset from current spell loadout.");
            try
            {
                foreach (var loadout in Config.Loadouts)
                {
                    var label = loadout.Name + "##" + loadout.GetHashCode();
                    ImGui.Text(label);
                }
            }
            catch
            { }
            if (hasChanged)
                SaveConfig(Config);
        };
    }
}
