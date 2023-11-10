using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

#pragma warning disable CS8618

namespace Automaton.Helpers.Faloop.Model;

public class FaloopEventPayload
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("subType")]
    public string SubType { get; set; }

    [JsonPropertyName("data")]
    public JsonObject Data { get; set; }
}
