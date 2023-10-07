using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

#pragma warning disable CS8618

namespace PandorasBox.Helpers.Faloop.Model;

public class MobReportData
{
    [JsonPropertyName("action")]
    public string Action { get; set; }

    [JsonPropertyName("data")]
    public JsonObject Data { get; set; }

    [JsonPropertyName("mobId")]
    public uint MobId { get; set; }

    [JsonPropertyName("worldId")]
    public uint WorldId { get; set; }

    [JsonPropertyName("zoneInstance")]
    public int? ZoneInstance { get; set; }

    public class Spawn
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("window")]
        public int Window { get; set; }

        [JsonPropertyName("zoneId")]
        public uint ZoneId { get; set; }

        [JsonPropertyName("zonePoiIds")]
        public List<int> ZonePoiIds { get; set; }

        [JsonPropertyName("reporters")]
        public List<Reporter>? Reporters { get; set; }

        public class Reporter
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }
        }
    }

    public class Death
    {
        [JsonPropertyName("num")]
        public int Num { get; set; }

        [JsonPropertyName("prevStartedAt")]
        public DateTime PrevStartedAt { get; set; }

        [JsonPropertyName("startedAt")]
        public DateTime StartedAt { get; set; }
    }
}
