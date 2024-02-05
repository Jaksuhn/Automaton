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

public class AutoMonsterToss : Feature
{
    public override string Name => "Auto Monster Toss";
    public override string Description => "Auto play the Monster Toss minigame in the Gold Saucer";
    public override FeatureType FeatureType => FeatureType.Other;

    public bool Initialized { get; set; }
    private VirtualKey ConflictKey { get; set; } = VirtualKey.SHIFT;

    public override void Enable()
    {
        base.Enable();
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "BasketBall", OnAddonSetup);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "BasketBall", OnAddonSetup);
        Svc.Framework.Update += OnUpdate;
        Initialized = true;
    }

    public override void Disable()
    {
        base.Disable();
        Svc.Framework.Update -= OnUpdate;
        Svc.AddonLifecycle.UnregisterListener(OnAddonSetup);
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
            Svc.PluginInterface.UiBuilder.AddNotification($"{nameof(ConflictKey)} used on {nameof(AutoMonsterToss)}", $"{nameof(Automaton)}", NotificationType.Success);
        }
    }

    private unsafe void OnAddonSetup(AddonEvent type, AddonArgs args)
    {
        switch (type)
        {
            case AddonEvent.PostDraw:
                if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("BasketBall", out var addon) && GenericHelpers.IsAddonReady(addon))
                {
                    if (GenericHelpers.TryGetAddonByName<AddonSelectString>("SelectString", out var addonSelectString) && GenericHelpers.IsAddonReady(&addonSelectString->AtkUnitBase))
                    {
                        Click.TrySendClick("select_string1");
                        return;
                    }

                    var button = addon->GetButtonNodeById(10);
                    if (button == null || !button->IsEnabled) return;

                    addon->GetNodeById(12)->ChildNode->PrevSiblingNode->PrevSiblingNode->SetWidth(450);

                    Callback.Fire(addon, true, 11, 1, 0);
                }

                break;
            case AddonEvent.PreFinalize:
                TaskManager.Enqueue(StartAnotherRound);
                break;
        }
    }

    private static unsafe bool? StartAnotherRound()
    {
        if (GenericHelpers.IsOccupied()) return false;
        var machineTarget = Svc.Targets.PreviousTarget;
        var machine = machineTarget.DataId == 2004804 ? (GameObject*)machineTarget.Address : null;

        if (machine != null)
        {
            TargetSystem.Instance()->InteractWithObject(machine);
            return true;
        }

        return false;
    }
}
