using Automaton.FeaturesSetup;
using Automaton.Helpers;
using Dalamud.Interface.Utility.Raii;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Collections.Generic;

namespace Automaton.Features.Experiments;

public unsafe class DefaultChatChannel : Feature
{
    public override string Name => "Default Chat Channel";
    public override string Description => "Sets the default chat channel.";

    public override FeatureType FeatureType => FeatureType.Disabled;

    public Configs Config { get; private set; }
    public class Configs : FeatureConfig
    {
        public int SelectedChannel = 3;
        public bool OnLogin = true;
        public bool OnZoneChange = false;
    }

    protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
    {
        try
        {
            if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("ChatLog", out var addon))
            {
                var b = new List<string>();
                for (var i = 0; i < addon->AtkValues[6].Int; i++)
                    b.Add(TextHelper.AtkValueStringToString(addon->AtkValues[8 + i].String));

                using var combo = ImRaii.Combo("channels", b[Config.SelectedChannel]);
                if (combo)
                    foreach (var x in b)
                    {
                        var selectedRoute = ImGui.Selectable(x, b.IndexOf(x) == Config.SelectedChannel);
                        if (selectedRoute)
                        {
                            Config.SelectedChannel = b.IndexOf(x);
                            hasChanged = true;
                        }
                    }
            }
        }
        catch (Exception ex) { ex.Log(); }
        if (ImGui.Checkbox("On Login", ref Config.OnLogin)) hasChanged = true;
        if (ImGui.Checkbox("On Zone Change", ref Config.OnZoneChange)) hasChanged = true;
    };

    public override void Enable()
    {
        Config = LoadConfig<Configs>() ?? new Configs();
        Svc.ClientState.Login += OnLogin;
        Svc.ClientState.TerritoryChanged += OnZoneChange;
        base.Enable();
    }

    public override void Disable()
    {
        SaveConfig(Config);
        Svc.ClientState.Login -= OnLogin;
        Svc.ClientState.TerritoryChanged -= OnZoneChange;
        base.Disable();
    }

    private void OnLogin()
    {
        if (!Config.OnLogin) return;
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("ChatLog", out var addon))
            Callback.Fire(addon, false, 4, Config.SelectedChannel, Config.SelectedChannel, 0);
    }

    private void OnZoneChange(ushort obj)
    {
        if (!Config.OnZoneChange) return;
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("ChatLog", out var addon))
            Callback.Fire(addon, false, 4, Config.SelectedChannel, Config.SelectedChannel, 0);
    }
}
