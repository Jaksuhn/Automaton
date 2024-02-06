using Automaton.FeaturesSetup;
using Automaton.Helpers;
using Automaton.Helpers.Faloop;
using Automaton.Helpers.Faloop.Model;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.DalamudServices;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace Automaton.Features.Chat;

internal class FaloopToChat : Feature
{
    public override string Name => "Echo Faloop";

    public override string Description => "Prints out faloop marks in chat. Requires sign in.";

    public override FeatureType FeatureType => FeatureType.ChatFeature;

    public Configs MainConfig { get; private set; }
    public Configs RankS { get; private set; }
    public Configs RankA { get; private set; }
    public Configs RankB { get; private set; }
    public Configs Fate { get; private set; }

    private readonly FaloopSession session = new();
    private readonly FaloopSocketIOClient socket = new();

    private readonly string[] jurisdictions = Enum.GetNames<Jurisdiction>();
    private readonly string[] channels = Enum.GetNames<XivChatType>();
    private readonly string[] majorPatches = Enum.GetNames<MajorPatch>();

    public class Configs : FeatureConfig
    {
        [FeatureConfigOption("Username")]
        public string FaloopUsername = string.Empty;

        [FeatureConfigOption("Password")]
        public string FaloopPassword = string.Empty;

        public int Channel = Enum.GetValues<XivChatType>().ToList().IndexOf(XivChatType.Echo);
        public int Jurisdiction;
        public Dictionary<MajorPatch, bool> MajorPatches = new()
        {
            {MajorPatch.ARealmReborn, true},
            {MajorPatch.Heavensward, true},
            {MajorPatch.Stormblood, true},
            {MajorPatch.Shadowbringer, true},
            {MajorPatch.Endwalker, true},
        };
        public bool EnableSpawnReport;
        public bool EnableSpawnTimestamp;
        public bool EnableDeathReport;
        public bool EnableDeathTimestamp;
        public bool DisableInDuty;
        public bool SkipOrphanReport;
    }

    protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
    {
        ImGui.InputText("Faloop Username", ref MainConfig.FaloopUsername, 32);
        ImGui.InputText("Faloop Password", ref MainConfig.FaloopPassword, 128);
        if (ImGui.Button("Save & Connect")) Connect();
        ImGui.SameLine();
        if (ImGui.Button("Emit mock payload")) EmitMockData();
        ImGui.SameLine();
        if (ImGui.Button("Kill Connection")) socket.Dispose();

        DrawPerRankConfig("Rank S", RankS);
        DrawPerRankConfig("Rank A", RankA);
        DrawPerRankConfig("Rank B", RankB);
        DrawPerRankConfig("Fate", Fate);
    };

    private void DrawPerRankConfig(string label, Configs rankConfig)
    {
        if (ImGui.CollapsingHeader(label))
        {
            ImGui.Indent();
            ImGui.Combo($"Channel##{label}", ref rankConfig.Channel, channels, channels.Length);
            ImGui.Combo($"Jurisdiction##{label}", ref rankConfig.Jurisdiction, jurisdictions, jurisdictions.Length);

            ImGui.Text("Expansions");
            ImGui.Indent();
            foreach (var patchVersion in Enum.GetValues<MajorPatch>())
            {
                ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(rankConfig.MajorPatches, patchVersion, out _);
                ImGui.Checkbox(Enum.GetName(patchVersion), ref value);
            }
            ImGui.Unindent();
            ImGui.NewLine();

            ImGui.Checkbox($"Report Spawns##{label}", ref rankConfig.EnableSpawnReport);
            if (rankConfig.EnableSpawnReport)
            {
                ImGui.Indent();
                ImGui.Checkbox($"Display Spawn Timestamp##{label}", ref rankConfig.EnableSpawnTimestamp);
                ImGui.Unindent();
            }
            ImGui.Checkbox($"Report Deaths##{label}", ref rankConfig.EnableDeathReport);
            if (rankConfig.EnableDeathReport)
            {
                ImGui.Indent();
                ImGui.Checkbox($"Display Death Timestamp##{label}", ref rankConfig.EnableDeathTimestamp);
                ImGui.Unindent();
            }
            ImGui.Checkbox($"Disable Reporting While in Duty##{label}", ref rankConfig.DisableInDuty);

            ImGui.Checkbox($"Skip Orphan Report##{label}", ref rankConfig.SkipOrphanReport);
            ImGui.Unindent();
        }
    }

    public override void Enable()
    {
        MainConfig = LoadConfig<Configs>($"{nameof(FaloopToChat)}{nameof(MainConfig)}") ?? new Configs();
        RankS = LoadConfig<Configs>($"{nameof(FaloopToChat)}{nameof(RankS)}") ?? new Configs();
        RankA = LoadConfig<Configs>($"{nameof(FaloopToChat)}{nameof(RankA)}") ?? new Configs();
        RankB = LoadConfig<Configs>($"{nameof(FaloopToChat)}{nameof(RankB)}") ?? new Configs();
        Fate = LoadConfig<Configs>($"{nameof(FaloopToChat)}{nameof(Fate)}") ?? new Configs();

        socket.OnConnected += OnConnected;
        socket.OnDisconnected += OnDisconnected;
        socket.OnError += OnError;
        socket.OnMobReport += OnMobReport;
        socket.OnAny += OnAny;
        socket.OnReconnected += OnReconnected;
        socket.OnReconnectError += OnReconnectError;
        socket.OnReconnectAttempt += OnReconnectAttempt;
        socket.OnReconnectFailed += OnReconnectFailed;
        socket.OnPing += OnPing;
        socket.OnPong += OnPong;

        Connect();
        CleanSpawnHistories();
        base.Enable();
    }

    public override void Disable()
    {
        SaveConfig(MainConfig, $"{nameof(FaloopToChat)}{nameof(MainConfig)}");
        SaveConfig(RankS, $"{nameof(FaloopToChat)}{nameof(RankS)}");
        SaveConfig(RankA, $"{nameof(FaloopToChat)}{nameof(RankA)}");
        SaveConfig(RankB, $"{nameof(FaloopToChat)}{nameof(RankB)}");
        SaveConfig(Fate, $"{nameof(FaloopToChat)}{nameof(Fate)}");
        socket.Dispose();
        base.Disable();
    }

    public class SpawnHistory
    {
        public uint MobId;
        public uint WorldId;
        public DateTime At;
    }

    public List<SpawnHistory> SpawnHistories = [];

    private static TextPayload GetRankIcon(string rank)
    {
        return rank switch
        {
            "S" => new TextPayload(SeIconChar.BoxedLetterS.ToIconString()),
            "A" => new TextPayload(SeIconChar.BoxedLetterA.ToIconString()),
            "B" => new TextPayload(SeIconChar.BoxedLetterB.ToIconString()),
            "F" => new TextPayload(SeIconChar.BoxedLetterF.ToIconString()),
            _ => throw new ArgumentException($"Unknown rank: {rank}"),
        };
    }

    private Configs GetRankConfig(string rank) =>
        rank switch
        {
            "S" => RankS,
            "A" => RankA,
            "B" => RankB,
            "F" => Fate,
            _ => default,
        };

    private void OnConnected() => PrintFullyQualifiedModuleMessage("Connected");

    private void OnDisconnected(string cause)
    {
        PrintFullyQualifiedModuleMessage("Disconnected.");
        Svc.Log.Warning($"Disconnected. Reason: {cause}");
    }

    private static void OnError(string error) => Svc.Log.Error($"Disconnected = {error}");

    private void OnMobReport(MobReportData data)
    {
        var mob = Svc.Data.GetExcelSheet<BNpcName>()?.GetRow(data.MobId);
        if (mob == default)
        {
            Svc.Log.Debug($"{nameof(OnMobReport)}: {nameof(mob)} == null");
            return;
        }

        var mobData = session.EmbedData.Mobs.FirstOrDefault(x => x.Id == data.MobId);
        if (mobData == default)
        {
            Svc.Log.Debug($"{nameof(OnMobReport)}: {nameof(mobData)} == null");
            return;
        }

        var world = Svc.Data.GetExcelSheet<World>()?.GetRow(data.WorldId);
        var dataCenter = world?.DataCenter?.Value;
        if (world == default || dataCenter == default)
        {
            Svc.Log.Debug($"{nameof(OnMobReport)}: {nameof(world)} == null || {nameof(dataCenter)} == null");
            return;
        }

        var currentWorld = Svc.ClientState.LocalPlayer?.CurrentWorld.GameData;
        var currentDataCenter = currentWorld?.DataCenter?.Value;
        if (currentWorld == default || currentDataCenter == default)
        {
            Svc.Log.Debug($"{nameof(OnMobReport)}: {nameof(currentWorld)} == null || {nameof(currentDataCenter)} == null");
            return;
        }

        var config = GetRankConfig(mobData.Rank);
        if (config == default)
        {
            Svc.Log.Debug($"{nameof(OnMobReport)}: {nameof(config)} == null");
            return;
        }

        if (!config.MajorPatches.TryGetValue(mobData.Version, out var value) || !value)
        {
            Svc.Log.Debug($"{nameof(OnMobReport)}: {nameof(majorPatches)}");
            return;
        }

        if (config.DisableInDuty && Svc.Condition[ConditionFlag.BoundByDuty])
        {
            Svc.Log.Debug($"{nameof(OnMobReport)}: in duty");
            return;
        }

        switch ((Jurisdiction)config.Jurisdiction)
        {
            case Jurisdiction.All:
            case Jurisdiction.Region when dataCenter.Region == currentDataCenter.Region:
            case Jurisdiction.DataCenter when dataCenter.RowId == currentDataCenter.RowId:
            case Jurisdiction.World when world.RowId == currentWorld.RowId:
                break;
            default:
                Svc.Log.Verbose($"{nameof(OnMobReport)}: unmatched");
                return;
        }

        switch (data.Action)
        {
            case "spawn" when config.EnableSpawnReport:
                OnSpawnMobReport(data, mob, world, config.Channel, mobData.Rank);
                Svc.Log.Verbose($"{nameof(OnMobReport)}: {OnSpawnMobReport}");
                break;
            case "death" when config.EnableDeathReport:
                OnDeathMobReport(data, mob, world, config.Channel, mobData.Rank, config.SkipOrphanReport);
                Svc.Log.Verbose($"{nameof(OnMobReport)}: {OnDeathMobReport}");
                break;
        }
    }

    private void OnSpawnMobReport(MobReportData data, BNpcName mob, World world, int channel, string rank)
    {
        var spawn = data.Data.Deserialize<MobReportData.Spawn>();
        if (spawn == default)
        {
            Svc.Log.Debug($"{nameof(OnSpawnMobReport)}: {nameof(spawn)} == null");
            return;
        }

        SpawnHistories.Add(new SpawnHistory
        {
            MobId = data.MobId,
            WorldId = data.WorldId,
            At = spawn.Timestamp,
        });

        var payloads = new List<Payload>
        {
            new TextPayload($"{SeIconChar.BoxedPlus.ToIconString()}"),
            GetRankIcon(rank),
            new TextPayload($" {mob.Singular.RawString} "),
        };

        var mapLink = CoordinatesHelper.CreateMapLink(spawn.ZoneId, spawn.ZonePoiIds.First(), data.ZoneInstance, session);
        if (mapLink != default)
            payloads.AddRange(mapLink.Payloads);

        payloads.AddRange(new Payload[]
        {
            new IconPayload(BitmapFontIcon.CrossWorld),
            new TextPayload($"{(GetRankConfig(rank).EnableSpawnTimestamp ? $"{world.Name} {NumberHelper.FormatTimeSpan(spawn.Timestamp)}": world.Name)}"),
        });

        Svc.Chat.Print(new XivChatEntry
        {
            Name = spawn.Reporters?.FirstOrDefault()?.Name ?? "Faloop",
            Message = new SeString(payloads),
            Type = Enum.GetValues<XivChatType>()[channel],
        });
    }

    private void OnDeathMobReport(MobReportData data, BNpcName mob, World world, int channel, string rank, bool skipOrphanReport)
    {
        var death = data.Data.Deserialize<MobReportData.Death>();
        if (death == default)
        {
            Svc.Log.Debug($"{nameof(OnDeathMobReport)}: {nameof(death)} == null");
            return;
        }

        if (skipOrphanReport && SpawnHistories.RemoveAll(x => x.MobId == data.MobId && x.WorldId == data.WorldId) == 0)
        {
            Svc.Log.Debug($"{nameof(OnDeathMobReport)}: {nameof(skipOrphanReport)}");
            return;
        }

        Svc.Chat.Print(new XivChatEntry
        {
            Name = "Faloop",
            Message = new SeString(new List<Payload>
            {
                new TextPayload($"{SeIconChar.Cross.ToIconString()}"),
                GetRankIcon(rank),
                new TextPayload($" {mob.Singular.RawString}"),
                new IconPayload(BitmapFontIcon.CrossWorld),
                new TextPayload($"{(GetRankConfig(rank).EnableDeathTimestamp ? $"{world.Name} {NumberHelper.FormatTimeSpan(death.StartedAt)}": world.Name)}"),
            }),
            Type = Enum.GetValues<XivChatType>()[channel],
        });
    }

    private static void OnAny(string name, SocketIOResponse response) => Svc.Log.Debug($"{nameof(OnAny)} Event {name} = {response}");

    private static void OnReconnected(int count) => Svc.Log.Debug($"Reconnected {count}");

    private static void OnReconnectError(Exception exception) => Svc.Log.Error($"Reconnect error {exception}");

    private static void OnReconnectAttempt(int count) => Svc.Log.Debug($"Reconnect attempt {count}");

    private static void OnReconnectFailed() => Svc.Log.Debug("Reconnect failed");

    private static void OnPing() => Svc.Log.Debug("Ping");

    private static void OnPong(TimeSpan span) => Svc.Log.Debug($"Pong: {span}");

    public void Connect()
    {
        if (string.IsNullOrWhiteSpace(MainConfig.FaloopUsername) || string.IsNullOrWhiteSpace(MainConfig.FaloopPassword))
        {
            PrintFullyQualifiedModuleMessage("Login information invalid.");
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                if (await session.LoginAsync(MainConfig.FaloopUsername, MainConfig.FaloopPassword))
                    await socket.Connect(session);
            }
            catch (Exception exception)
            {
                Svc.Log.Error($"Connection Failed {exception}");
            }
        });
    }

    public void EmitMockData()
    {
        Task.Run(async () =>
        {
            try
            {
                OnMobReport(MockData.SpawnMobReport);
                await Task.Delay(3000);
                OnMobReport(MockData.DeathMobReport);
            }
            catch (Exception exception)
            {
                Svc.Log.Error(nameof(EmitMockData) + $" {exception}");
            }
        });
    }

    private void CleanSpawnHistories() => SpawnHistories.RemoveAll(x => DateTime.UtcNow - x.At > TimeSpan.FromHours(1));
}
