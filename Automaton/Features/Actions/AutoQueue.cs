using Automaton.FeaturesSetup;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Automaton.Features.Actions;
internal class AutoQueue : Feature
{
    public override string Name => "Auto Queue";
    public override string Description => "";
    public override FeatureType FeatureType => FeatureType.Actions;
    public override bool isDebug => true;

    private delegate void AbandonDuty(bool a1);
    private AbandonDuty _abandonDuty;
    private List<string> peoples = [];
    public Configs Config { get; private set; }
    public class Configs : FeatureConfig
    {
        public List<string> peopleToCheckFor = [ ];
        public bool leaveIfAllArentPresent;
    }

    protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
    {
        try
        {
            var x = string.Empty;
            if (ImGui.InputText("Player Names", ref x, 32, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                peoples.Add(x);
                hasChanged = true;
            }
        }
        catch { }
        if (ImGui.Checkbox("Leave if all members are not present", ref Config.leaveIfAllArentPresent)) hasChanged = true;
        try
        {
            foreach (var person in peoples)
            {
                ImGui.TextUnformatted(person);
                ImGui.SameLine();
                if (ImGuiComponents.IconButton($"###{person}{peoples.IndexOf(person)}", FontAwesomeIcon.Trash))
                {
                    peoples.Remove(person);
                    SaveConfig(Config);
                }
            }

        }
        catch(Exception e) { e.Log();  }
    };

    public override void Enable()
    {
        base.Enable();
        Config = LoadConfig<Configs>() ?? new Configs();
        Svc.ClientState.TerritoryChanged += OnTerritoryChanged;
        Svc.DutyState.DutyStarted += OnDutyStart;
        Common.OnAddonSetup += AddonSetup;
        _abandonDuty = Marshal.GetDelegateForFunctionPointer<AbandonDuty>(Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B 43 28 B1 01"));
    }

    public override void Disable()
    {
        base.Disable();
        SaveConfig(Config);
        Svc.ClientState.TerritoryChanged -= OnTerritoryChanged;
        Svc.DutyState.DutyStarted -= OnDutyStart;
        Common.OnAddonSetup -= AddonSetup;
    }

    private unsafe void AddonSetup(SetupAddonArgs obj)
    {
        if (obj.AddonName != "ContentsFinder") return;
        Callback.Fire(obj.Addon, true, 12, 0);
    }

    private unsafe void OnTerritoryChanged(ushort obj)
    {
        if (GameMain.Instance()->CurrentContentFinderConditionId != 0) return;
        if (RouletteController.Instance()->GetPenaltyRemainingInMinutes(0) > 0) return;
        TaskManager.Enqueue(() => !GenericHelpers.IsOccupied());
        TaskManager.Enqueue(() => ECommons.Automation.Chat.Instance.SendMessage("/maincommand Duty Finder"));
    }

    private void OnDutyStart(object sender, ushort e)
    {
        if (Config.leaveIfAllArentPresent && peoples.Count > 0 && !new HashSet<string>(peoples).IsSubsetOf(Svc.Party.Select(p => p.Name.TextValue)))
            _abandonDuty(false);
    }
}
