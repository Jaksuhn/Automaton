using Automaton.FeaturesSetup;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace Automaton.UI;
internal class BasicWindow : Window
{
    private Feature Feature { get; set; }
    public BasicWindow(Feature t) : base($"{Name} - {t.Name}")
    {
        Feature = t;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        P.Ws.AddWindow(this);
    }

    public static void Dispose()
    {

    }
    public override void Draw() => Feature.DrawBasic();
}
