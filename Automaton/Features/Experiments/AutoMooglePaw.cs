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
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Automaton.Features.Experiments;

public class AutoMooglePaw : Feature
{
    public override string Name => "Auto Moogle's Paw";
    public override string Description => "Auto play the Moogle's Paw minigame in the Gold Saucer";
    public override FeatureType FeatureType => FeatureType.Other;

    public bool Initialized { get; set; }
    private VirtualKey ConflictKey { get; set; } = VirtualKey.SHIFT;

    public override void Enable()
    {
        base.Enable();
        Svc.Framework.Update += OnUpdate;
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "UfoCatcher", OnAddonSetup);

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

    private void OnAddonSetup(AddonEvent type, AddonArgs args)
    {
        TaskManager.Enqueue(WaitSelectStringAddon);
        TaskManager.Enqueue(ClickGameButton);
    }

    private void OnUpdate(IFramework framework)
    {
        if (!TaskManager.IsBusy) return;

        if (Svc.KeyState[ConflictKey])
        {
            TaskManager.Abort();
            Svc.PluginInterface.UiBuilder.AddNotification($"{nameof(ConflictKey)} used on {nameof(AutoMooglePaw)}", $"{nameof(Automaton)}", NotificationType.Success);
        }
    }

    private static unsafe bool? WaitSelectStringAddon()
    {
        return GenericHelpers.TryGetAddonByName<AddonSelectString>("SelectString", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase)
&& Click.TrySendClick("select_string1");
    }

    private unsafe bool? ClickGameButton()
    {
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("UfoCatcher", out var addon) && GenericHelpers.IsAddonReady(addon))
        {
            var button = addon->GetButtonNodeById(2);
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
        var machine = machineTarget.DataId == 2005036 ? (GameObject*)machineTarget.Address : null;

        if (machine != null)
        {
            TargetSystem.Instance()->InteractWithObject(machine);
            return true;
        }

        return false;
    }
}
