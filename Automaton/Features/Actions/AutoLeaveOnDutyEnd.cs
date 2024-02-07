using Automaton.FeaturesSetup;
using ECommons.DalamudServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automaton.Features.Actions;
internal class AutoLeaveOnDutyEnd : Feature
{
    public override string Name => "Auto Leave on Duty End";
    public override string Description => "";
    public override FeatureType FeatureType => FeatureType.Actions;

    public override void Enable()
    {
        base.Enable();
        Svc.DutyState.DutyCompleted += OnDutyComplete;
    }

    public override void Disable()
    {
        base.Disable();
        Svc.DutyState.DutyCompleted -= OnDutyComplete;
    }

    private void OnDutyComplete(object sender, ushort e) => ECommons.Automation.Chat.Instance.SendMessage("/leaveduty");
}
