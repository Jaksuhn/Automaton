using Automaton.FeaturesSetup;
using Dalamud.Utility;
using ECommons.DalamudServices;
using ImGuiNET;
using System.Linq;

namespace Automaton.Features.Actions;
internal class AutoTargetByName : Feature
{
    public override string Name => "Auto Target By Name";
    public override string Description => "When target exists, auto target them.";
    public override FeatureType FeatureType => FeatureType.Actions;

    public Configs Config { get; private set; }
    public class Configs : FeatureConfig
    {
        public string TargetName = string.Empty;
    }

    protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
    {
        ImGui.InputText("Target Name", ref Config.TargetName, 32);
    };

    public override void Enable()
    {
        base.Enable();
        Config = LoadConfig<Configs>() ?? new Configs();
        Svc.Framework.Update += OnUpdate;
    }

    public override void Disable()
    {
        base.Disable();
        SaveConfig(Config);
        Svc.Framework.Update -= OnUpdate;
    }

    private void OnUpdate(IFramework framework)
    {
        if (Svc.ClientState.LocalPlayer == null || !Svc.ClientState.LocalPlayer.IsTargetable) return;
        var t = Svc.Objects.First(o => o.IsTargetable && !Config.TargetName.IsNullOrEmpty() && o.Name.TextValue.Equals(Config.TargetName, System.StringComparison.InvariantCultureIgnoreCase));
        if (t != null)
            Svc.Targets.Target = t;
    }
}
