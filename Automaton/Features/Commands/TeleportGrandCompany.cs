using Automaton.FeaturesSetup;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;

namespace Automaton.Features.Commands;

public unsafe class TeleportGrandCompany : CommandFeature
{
    public override string Name => "Teleport to Grand Company";
    public override string Command { get; set; } = "/tpgc";
    public override string[] Alias => new string[] { "" };
    public override string Description => "";
    public override List<string> Parameters => new() { "" };

    public override FeatureType FeatureType => FeatureType.Commands;

    protected override void OnCommand(List<string> args)
    {
        var gc = UIState.Instance()->PlayerState.GrandCompany;
        switch (gc)
        {
            case 1:
                Svc.Commands.ProcessCommand($"/tp {Svc.Data.GetExcelSheet<Aetheryte>(Svc.ClientState.ClientLanguage).GetRow(8).PlaceName.Value.Name}");
                break;
            case 2:
                Svc.Commands.ProcessCommand($"/tp {Svc.Data.GetExcelSheet<Aetheryte>(Svc.ClientState.ClientLanguage).GetRow(2).PlaceName.Value.Name}");
                break;
            case 3:
                Svc.Commands.ProcessCommand($"/tp {Svc.Data.GetExcelSheet<Aetheryte>(Svc.ClientState.ClientLanguage).GetRow(9).PlaceName.Value.Name}");
                break;
        }
    }
}
