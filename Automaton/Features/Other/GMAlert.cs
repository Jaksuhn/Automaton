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
    public override string Description => "Chat message when a GM is nearby";
    public override FeatureType FeatureType => FeatureType.Other;

    public Configs Config { get; private set; }
    public class Configs : FeatureConfig
    {
        public Dictionary<string, int> History { get; private set; } = [];
    }

    protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
    {
        ImGui.TextUnformatted("GM History");
        foreach (var player in Config.History)
            ImGui.TextUnformatted($"{player.Key}, met {player.Value} {(player.Value > 1 ? "times" : "time")} ");
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
        if (Svc.ClientState.LocalPlayer == null) return;

        foreach (var player in Svc.Objects.OfType<PlayerCharacter>().Where(pc => pc.ObjectId != 0xE000000))
            unsafe
            {
                if (((Character*)player.Address)->CharacterData.OnlineStatus is <= 3 and > 0)
                {
                    if (!Config.History.TryGetValue(player.Name.TextValue, out var value))
                        Config.History.Add(player.Name.TextValue, 1);
                    else
                        Config.History[player.Name.TextValue] = ++value;

                    SaveConfig(Config);
                    PrintFullyQualifiedModuleMessage(player.Name);
                }
            }
    }
}
