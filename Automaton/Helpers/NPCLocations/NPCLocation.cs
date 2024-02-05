using Lumina.Excel.GeneratedSheets;

namespace Automaton.Helpers.NPCLocations;

public class NpcLocation
{
    public NpcLocation(float x, float y, TerritoryType territoryType, uint? map = null)
    {
        X = x;
        Y = y;
        TerritoryExcel = territoryType;
        MapId = map != null ? (uint)map : territoryType.Map.Row;
    }

    public TerritoryType TerritoryExcel { get; set; }

    public float MapX => ToMapCoordinate(X, TerritoryExcel.Map.Value.SizeFactor, TerritoryExcel.Map.Value.OffsetX);
    public float MapY => ToMapCoordinate(Y, TerritoryExcel.Map.Value.SizeFactor, TerritoryExcel.Map.Value.OffsetY);
    public float X { get; }
    public float Y { get; }
    public uint TerritoryType => TerritoryExcel.RowId;
    public uint MapId { get; }

    // TODO: This needs to be removed. This is an exact duplicate of Dalamud/Game/Text/SeStringHandling/Payloads/MapLinkPayload#ConvertRawPositionToMapCoordinate.cs
    private static float ToMapCoordinate(float val, float scale, short offset)
    {
        var c = scale / 100.0f;

        val = (val + offset) * c;

        return (41.0f / c * ((val + 1024.0f) / 2048.0f)) + 1;
    }
}
