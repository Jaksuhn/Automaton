using Automaton.FeaturesSetup;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;

namespace Automaton.Features.Other;

public class GMAlert : Feature
{
    public override string Name => "GM Alert";
    public override string Description => "Chat message when a GM is nearby. Will cancel visland and SND automatically.";
    public override FeatureType FeatureType => FeatureType.Other;

    public Configs Config { get; private set; }
    public class Configs : FeatureConfig
    {
        public Dictionary<string, int> History { get; private set; } = [];
        public bool KillGameIfFound = false;
    }

    protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
    {
        if (ImGui.Checkbox("Kill game when GM is nearby", ref Config.KillGameIfFound)) hasChanged = true;
        ImGui.Spacing();
        ImGui.TextUnformatted("GM History");
        foreach (var player in Config.History)
            ImGui.TextUnformatted($"{player.Key}, met {player.Value} {(player.Value > 1 ? "times" : "time")} ");
    };

    public bool sent;

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

    private unsafe void OnUpdate(IFramework framework)
    {
        if (Svc.ClientState.LocalPlayer == null) return;

        foreach (var player in Svc.Objects.OfType<PlayerCharacter>().Where(pc => pc.ObjectId != 0xE000000))
            if (((Character*)player.Address)->CharacterData.OnlineStatus is <= 3 and > 0 && !sent)
            {
                if (!Config.History.TryGetValue(player.Name.TextValue, out var value))
                    Config.History.Add(player.Name.TextValue, 1);
                else
                    Config.History[player.Name.TextValue] = ++value;

                SaveConfig(Config);
                PrintFullyQualifiedModuleMessage($"{player.Name} is a GM!");
                sent = true;

                ECommons.Automation.Chat.Instance.SendMessage("/snd stop");
                ECommons.Automation.Chat.Instance.SendMessage("/visland stop");
                if (Config.KillGameIfFound)
                    ECommons.Automation.Chat.Instance.SendMessage("/xlkill");
                return;
            }

        sent = false;
    }
}
