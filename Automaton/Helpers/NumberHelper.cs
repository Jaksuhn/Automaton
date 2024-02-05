using System;
using System.Numerics;

namespace Automaton.Helpers;

public static class NumberHelper
{
    public static int RoundOff(this int i, int sliderIncrement)
    {
        var sliderAsDouble = Convert.ToDouble(sliderIncrement);
        return ((int)Math.Round(i / sliderAsDouble)) * sliderIncrement;
    }

    public static float RoundOff(this float i, float sliderIncrement) => (float)Math.Round(i / sliderIncrement) * sliderIncrement;

    public static string FormatTimeSpan(DateTime time)
    {
        var span = DateTime.UtcNow - time;

        if (span.Days > 0)
            return $"{span.Days} days ago";
        if (span.Hours > 0)
            return $"{span.Hours} hours ago";
        return span.Minutes > 0 ? $"{span.Minutes} minutes ago" : span.Seconds > 10 ? $"{span.Seconds} seconds ago" : "now";
    }

    public struct Angle(float radians = 0)
    {
        public const float RadToDeg = 180 / MathF.PI;
        public const float DegToRad = MathF.PI / 180;

        public float Rad = radians;
        public float Deg => Rad * RadToDeg;

        public static Angle FromDirection(Vector2 dir) => FromDirection(dir.X, dir.Y);
        public static Angle FromDirection(float x, float z) => new(MathF.Atan2(x, z));
        public readonly Vector2 ToDirection() => new(Sin(), Cos());

        public static Angle operator +(Angle a, Angle b) => new(a.Rad + b.Rad);
        public static Angle operator -(Angle a, Angle b) => new(a.Rad - b.Rad);
        public static Angle operator -(Angle a) => new(-a.Rad);
        public static Angle operator *(Angle a, float b) => new(a.Rad * b);
        public static Angle operator *(float a, Angle b) => new(a * b.Rad);
        public static Angle operator /(Angle a, float b) => new(a.Rad / b);
        public readonly Angle Abs() => new(Math.Abs(Rad));
        public readonly float Sin() => MathF.Sin(Rad);
        public readonly float Cos() => MathF.Cos(Rad);
        public readonly float Tan() => MathF.Tan(Rad);
        public static Angle Asin(float x) => new(MathF.Asin(x));
        public static Angle Acos(float x) => new(MathF.Acos(x));

        public readonly Angle Normalized()
        {
            var r = Rad;
            while (r < -MathF.PI)
                r += 2 * MathF.PI;
            while (r > MathF.PI)
                r -= 2 * MathF.PI;
            return new(r);
        }

        public readonly bool AlmostEqual(Angle other, float epsRad)
        {
            var delta = Math.Abs(Rad - other.Rad);
            return delta <= epsRad || delta >= (2 * MathF.PI) - epsRad;
        }

        public static bool operator ==(Angle l, Angle r) => l.Rad == r.Rad;
        public static bool operator !=(Angle l, Angle r) => l.Rad != r.Rad;
        public override readonly bool Equals(object? obj) => obj is Angle angle && this == angle;
        public override readonly int GetHashCode() => Rad.GetHashCode();
        public override string ToString() => Deg.ToString("f0");
    }
}
public static class AngleExtensions
{
    public static NumberHelper.Angle Radians(this float radians) => new(radians);
    public static NumberHelper.Angle Degrees(this float degrees) => new(degrees * NumberHelper.Angle.DegToRad);
    public static NumberHelper.Angle Degrees(this int degrees) => new(degrees * NumberHelper.Angle.DegToRad);
}
