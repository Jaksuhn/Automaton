using System.Globalization;

namespace Automaton.Helpers
{
    public static class TextHelper
    {
        public static string ToTitleCase(this string s) =>
            CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLower());
    }
}
