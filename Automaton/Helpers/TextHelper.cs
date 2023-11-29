using Dalamud.Memory;
using Lumina.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Automaton.Helpers
{
    public static class TextHelper
    {
        public static string ToTitleCase(this string s) =>
            CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLower());

        public static string ParseSeStringLumina(SeString? luminaString)
            => luminaString == null ? string.Empty : Dalamud.Game.Text.SeStringHandling.SeString.Parse(luminaString.RawData).TextValue;

        public static string GetLast(this string source, int tail_length)
        {
            if (tail_length >= source.Length)
                return source;
            return source.Substring(source.Length - tail_length);
        }

        public static string FilterNonAlphanumeric(string input) => Regex.Replace(input, "[^\\p{L}\\p{N}]", string.Empty);

        public static unsafe string AtkValueStringToString(byte* atkString) => MemoryHelper.ReadSeStringNullTerminated(new nint(atkString)).ToString();
    }
}
