using Automaton.FeaturesSetup;
using Automaton.UI;
using Dalamud.Game.Command;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using ECommons;
using ECommons.DalamudServices;
using ECommons.EzHookManager;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using Lumina.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Automaton.Features.UI;
internal unsafe class AchievementTracker : Feature
{
    public override string Name => "Achievement Tracker";
    public override string Description => $"Adds an achievement tracker. Open with {Command}";
    public override FeatureType FeatureType => FeatureType.UI;
    public static string Command => "/atracker";
    private readonly List<string> registeredCommands = [];

    private BasicWindow window;

    private ExcelSheet<Lumina.Excel.GeneratedSheets.Achievement> achvSheet;
    private Lumina.Excel.GeneratedSheets.Achievement selectedAchievement;
    internal static string Search = string.Empty;
    private Vector2 iconButtonSize = ImGuiHelpers.GetButtonSize(FontAwesomeIcon.Trash.ToIconString());
    private DateTime lastCallTime;

    public delegate void ReceiveAchievementProgressDelegate(Achievement* achievement, uint id, uint current, uint max);
    [EzHook("C7 81 ?? ?? ?? ?? ?? ?? ?? ?? 89 91 ?? ?? ?? ?? 44 89 81")]
    public EzHook<ReceiveAchievementProgressDelegate> ReceiveAchievementProgressHook = null!;

    public Configs Config { get; private set; }
    public class Configs : FeatureConfig
    {
        public List<Achv> Achievements = [];
        public Vector4 BarColour = Vector4.One;
        public int UpdateFrequency = 60;
        public bool AutoRemoveCompleted = false;
    }

    protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
    {
        var edited = false;
        ImGuiEx.TextV("Bar Colour: ");
        ImGui.SameLine();
        var newColor = ImGuiComponents.ColorPickerWithPalette(1, "##color", Config.BarColour, ImGuiColorEditFlags.NoAlpha);

        edited |= !Config.BarColour.Equals(newColor);

        if (edited)
        {
            Config.BarColour = newColor;
            hasChanged = true;
        }

        if (ImGui.SliderInt("Update Frequency (s)", ref Config.UpdateFrequency, 60, 600, "%d", ImGuiSliderFlags.AlwaysClamp)) hasChanged = true;
        if (ImGui.Checkbox("Auto Remove Completed", ref Config.AutoRemoveCompleted)) hasChanged = true;
    };

    public class Achv
    {
        public uint ID;
        public string Name;
        public uint CurrentProgress;
        public uint MaxProgress;
        public string Description = string.Empty;
        public byte Points = 0;
        public bool Completed => CurrentProgress != default && CurrentProgress >= MaxProgress;
    }

    public override void Enable()
    {
        base.Enable();
        Config = LoadConfig<Configs>() ?? new Configs();
        achvSheet = Svc.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Achievement>();

        if (Svc.Commands.Commands.ContainsKey(Command))
        {
            Svc.Log.Error($"Command '{Command}' is already registered.");
        }
        else
        {
            Svc.Commands.AddHandler(Command, new CommandInfo(OnCommand)
            {
                HelpMessage = "",
                ShowInHelp = false
            });

            registeredCommands.Add(Command);
        }

        EzSignatureHelper.Initialize(this);
        ReceiveAchievementProgressHook.Enable();

        window = new BasicWindow(this);
    }

    public override void Disable()
    {
        base.Disable();
        SaveConfig(Config);
        P.Ws.RemoveWindow(window);

        foreach (var c in registeredCommands)
        {
            Svc.Commands.RemoveHandler(c);
        }
        registeredCommands.Clear();
    }

    protected virtual void OnCommand(string _, string args) => window.IsOpen = !window.IsOpen;

    public override void Draw()
    {
        if (!Player.Available) return;

        try
        {
            DrawAchievementSearch();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            DrawTracker();
        }
        catch (Exception e)
        {
            Svc.Log.Error(e.ToString());
        }
    }

    private void ReceiveAchievementProgressDetour(Achievement* achievement, uint id, uint current, uint max)
    {
        try
        {
            Svc.Log.Debug($"{nameof(ReceiveAchievementProgressDetour)}: [{id}] {current} / {max}");
            foreach (var achv in Config.Achievements)
            {
                if (achv.ID == id)
                {
                    achv.CurrentProgress = current;
                    achv.MaxProgress = max;
                }
            }
        }
        catch (Exception e)
        {
            Svc.Log.Error("Error receiving achievement progress: {e}", e);
        }

        ReceiveAchievementProgressHook.Original(achievement, id, current, max);
    }

    private void RequestUpdate(uint id = 0)
    {
        if (id == 0)
            Config.Achievements.Where(a => !a.Completed).ToList().ForEach(achv => Achievement.Instance()->RequestAchievementProgress(achv.ID));
        else
            Achievement.Instance()->RequestAchievementProgress(id);
    }

    private void DrawAchievementSearch()
    {
        var timeSinceLastCall = DateTime.Now - lastCallTime;

        if (timeSinceLastCall.TotalSeconds >= Config.UpdateFrequency)
        {
            RequestUpdate();
            lastCallTime = DateTime.Now;
        }
        var preview = selectedAchievement is null ? string.Empty : $"{selectedAchievement?.Name}";

        ImGuiEx.TextV($"Select Achievement");
        ImGui.SameLine(120f.Scale());

        ImGuiEx.SetNextItemFullWidth();
        using var combo = ImRaii.Combo("###AchievementSelect", preview);
        if (!combo) return;
        ImGui.Text("Search");
        ImGui.SameLine();
        ImGui.InputText("###AchievementSearch", ref Search, 100);

        if (ImGui.Selectable(string.Empty, selectedAchievement == null))
        {
            selectedAchievement = null;
        }

        foreach (var achv in achvSheet.Where(x => !x.Name.RawString.IsNullOrEmpty() && x.Name.RawString.Contains(Search, StringComparison.CurrentCultureIgnoreCase)))
        {
            ImGui.PushID($"###achievement{achv.RowId}");
            var selected = ImGui.Selectable($"{achv.Name.RawString}", achv.RowId == selectedAchievement?.RowId);

            if (selected)
            {
                Config.Achievements.Add(new Achv { ID = achv.RowId, Name = achv.Name, Description = achvSheet.GetRow(achv.RowId).Description.RawString, Points = achvSheet.GetRow(achv.RowId).Points });
                RequestUpdate(achv.RowId);
                SaveConfig(Config);
            }

            ImGui.PopID();
        }
    }

    private void DrawTracker()
    {
        try
        {
            var copy = Config.Achievements;
            foreach (var achv in copy)
            {
                if (Config.AutoRemoveCompleted && achv.Completed)
                {
                    Config.Achievements.Remove(achv);
                    continue;
                }
                ImGui.Columns(2);
                ImGuiEx.TextV($"[{achv.ID}] {achv.Name}");
                if (ImGui.IsItemHovered()) ImGui.SetTooltip($"[{achv.Points}pts] {achv.Description}");

                ImGui.NextColumn();
                DrawProgressBar((int)achv.CurrentProgress, (int)achv.MaxProgress);
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - iconButtonSize.X - ImGui.GetStyle().WindowPadding.X);
                if (ImGuiComponents.IconButton((int)achv.ID, FontAwesomeIcon.Trash))
                {
                    Config.Achievements.Remove(achv);
                    SaveConfig(Config);
                }
                ImGui.Columns(1);
            }
        }
        catch (Exception e) { e.Log(); }
    }

    // https://github.com/KazWolfe/CollectorsAnxiety/blob/bf48a4b0681e5f70fb67e3b1cb22b4565ecfcc02/CollectorsAnxiety/Util/ImGuiUtil.cs#L10
    private void DrawProgressBar(int progress, int total)
    {
        try
        {
            ImGui.BeginGroup();

            var cursor = ImGui.GetCursorPos();
            var sizeVec = new Vector2(ImGui.GetContentRegionAvail().X - iconButtonSize.X - (ImGui.GetStyle().WindowPadding.X * 2), iconButtonSize.Y);

            var percentage = progress / (float)total;
            var label = string.Format("{0:P} Complete ({1} / {2})", percentage, progress, total);
            var labelSize = ImGui.CalcTextSize(label);

            using var _ = ImRaii.PushColor(ImGuiCol.PlotHistogram, Config.BarColour);
            ImGui.ProgressBar(percentage, sizeVec, "");

            ImGui.SetCursorPos(new Vector2(cursor.X + sizeVec.X - labelSize.X - 4, cursor.Y));
            ImGuiEx.TextV(label);

            ImGui.EndGroup();
        }
        catch (Exception e) { e.Log(); }
    }
}
