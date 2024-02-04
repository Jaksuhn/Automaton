using Automaton.FeaturesSetup;
using ClickLib;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Internal.Notifications;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Automaton.Features.Experiments
{
    public class AutoCrystalTowerStriker : Feature
    {
        public override string Name => "Auto Crystal Tower Striker";
        public override string Description => "Auto play the Crystal Tower Striker minigame in the Gold Saucer";
        public override FeatureType FeatureType => FeatureType.Other;

        public bool Initialized { get; set; }
        private VirtualKey ConflictKey { get; set; } = VirtualKey.SHIFT;

        public override void Enable()
        {
            base.Enable();

            Svc.Framework.Update += OnUpdate;
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Hummer", OnAddonSetup);

            Initialized = true;
        }

        public override void Disable()
        {
            base.Disable();
            Svc.Framework.Update -= OnUpdate;
            Svc.AddonLifecycle.UnregisterListener(OnAddonSetup);
            TaskManager?.Abort();

            Initialized = false;
        }

        private void OnUpdate(IFramework framework)
        {
            if (!TaskManager.IsBusy) return;

            if (Svc.KeyState[ConflictKey])
            {
                TaskManager.Abort();
                Svc.PluginInterface.UiBuilder.AddNotification($"{nameof(ConflictKey)} used on {nameof(AutoCrystalTowerStriker)}", $"{nameof(Automaton)}", NotificationType.Success);
            }
        }

        private unsafe void OnAddonSetup(AddonEvent type, AddonArgs args)
        {
            TaskManager.Enqueue(WaitSelectStringAddon);
            TaskManager.Enqueue(ClickGameButton);
        }

        private static unsafe bool? WaitSelectStringAddon()
        {
            if (GenericHelpers.TryGetAddonByName<AddonSelectString>("SelectString", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
                return Click.TrySendClick("select_string1");

            return false;
        }

        private unsafe bool? ClickGameButton()
        {
            if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("Hummer", out var addon) && GenericHelpers.IsAddonReady(addon))
            {
                var button = addon->GetButtonNodeById(29);
                if (button == null || !button->IsEnabled) return false;

                addon->IsVisible = false;

                Callback.Fire(addon, true, 11, 3, 0);

                TaskManager.DelayNext(5000);
                TaskManager.Enqueue(StartAnotherRound);
                return true;
            }

            return false;
        }

        private static unsafe bool? StartAnotherRound()
        {
            if (GenericHelpers.IsOccupied()) return false;
            var machineTarget = Svc.Targets.PreviousTarget;
            var machine = machineTarget.DataId == 2005035 ? (GameObject*)machineTarget.Address : null;

            if (machine != null)
            {
                TargetSystem.Instance()->InteractWithObject(machine);
                return true;
            }

            return false;
        }
    }
}
