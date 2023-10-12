using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Automaton.Helpers.Faloop;
using ECommons.DalamudServices;
using ECommons.Logging;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Linq;

namespace Automaton.Helpers
{
    public static class CoordinatesHelper
    {
        public static Lumina.Excel.ExcelSheet<Aetheryte> Aetherytes = Svc.Data.GetExcelSheet<Aetheryte>(Svc.ClientState.ClientLanguage);
        public static Lumina.Excel.ExcelSheet<MapMarker> AetherytesMap = Svc.Data.GetExcelSheet<MapMarker>(Svc.ClientState.ClientLanguage);

        public class MapLinkMessage(ushort chatType, string sender, string text, float x, float y, float scale, uint territoryId, string placeName, DateTime recordTime)
        {
            public static MapLinkMessage Empty => new(0, string.Empty, string.Empty, 0, 0, 100, 0, string.Empty, DateTime.Now);

            public ushort ChatType = chatType;
            public string Sender = sender;
            public string Text = text;
            public float X = x;
            public float Y = y;
            public float Scale = scale;
            public uint TerritoryId = territoryId;
            public string PlaceName = placeName;
            public DateTime RecordTime = recordTime;
        }

        public static string GetNearestAetheryte(MapLinkMessage maplinkMessage)
        {
            var aetheryteName = "";
            double distance = 0;
            foreach (var data in Aetherytes)
            {
                if (!data.IsAetheryte) continue;
                if (data.Territory.Value == null) continue;
                if (data.PlaceName.Value == null) continue;
                var scale = maplinkMessage.Scale;
                if (data.Territory.Value.RowId == maplinkMessage.TerritoryId)
                {
                    var mapMarker = AetherytesMap.FirstOrDefault(m => m.DataType == 3 && m.DataKey == data.RowId);
                    if (mapMarker == null)
                    {
                        PluginLog.Error($"Cannot find aetherytes position for {maplinkMessage.PlaceName}#{data.PlaceName.Value.Name}");
                        continue;
                    }
                    var AethersX = ConvertMapMarkerToMapCoordinate(mapMarker.X, scale);
                    var AethersY = ConvertMapMarkerToMapCoordinate(mapMarker.Y, scale);
                    PluginLog.Log($"Aetheryte: {data.PlaceName.Value.Name} ({AethersX} ,{AethersY})");
                    var temp_distance = Math.Pow(AethersX - maplinkMessage.X, 2) + Math.Pow(AethersY - maplinkMessage.Y, 2);
                    if (aetheryteName == "" || temp_distance < distance)
                    {
                        distance = temp_distance;
                        aetheryteName = data.PlaceName.Value.Name;
                    }
                }
            }
            return aetheryteName;
        }

        private static float ConvertMapMarkerToMapCoordinate(int pos, float scale)
        {
            var num = scale / 100f;
            var rawPosition = (int)((float)(pos - 1024.0) / num * 1000f);
            return ConvertRawPositionToMapCoordinate(rawPosition, scale);
        }

        private static float ConvertRawPositionToMapCoordinate(int pos, float scale)
        {
            var num = scale / 100f;
            return (float)((((pos / 1000f * num) + 1024.0) / 2048.0 * 41.0 / num) + 1.0);
        }

        public static void TeleportToAetheryte(MapLinkMessage maplinkMessage)
        {
            var aetheryteName = GetNearestAetheryte(maplinkMessage);
            if (aetheryteName != "")
            {
                PluginLog.Log($"Teleporting to {aetheryteName}");
                Svc.Commands.ProcessCommand($"/tp {aetheryteName}");
            }
            else
            {
                PluginLog.Error($"Cannot find nearest aetheryte of {maplinkMessage.PlaceName}({maplinkMessage.X}, {maplinkMessage.Y}).");
            }
        }

        public static SeString? CreateMapLink(uint zoneId, int zonePoiId, int? instance, FaloopSession session)
        {
            var zone = Svc.Data.GetExcelSheet<TerritoryType>()?.GetRow(zoneId);
            var map = zone?.Map.Value;
            if (zone == default || map == default)
            {
                PluginLog.Debug("CreateMapLink: zone == null || map == null");
                return default;
            }

            var location = session.EmbedData.ZoneLocations.FirstOrDefault(x => x.Id == zonePoiId);
            if (location == default)
            {
                PluginLog.Debug("CreateMapLink: location == null");
                return default;
            }

            var n = 41 / (map.SizeFactor / 100.0);
            var loc = location.Location.Split(new[] { ',' }, 2)
                .Select(int.Parse)
                .Select(x => (x / 2048.0 * n) + 1)
                .Select(x => Math.Round(x, 1))
                .Select(x => (float)x)
                .ToList();

            var mapLink = SeString.CreateMapLink(zone.RowId, zone.Map.Row, loc[0], loc[1]);

            var instanceIcon = GetInstanceIcon(instance);
            return instanceIcon != default ? mapLink.Append(instanceIcon) : mapLink;
        }

        private static TextPayload? GetInstanceIcon(int? instance)
        {
            return instance switch
            {
                1 => new TextPayload(SeIconChar.Instance1.ToIconString()),
                2 => new TextPayload(SeIconChar.Instance2.ToIconString()),
                3 => new TextPayload(SeIconChar.Instance3.ToIconString()),
                _ => default,
            };
        }
    }
}
