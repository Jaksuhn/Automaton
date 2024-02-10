using Automaton.FeaturesSetup;
using ECommons.DalamudServices;
using System.Runtime.InteropServices;

namespace Automaton.Features.Actions;
internal class AutoLeaveOnDutyEnd : Feature
{
    public override string Name => "Auto Leave on Duty End";
    public override string Description => "";
    public override FeatureType FeatureType => FeatureType.Actions;

    private delegate void AbandonDuty(bool a1);
    private AbandonDuty _abandonDuty;

    public override bool UseAutoConfig => true;
    public Configs Config { get; private set; }
    public class Configs : FeatureConfig
    {
        [FeatureConfigOption("Time to wait until leave (ms)", EditorSize = 300, IntMin = 0, IntMax = 600000)]
        public int timeToWait = 0;
    }

    public override void Enable()
    {
        base.Enable();
        Config = LoadConfig<Configs>() ?? new Configs();
        Svc.DutyState.DutyCompleted += OnDutyComplete;
        _abandonDuty = Marshal.GetDelegateForFunctionPointer<AbandonDuty>(Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B 43 28 B1 01"));
    }

    public override void Disable()
    {
        base.Disable();
        SaveConfig(Config);
        Svc.DutyState.DutyCompleted -= OnDutyComplete;
    }

    private void OnDutyComplete(object sender, ushort e)
    {
        TaskManager.DelayNext(Config.timeToWait);
        TaskManager.Enqueue(() => _abandonDuty(false));
    }
}
