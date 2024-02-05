using Automaton.FeaturesSetup;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using System.Collections.Generic;

namespace Automaton.Features.Commands;

public unsafe class FakeTranslate : CommandFeature
{
    public override string Name => "Fake Translate";
    public override string Command { get; set; } = "/faketranslate";
    public override string Description => "Use those funny translate arrows on any text";
    public override bool isDebug => true;

    public override FeatureType FeatureType => FeatureType.Commands;

    protected override void OnCommand(List<string> args)
    {
        var bytes = new SeString(new Payload[]
        {
            new IconPayload(BitmapFontIcon.AutoTranslateBegin),
            new TextPayload(string.Join(" ", args)),
            new IconPayload(BitmapFontIcon.AutoTranslateEnd),
        }).Encode();
#pragma warning disable CS0618
        ECommons.Automation.Chat.Instance.SendMessageUnsafe(bytes);
#pragma warning restore CS0618
    }
}
