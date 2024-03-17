using Automaton.Helpers.Faloop.Model;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Text.Json;

namespace Automaton.Helpers.Faloop;
public record MobSpawnEvent(MobReportData Data, BNpcName Mob, World World, string Rank)
{
    public readonly MobReportData.Spawn Spawn = Data.Data.Deserialize<MobReportData.Spawn>() ?? throw new InvalidOperationException("Spawn is null");
}
