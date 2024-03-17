using Automaton.Helpers.Faloop.Model;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Text.Json;

namespace Automaton.Helpers.Faloop;
public record MobDeathEvent(MobReportData Data, BNpcName Mob, World World, string Rank)
{
    public readonly MobReportData.Death Death = Data.Data.Deserialize<MobReportData.Death>() ?? throw new InvalidOperationException("Death is null");
}
