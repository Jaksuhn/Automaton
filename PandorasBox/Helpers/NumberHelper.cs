using System;
using System.Text;

namespace PandorasBox.Helpers
{
    public static class NumberHelper
    {
        public static int RoundOff(this int i, int sliderIncrement)
        {
            var sliderAsDouble = Convert.ToDouble(sliderIncrement);
            return ((int)Math.Round(i / sliderAsDouble)) * (int)sliderIncrement;
        }

        public static float RoundOff(this float i, float sliderIncrement)
        {
            return (float)Math.Round(i / sliderIncrement) * sliderIncrement;
        }

        public static string FormatTimeSpan(DateTime time)
        {
            var span = DateTime.UtcNow - time;

            if (span.Days > 0)
                return $"{span.Days} days ago";
            if (span.Hours > 0)
                return $"{span.Hours} hours ago";
            if (span.Minutes > 0)
                return $"{span.Minutes} minutes ago";
            if (span.Seconds > 10)
                return $"{span.Seconds} seconds ago";

            return "now";
        }
    }
}
