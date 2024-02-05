using Automaton.FeaturesSetup;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.DalamudServices;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;

namespace Automaton.Features.Commands
{
    public unsafe class ColourText : CommandFeature
    {
        public override string Name => "Colour Text";
        public override string Command { get; set; } = "/colourtext";
        public override string Description => "Makes your text a random colour.";
        public override bool isDebug => true;

        public override FeatureType FeatureType => FeatureType.Commands;

        protected override void OnCommand(List<string> args)
        {
            var random = new Random((int)(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond % int.MaxValue));
            var randomRowIndex = random.Next(1, (int)(Svc.Data.GetExcelSheet<UIColor>().RowCount + 1));
            var bytes = new SeString(new Payload[]
            {
                new UIForegroundPayload((ushort)Svc.Data.GetExcelSheet<UIColor>().GetRow((uint)randomRowIndex).UIForeground),
                new TextPayload(string.Join(" ", args)),
                UIForegroundPayload.UIForegroundOff,
            }).Encode();
#pragma warning disable CS0618
            ECommons.Automation.Chat.Instance.SendMessageUnsafe(bytes);
#pragma warning restore CS0618
        }
    }
}
