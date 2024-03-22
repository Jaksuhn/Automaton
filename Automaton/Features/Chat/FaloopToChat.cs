using Automaton.FeaturesSetup;
using Automaton.Helpers;
using Automaton.Helpers.Faloop;
using Automaton.Helpers.Faloop.Model;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons;
using ECommons.DalamudServices;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using SocketIOClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
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
        if (ImGui.InputText("Faloop Username", ref MainConfig.FaloopUsername, 32)) hasChanged = true;
        if (ImGui.InputText("Faloop Password", ref MainConfig.FaloopPassword, 128)) hasChanged = true;
        if (ImGui.Button("Save & Connect")) Connect();
        ImGui.SameLine();
        if (ImGui.Button("Emit mock payload")) EmitMockData();
        ImGui.SameLine();
        if (ImGui.Button("Kill Connection")) socket.Dispose();

        if (DrawPerRankConfig("Rank S", RankS)) hasChanged = true;
        if (DrawPerRankConfig("Rank A", RankA)) hasChanged = true;
        if (DrawPerRankConfig("Rank B", RankB)) hasChanged = true;
        if (DrawPerRankConfig("Fate", Fate)) hasChanged = true;
    };

    private bool DrawPerRankConfig(string label, Configs rankConfig)
    {
        var changed = false;
        if (ImGui.CollapsingHeader(label))
        {
            ImGui.Indent();
            if (ImGui.Combo($"Channel##{label}", ref rankConfig.Channel, channels, channels.Length)) changed = true;
            if (ImGui.Combo($"Jurisdiction##{label}", ref rankConfig.Jurisdiction, jurisdictions, jurisdictions.Length)) changed = true;

            ImGui.Text("Expansions");
            ImGui.Indent();
            foreach (var patchVersion in Enum.GetValues<MajorPatch>())
            {
                ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(rankConfig.MajorPatches, patchVersion, out _);
                if (ImGui.Checkbox(Enum.GetName(patchVersion), ref value)) changed = true;
            }
            ImGui.Unindent();
            ImGui.NewLine();

            if (ImGui.Checkbox($"Report Spawns##{label}", ref rankConfig.EnableSpawnReport)) changed = true;
            if (rankConfig.EnableSpawnReport)
            {
                ImGui.Indent();
                if (ImGui.Checkbox($"Display Spawn Timestamp##{label}", ref rankConfig.EnableSpawnTimestamp)) changed = true;
                ImGui.Unindent();
            }
            if (ImGui.Checkbox($"Report Deaths##{label}", ref rankConfig.EnableDeathReport)) changed = true;
            if (rankConfig.EnableDeathReport)
            {
                ImGui.Indent();
                if (ImGui.Checkbox($"Display Death Timestamp##{label}", ref rankConfig.EnableDeathTimestamp)) changed = true;
                ImGui.Unindent();
            }
            if (ImGui.Checkbox($"Disable Reporting While in Duty##{label}", ref rankConfig.DisableInDuty)) changed = true;

            if (ImGui.Checkbox($"Skip Orphan Report##{label}", ref rankConfig.SkipOrphanReport)) changed = true;
            ImGui.Unindent();
        }
        return changed;
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
                OnSpawnMobReport(new MobSpawnEvent(data, mob, world, mobData.Rank), config.Channel);
                Svc.Log.Verbose($"{nameof(OnMobReport)}: {OnSpawnMobReport}");
                break;
            case "death" when config.EnableDeathReport:
                OnDeathMobReport(new MobDeathEvent(data, mob, world, mobData.Rank), config.Channel, config.SkipOrphanReport);
                Svc.Log.Verbose($"{nameof(OnMobReport)}: {OnDeathMobReport}");
                break;
        }
    }

    private void OnSpawnMobReport(MobSpawnEvent ev, int channel)
    {
        SpawnHistories.Add(new SpawnHistory
        {
            MobId = ev.Data.MobId,
            WorldId = ev.Data.WorldId,
            At = ev.Spawn.Timestamp,
        });

        var payloads = new List<Payload>
        {
            new TextPayload($"{SeIconChar.BoxedPlus.ToIconString()}"),
            GetRankIcon(ev.Rank),
            new TextPayload($" {ev.Mob.Singular.RawString} "),
        };

        var mapLink = CoordinatesHelper.CreateMapLink(ev.Spawn.ZoneId, ev.Spawn.ZonePoiIds.First(), ev.Data.ZoneInstance, session);
        if (mapLink != default)
            payloads.AddRange(mapLink.Payloads);

        payloads.AddRange(new Payload[]
        {
            new IconPayload(BitmapFontIcon.CrossWorld),
            new TextPayload($"{(GetRankConfig(ev.Rank).EnableSpawnTimestamp ? $"{ev.World.Name} {NumberHelper.FormatTimeSpan(ev.Spawn.Timestamp)}": ev.World.Name)}"),
        });

        Svc.Chat.Print(new XivChatEntry
        {
            Name = ev.Spawn.Reporters?.FirstOrDefault()?.Name ?? "Faloop",
            Message = new SeString(payloads),
            Type = Enum.GetValues<XivChatType>()[channel],
        });
    }

    private void OnDeathMobReport(MobDeathEvent ev, int channel, bool skipOrphanReport)
    {
        if (skipOrphanReport && SpawnHistories.RemoveAll(x => x.MobId == ev.Data.MobId && x.WorldId == ev.Data.WorldId) == 0)
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
                    GetRankIcon(ev.Rank),
                    new TextPayload($" {ev.Mob.Singular.RawString} "),
                    new IconPayload(BitmapFontIcon.CrossWorld),
                    new TextPayload($"{(GetRankConfig(ev.Rank).EnableDeathTimestamp ? $"{ev.World.Name} {NumberHelper.FormatTimeSpan(ev.Death.StartedAt)}": ev.World.Name)}"),
                }),
            Type = Enum.GetValues<XivChatType>()[channel],
        });
    }

    // https://github.com/huntsffxiv/huntalerts/blob/main/HuntAlerts/Helpers/Utilities.cs
    //private static CancellationTokenSource CancellationTokenSource;
    //private static bool IsTaskRunning = false;
    //private static async void TaskTeleport(Lumina.Text.SeString world, uint startLocation, (int, int) zone)
    //{
    //    if (IsTaskRunning)
    //    {
    //        CancellationTokenSource.Cancel(); // Cancel the current task if running
    //        IsTaskRunning = false;
    //        return;
    //    }

    //    CancellationTokenSource = new CancellationTokenSource();
    //    var token = CancellationTokenSource.Token;
    //    IsTaskRunning = true;

    //    try
    //    {
    //        var hastoserverTransfer = false;
    //        var currentworldName = "";
    //        currentworldName = Svc.ClientState.LocalPlayer.CurrentWorld.GameData.Name;

    //        if (lifestreamEnabled && currentworldName != world)
    //        {
    //            if (currentworldName != world)
    //            {
    //                hastoserverTransfer = true;
    //                // Execute initial command
    //                Svc.Commands.ProcessCommand($"/li {world}");
    //            }
    //        }
    //        else
    //        {
    //            if (currentworldName != world)
    //            {
    //                Svc.Chat.Print("Can't teleport to hunt world without the Lifestream plugin being enabled as you are off world.");
    //                return;
    //            }
    //        }

    //        if (teleporterEnabled)
    //        {
    //            // Start loop
    //            var startTime = DateTime.Now;
    //            while (!token.IsCancellationRequested && (DateTime.Now - startTime).TotalSeconds <= 720)
    //            {

    //                // Check character's current world and logged in status here
    //                // if condition met, break loop and run another command
    //                if (Svc.ClientState.IsLoggedIn && Svc.ClientState.LocalPlayer != null)
    //                {
    //                    currentworldName = Svc.ClientState.LocalPlayer.CurrentWorld.GameData.Name;
    //                    Svc.Log.Verbose($"Player is logged in. Currentworld: " + currentworldName);

    //                    if (currentworldName == world)
    //                    {
    //                        var targetableStartTime = DateTime.Now;

    //                        // Loop until the player is targetable or until canceled
    //                        while (!token.IsCancellationRequested && (DateTime.Now - targetableStartTime).TotalSeconds <= 60) // Inner loop timeout (e.g., 60 seconds)
    //                        {
    //                            if (Svc.ClientState.LocalPlayer?.IsTargetable == true)
    //                            {
    //                                // Player is targetable, execute the command
    //                                // Code to execute when the button is pressed
    //                                if (startLocation is not "invalid" and not "")
    //                                {
    //                                    Svc.Log.Verbose($"Player is on hunt world, starting teleport to hunt location. Currentworld: " + currentworldName + "StartLocation: " + startLocation);
    //                                    if (hastoserverTransfer == true)
    //                                    {
    //                                        await Task.Delay(2000, token); // wait 2 seconds to start teleport
    //                                    }

    //                                    // Check and replace start location if a city is passed in
    //                                    if (startLocation.ToLower().Contains("limsa")) { startLocation = "Limsa"; }
    //                                    if (startLocation.ToLower().Contains("gridania")) { startLocation = "gridania"; }
    //                                    if (startLocation.ToLower().Contains("ul'dah") || startLocation.ToLower().Contains("uldah")) { startLocation = "ul'dah"; }

    //                                    Svc.Commands.ProcessCommand($"/tp {startLocation}");

    //                                    if (openmaponArrival == true && locationCoords != "")
    //                                    {
    //                                        Svc.Log.Verbose("Open map on arrival is enabled and coords exist");
    //                                        var flagtargetableStartTime = DateTime.Now;
    //                                        while (!token.IsCancellationRequested && (DateTime.Now - flagtargetableStartTime).TotalSeconds <= 60) // Inner loop timeout (e.g., 60 seconds)
    //                                        {

    //                                            var territoryType = Svc.ClientState.TerritoryType;
    //                                            var territoryName = Svc.Data.GetExcelSheet<TerritoryType>()
    //                                                                 .GetRow(territoryType)?.PlaceName.Value?.Name.ToString();

    //                                            Svc.Log.Verbose($"In Loop waiting on targetable and location match. Current Zone: {territoryName} | Destination Zone: {zone}");

    //                                            if ((Svc.ClientState.LocalPlayer?.IsTargetable == true) && (territoryName == zone))
    //                                            {
    //                                                await Task.Delay(500, token);
    //                                                Svc.Log.Verbose($"Opening map and flagging coordinates");
    //                                                FlagOnMap(locationCoords, zone);
    //                                                return;
    //                                            }
    //                                            await Task.Delay(1000, token); // wait 2 seconds to start teleport
    //                                        }
    //                                    }
    //                                    else
    //                                    {
    //                                        return;
    //                                    }
    //                                }
    //                                else if (zone != "invalid")
    //                                {
    //                                    Svc.Log.Verbose($"Player is on hunt world, starting teleport to hunt location. Currentworld: " + currentworldName + "StartZone: " + zone);
    //                                    Svc.Commands.ProcessCommand($"/tpm {zone}");
    //                                    return;
    //                                }
    //                            }

    //                            // Wait a bit before checking again
    //                            await Task.Delay(1000, token); // Check every second, for example
    //                        }
    //                    }
    //                }
    //                else
    //                {
    //                    Svc.Log.Verbose($"Player is still transfering");
    //                }

    //                await Task.Delay(5000, token); // Wait for 5 seconds
    //            }
    //        }
    //    }
    //    catch (TaskCanceledException)
    //    {
    //        // Handle cancellation
    //    }
    //    finally
    //    {
    //        IsTaskRunning = false;
    //    }
    //}

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
