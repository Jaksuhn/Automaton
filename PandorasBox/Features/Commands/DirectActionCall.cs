using Dalamud.Logging;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;

namespace PandorasBox.Features.Commands
{
    public unsafe class DirectActionCall : CommandFeature
    {
        public override string Name => "Direct Action Call";
        public override string Command { get; set; } = "/directaction";
        public override string[] Alias => new string[] { "/ada" };
        public override string Description => "Call any action directly.";
        public override List<string> Parameters => new() { "[<ActionType>]", "[<ID>]" };

        public override FeatureType FeatureType => FeatureType.Commands;

        protected override void OnCommand(List<string> args)
        {
            try
            {
                var actionType = ParseActionType(args[0]);
                var actionID = uint.Parse(args[1]);
                PluginLog.Log($"Executing {Svc.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>(Svc.ClientState.ClientLanguage).GetRow(actionID).Name.RawString}");
                ActionManager.Instance()->UseActionLocation(actionType, actionID);
            }
            catch { }
        }

        private static ActionType ParseActionType(string input)
        {
            if (Enum.TryParse(input, true, out ActionType result))
                return result;

            if (byte.TryParse(input, out var intValue))
                if (Enum.IsDefined(typeof(ActionType), intValue))
                    return (ActionType)intValue;

            throw new ArgumentException("Invalid ActionType", nameof(input));
        }
    }
}
