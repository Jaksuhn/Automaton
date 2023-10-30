using Lumina.Text;
using System.Globalization;

namespace Automaton.Helpers
{
    public static class TextHelper
    {
        public static string ToTitleCase(this string s) =>
            CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLower());

        public static string ParseSeStringLumina(SeString? luminaString)
            => luminaString == null ? string.Empty : Dalamud.Game.Text.SeStringHandling.SeString.Parse(luminaString.RawData).TextValue;
    }
}
