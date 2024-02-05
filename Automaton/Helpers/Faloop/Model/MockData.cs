using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Automaton.Helpers.Faloop.Model;

public static class MockData
{
    private static readonly MobReportData.Spawn Spawn = new()
    {
        ZoneId = 399,
        ZonePoiIds = [643],
        Timestamp = DateTime.Parse("2022-12-09T12:06:33.031Z"),
        Window = 1,
    };

    public static readonly MobReportData SpawnMobReport = new()
    {
        Action = "spawn",
        MobId = 4376,
        WorldId = 52,
        ZoneInstance = 0,
        Data = JsonObject.Create(JsonSerializer.SerializeToElement(Spawn, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }))!,
    };

    private static readonly MobReportData.Death Death = new()
    {
        Num = 1,
        StartedAt = DateTime.Parse("2022-12-09T12:09:38.718Z"),
        PrevStartedAt = DateTime.Parse("2022-12-05T02:28:12.931Z"),
    };

    public static readonly MobReportData DeathMobReport = new()
    {
        Action = "death",
        MobId = 4376,
        WorldId = 52,
        ZoneInstance = 0,
        Data = JsonObject.Create(JsonSerializer.SerializeToElement(Death, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }))!,
    };
}
