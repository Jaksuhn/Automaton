using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Automaton.Helpers.Faloop.Model.Embed;

namespace Automaton.Helpers.Faloop;

public class FaloopEmbedData(FaloopApiClient client)
{
    public List<ZoneLocationData> ZoneLocations { get; private set; } = new();
    public List<MobData> Mobs { get; private set; } = new();

    private readonly FaloopApiClient client = client;

    public async Task Initialize()
    {
        var script = await GetMainScript();

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        foreach (var node in ExtractJsonNodes(script))
        {
            if (node is JsonArray { Count: > 0 } array && array[0] is JsonObject obj)
            {
                if (new[] { "id", "key", "rank", "version", "zoneIds" }.All(x => obj.ContainsKey(x)))
                {
                    Mobs = array.Deserialize<List<MobData>>(options)!;
                }

                if (new[] { "id", "zoneId", "type", "location" }.All(x => obj.ContainsKey(x)))
                {
                    ZoneLocations = array.Deserialize<List<ZoneLocationData>>(options)!;
                }
            }
        }
    }

    private async Task<string> GetMainScript()
    {
        var html = await client.DownloadText(new Uri("https://faloop.app/"));
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(html);

        var script = document.QuerySelector("script[src^=\"main\"]");
        if (script == default)
        {
            throw new ApplicationException("Could not find main.js");
        }

        var src = script.GetAttribute("src");
        if (string.IsNullOrWhiteSpace(src))
        {
            throw new ApplicationException("src attribute not found.");
        }

        var uri = new UriBuilder("https", "faloop.app")
        {
            Path = src,
        };

        return await client.DownloadText(uri.Uri);
    }

    private static IEnumerable<JsonNode> ExtractJsonNodes(string content)
    {
        var regex = new Regex(@"JSON\.parse\('(.+?)'\)", RegexOptions.Multiline);
        foreach (var match in regex.Matches(content).Where(x => x.Success && x.Groups[1].Success))
        {
            var json = Regex.Unescape(match.Groups[1].Value);
            yield return JsonNode.Parse(json)!;
        }
    }
}
