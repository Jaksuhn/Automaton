using Automaton.Debugging;
using Automaton.FeaturesSetup;
using ImGuiNET;
using System.Linq;
using System.Reflection;

namespace Automaton.Features.Debugging;

public unsafe class FeatureDebug : DebugHelper
{
    public override string Name => $"{nameof(FeatureDebug).Replace("Debug", "")} Debugging";

    private readonly FeatureProvider provider = new(Assembly.GetExecutingAssembly());

    public override void Draw()
    {
        ImGui.Text($"{Name}");
        ImGui.Separator();

        if (ImGui.Button("Load Features"))
        {
            provider.LoadFeatures();
            P.FeatureProviders.Add(provider);
        }
        if (ImGui.Button("Unload Features"))
        {
            foreach (var f in P.Features.Where(x => x is not null && x.Enabled))
            {
                f.Disable();
            }
            P.FeatureProviders.Clear();
            provider.UnloadFeatures();
        }
    }
}
