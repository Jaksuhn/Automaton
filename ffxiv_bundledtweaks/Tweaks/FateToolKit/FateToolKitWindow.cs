using clib.ImGuiHelpers;
using clib.Ui;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace ComplexTweaks.Tweaks;

public class FateToolKitWindow : MinimisableWindow {
    private readonly FateToolKit _tweak;
    private bool _showSettings;

    private static readonly PropertyInfo[] _tooltipProperties;

    static FateToolKitWindow() {
        _tooltipProperties = [.. typeof(PublicEvent)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .OrderBy(p => p.Name)];
    }

    public FateToolKitWindow(FateToolKit tweak) : base($"Fate Tracker##{nameof(FateToolKitWindow)}") {
        _tweak = tweak;
        TitleBarButtons.Add(new TitleBarButton {
            Icon = FontAwesomeIcon.Cog,
            Click = _ => _showSettings = !_showSettings,
        });
    }

    protected override Vector2 MinimisedSize => new(700, 90);

    public override bool DrawConditions() => IObjectTable.Get().LocalPlayer.Available;

    protected override void DrawContent(bool minimised) {
        _tweak.SyncRunningState();

        using (var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 6f))
        using (var runButtonColor = ImRaii.PushColor(ImGuiCol.Button, _tweak.Running ? (uint)Colors.Negative : (uint)Colors.Positive)
            .Push(ImGuiCol.ButtonHovered, _tweak.Running ? (uint)Colors.NegativeHover : (uint)Colors.PositiveHover)
            .Push(ImGuiCol.ButtonActive, _tweak.Running ? (uint)Colors.NegativeActive : (uint)Colors.PositiveActive)) {

            if (ImGui.Button(_tweak.Running ? (_tweak.PendingStopWhenSafe ? "Stopping" : "Stop") : "Start")) {
                if (_tweak.Running) {
                    if (ImGui.GetIO().KeyCtrl) {
                        _tweak.PendingStopWhenSafe = true;
                    }
                    else {
                        _tweak.ToggleRunning();
                        NavmeshIPC.Get().Stop();
                    }
                }
                else {
                    _tweak.ToggleRunning();
                }
            }
            ImGui.TooltipOnHover(_tweak.Running, $"Stop. Ctrl+{SeIconChar.MouseLeftClick.ToIconString()} soft stop");

            ImGui.SameLine();
            DrawHeaderChip(
                $"Automation: {(_tweak.Running ? Svc.Automation.Status : "Stopped")}",
                _tweak.Running ? Colors.ChipGold : Colors.ChipMuted,
                Colors.Grey2
            );

            ImGui.SameLine();
            DrawHeaderChip(
                $"State: {_tweak.CurrentState}",
                _tweak.Running && !_tweak.CurrentState.Equals("Idle", StringComparison.OrdinalIgnoreCase) ? Colors.ChipGold : Colors.ChipMuted,
                Colors.Grey2
            );

            ImGui.SameLine();
            DrawHeaderChip($"Completed: {_tweak.CompletedCount}", Colors.ChipInfo, Colors.Grey2);

            if (_tweak.RemainingUntilCompleted is { } remaining && remaining > 0) {
                ImGui.SameLine();
                DrawHeaderChip($"Remaining: {remaining}", Colors.ChipInfo, Colors.Grey2);
            }

            var modeRemaining = _tweak.GetCurrentMode().GetRemainingDisplay(_tweak);
            if (!string.IsNullOrEmpty(modeRemaining)) {
                ImGui.SameLine();
                var (bg, fg) = modeRemaining.Equals("Done", StringComparison.OrdinalIgnoreCase) ? (Colors.ChipMuted, Colors.Grey2) : (Colors.ChipInfo, Colors.Grey2);
                DrawHeaderChip(modeRemaining, bg, fg);
            }

            ImGui.SameLine();
            var style = ImGui.GetStyle();
            var rightButtonWidth = (ImGui.GetFrameHeight() + style.FramePadding.X * 2f) * 2f + style.ItemSpacing.X;
            var leftRightGap = style.ItemSpacing.X;
            var leftContentRight = ImGui.GetItemRectMax().X;
            if (Math.Max(0f, ImGui.GetContentRegionAvail().X - rightButtonWidth) is > 0 and var spacer) {
                ImGui.Dummy(new Vector2(spacer, 0f));
                ImGui.SameLine();
            }
            DrawModeButton();
            ImGui.SameLine();
            using (var _ = ImRaii.Disabled(_tweak.ModeSuppliesSwapZones))
            using (var zoneButtonColor = ImRaii.PushColor(ImGuiCol.Text, _tweak.HasSelectedSwapZones ? (uint)Color.Gold : ImGui.GetColorU32(ImGuiCol.Text))) {
                if (ImGuiComponents.IconButton("###ZoneSelector", FontAwesomeIcon.Globe)) {
                    TerritorySelectWindow.Show(_tweak.SelectedSwapZones, new() {
                        Filter = row => row.IsInUse && row.TerritoryIntendedUse.Value.StructsEnum is TerritoryIntendedUse.Overworld && !row.IsPvpZone,
                        Columns = TerritorySelectColumn.All & ~TerritorySelectColumn.Duty & ~TerritorySelectColumn.IntendedUse,
                    });
                }
            }
            if (_tweak.ModeSuppliesSwapZones)
                ImGui.TooltipOnHover(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled), "Zone list is defined by the current grind mode. Switch to None to select zones manually.");
            else if (_tweak.HasSelectedSwapZones)
                ImGui.TooltipOnHover($"Swap Zones: {_tweak.SelectedSwapZones.Count}");
            else
                ImGui.TooltipOnHover("Swap Zones (uses default swap behaviour if none selected)");

            if (minimised) {
                MinimisedContentWidth = Math.Max(400, leftContentRight - ImGui.GetWindowPos().X + leftRightGap + rightButtonWidth + style.WindowPadding.X * 2);
            }
        }

        if (_showSettings)
            DrawSettings();

        if (minimised)
            return;

        ImGui.SpacedSeparator();

        if (_tweak.GetOrderedFates().ToList() is not { Count: > 0 } fates) {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "No fates match the current filters.");
            return;
        }

        foreach (var (fate, isAvailable) in fates) {
            using var id = ImRaii.PushId($"fate_{fate.Id}");

            var availableWidth = ImGui.GetContentRegionAvail().X;
            var displayName = FormatDisplayName(fate);
            var nameWidth = Math.Min(200f.Scaled(), availableWidth * 0.4f);
            var progressWidth = Math.Max(1f, availableWidth - nameWidth - ImGui.GetStyle().ItemSpacing.X);

            using (var buttonStyle = ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f)))
            using (var color = ImRaii.PushColor(ImGuiCol.Button, 0).Push(ImGuiCol.ButtonHovered, ImGui.GetColorU32(ImGuiCol.ButtonHovered)).Push(ImGuiCol.ButtonActive, ImGui.GetColorU32(ImGuiCol.ButtonActive))) {
                if (fate.HasBonus) {
                    ImGui.Image(ITextureProvider.Get().GetFromGameIcon(new Dalamud.Interface.Textures.GameIconLookup(65001)).GetWrapOrEmpty().Handle, new Vector2(ImGui.IconUnitHeight()));
                    ImGui.SameLine(0f, 0f);
                }

                using (var nameCol = ImRaii.PushColor(ImGuiCol.Text, isAvailable ? (uint)Color.White : Colors.Grey3)) {
                    if (ImGui.Button(displayName, new Vector2(
                        fate.HasBonus
                            ? Math.Max(1f, nameWidth - ImGui.IconUnitWidth())
                            : nameWidth,
                        0
                    ))) {
                        if (NavmeshIPC.Get().IsRunning())
                            NavmeshIPC.Get().Stop();
                        else
                            NavmeshIPC.Get().PathfindAndMoveTo(fate.Position.RandomPoint(fate.Radius * 0.5f).OnMesh(), ICondition.Get()[ConditionFlag.InFlight]);
                    }
                }

                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    _tweak.ToggleBlacklist(fate);

                ImGui.TooltipOnHover(BuildFateTooltip(fate, displayName));
            }

            ImGui.SameLine();

            var percentage = fate.Progress / 100f;
            var progressLabel = $"{fate.Progress}%";

            var cursorPos = ImGui.GetCursorPos();
            var labelSize = ImGui.CalcTextSize(progressLabel);
            var textX = Math.Max(0f, progressWidth - labelSize.X - 4f);

            using (var color = ImRaii.PushColor(ImGuiCol.PlotHistogram, _tweak.Config.BarColour))
                ImGui.ProgressBar(percentage, new Vector2(progressWidth, ImGui.GetFrameHeight()), "");

            ImGui.SetCursorPos(new Vector2(cursorPos.X + textX, cursorPos.Y + (ImGui.GetFrameHeight() - labelSize.Y) * 0.5f));
            ImGui.TextColored(
                ImGui.GetProgressBarTextColor(_tweak.Config.BarColour, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg], percentage, textX, labelSize.X, progressWidth),
                progressLabel
            );

            ImGui.SpacedSeparator();
        }
    }

    private void DrawModeButton() {
        if (ImGuiComponents.IconButton("###GrindMode", FontAwesomeIcon.List))
            ImGui.OpenPopup("###GrindModePopup");
        ImGui.TooltipOnHover($"Grind mode: {_tweak.GetCurrentMode().DisplayName}\nEXPERIMENTAL (didn't get to test non-gemstones)");

        using var popup = ImRaii.Popup("###GrindModePopup");
        if (popup) {
            foreach (var mode in FateGrindModes.All) {
                if (ImGui.Selectable(mode.DisplayName, mode.DisplayName == _tweak.SelectedModeId)) {
                    _tweak.SelectedModeId = mode.DisplayName;
                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }

    private static void DrawHeaderChip(string text, Color background, Color textColor) {
        using var chipColor = ImRaii.PushColor(ImGuiCol.Button, (uint)background)
            .Push(ImGuiCol.ButtonHovered, (uint)background)
            .Push(ImGuiCol.ButtonActive, (uint)background)
            .Push(ImGuiCol.Text, (uint)textColor);
        ImGui.Button(text);
    }

    private unsafe void DrawSettings() {
        ImGui.TextColored(new Vector4(0.8f, 0.8f, 1f, 1f), "Level Filter");
        ImGui.Spacing();
        ImGui.TextWrapped("Skip FATEs outside this level range");
        ImGui.Spacing();

        ImGui.TextV("Min Level:");
        var max = PlayerState.Instance()->MaxLevel;
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f);
        if (ImGui.DragInt("###MinLevel", ref _tweak.Config.MinLevel, 0.1f, 1, max))
            _tweak.Config.MinLevel = Math.Clamp(_tweak.Config.MinLevel, 1, _tweak.Config.MaxLevel);

        ImGui.SameLine();
        ImGui.TextV("Max Level:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f);
        if (ImGui.DragInt("###MaxLevel", ref _tweak.Config.MaxLevel, 0.1f, 1, max))
            _tweak.Config.MaxLevel = Math.Clamp(_tweak.Config.MaxLevel, _tweak.Config.MinLevel, max);

        ImGui.SpacedSeparator();

        ImGui.TextColored(new Vector4(0.8f, 0.8f, 1f, 1f), "Blacklisted Rules");
        ImGui.Spacing();

        foreach (var rule in Enum.GetValues<PublicEvent.FateRule>().Where(r => r != PublicEvent.FateRule.None)) {
            ImGui.CollectionCheckbox(rule.ToString(), rule, _tweak.Config.BlacklistedRules);
        }

        ImGui.SpacedSeparator();

        ImGui.TextColored(new Vector4(0.8f, 0.8f, 1f, 1f), "Priority Order Configuration");
        ImGui.Spacing();
        ImGui.TextWrapped("Configure the order in which fates are prioritized. The order shown here is the order used by AvailableFates when selecting which fate to complete next.");
        ImGui.Spacing();

        ImGui.TextColored(new Vector4(0.8f, 0.8f, 1f, 1f), "Display Name");
        ImGui.Spacing();
        ImGui.TextV("Format:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(400f);
        ImGui.InputText("###DisplayNameFormat", ref _tweak.Config.DisplayNameFormat, 256);
        ImGuiComponents.HelpMarker("Available tokens: {Level}, {Name}, {Id}, {Progress}, {TimeRemaining}, {Distance}, {State}");
        ImGui.Spacing();

        var sortOrder = _tweak.Config.SortOrder.ToList();
        for (var i = 0; i < sortOrder.Count; i++) {
            using var id = ImRaii.PushId($"sort_{i}");
            var item = sortOrder[i];
            var criteria = item.Criteria;

            var handleSize = new Vector2(ImGui.GetFrameHeight());
            ImGui.Button($"##Drag{i}", handleSize);

            ImGui.DragDropSource(i, "FATE_SORT_ITEM"u8, criteria.ToString().Replace("_", " "));
            ImGui.DragDropTarget(i, "FATE_SORT_ITEM"u8, sortOrder.Count, (sourceIndex, insertIndex) => {
                var dragged = sortOrder[sourceIndex];
                sortOrder.RemoveAt(sourceIndex);
                if (sourceIndex < insertIndex)
                    insertIndex--;
                sortOrder.Insert(insertIndex, dragged);
                _tweak.Config.SortOrder = sortOrder;
            });

            ImGui.TooltipOnHover("Drag to change priority order");

            ImGui.SameLine();

            ImGui.SetNextItemWidth(200);
            using (var critCombo = ImRaii.Combo($"###Criteria{i}", criteria.ToString().Replace("_", " "))) {
                if (critCombo) {
                    foreach (var crit in Enum.GetValues<FateSortCriteria>()) {
                        if (ImGui.Selectable(crit.ToString().Replace("_", " "), crit == criteria)) {
                            item.Criteria = crit;
                            _tweak.Config.SortOrder[i] = item;
                        }
                    }
                }
            }

            ImGui.SameLine();
            var arrowIcon = item.Descending ? FontAwesomeIcon.ArrowDown : FontAwesomeIcon.ArrowUp;
            if (ImGuiComponents.IconButton($"###Dir{i}", arrowIcon)) {
                item.Descending = !item.Descending;
                _tweak.Config.SortOrder[i] = item;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(item.Descending ? "Descending (highest first)" : "Ascending (lowest first)");

            ImGui.SameLine();
            if (ImGuiComponents.IconButton($"###Remove{i}", FontAwesomeIcon.Trash)) {
                sortOrder.RemoveAt(i);
                _tweak.Config.SortOrder = sortOrder;
                i--;
                continue;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Remove this sort criteria");
        }

        ImGui.Spacing();
        if (ImGui.Button("Add Sort Criteria"))
            ImGui.OpenPopup("###AddSortCriteria");

        using (var popup = ImRaii.Popup("###AddSortCriteria")) {
            if (popup) {
                foreach (var crit in Enum.GetValues<FateSortCriteria>()) {
                    if (ImGui.Selectable(crit.ToString().Replace("_", " "))) {
                        sortOrder.Add(new FateSortOrder { Criteria = crit, Descending = true });
                        _tweak.Config.SortOrder = sortOrder;
                    }
                }
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Reset to Default")) {
            _tweak.Config.SortOrder =
            [
                new() { Criteria = FateSortCriteria.HasBonusWithTwist, Descending = true },
                    new() { Criteria = FateSortCriteria.Progress, Descending = true },
                    new() { Criteria = FateSortCriteria.HasBonus, Descending = true },
                    new() { Criteria = FateSortCriteria.TimeRemainingUrgent, Descending = false },
                    new() { Criteria = FateSortCriteria.Distance, Descending = false },
            ];
        }

        ImGui.SpacedSeparator();
    }

    private string BuildFateTooltip(PublicEvent fate, string displayName) {
        var sb = new StringBuilder();

        sb.AppendLine($"Display: {displayName}");

        foreach (var prop in _tooltipProperties) {
            object? raw;
            try {
                raw = prop.GetValue(fate);
            }
            catch {
                continue;
            }

            var value = raw switch {
                null => "?",
                float f when prop.Name == nameof(PublicEvent.TimeRemaining) =>
                    f >= 0 ? TimeSpan.FromSeconds(f).ToString(@"mm\:ss") : "∞",
                Vector3 v when prop.Name == nameof(PublicEvent.Position) =>
                    v.ToString(),
                bool b => b ? "True" : "False",
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => raw.ToString() ?? "?"
            };

            sb.AppendLine($"{prop.Name}: {value}");
        }

        var (isEligible, failedConditions) = _tweak.GetFateConditionDetails(fate);
        sb.AppendLine($"Will be automated? {isEligible}");
        if (failedConditions.Count > 0) {
            sb.AppendLine("Blocked by:");
            foreach (var reason in failedConditions)
                sb.AppendLine($" - {reason}");
        }

        return sb.ToString().TrimEnd();
    }

    public string FormatDisplayName(PublicEvent fate) => _tweak.Config.DisplayNameFormat
        .Replace("{Level}", fate.Level.ToString())
        .Replace("{Name}", fate.Name)
        .Replace("{Id}", fate.Id.ToString())
        .Replace("{Progress}", fate.Progress.ToString())
        .Replace("{TimeRemaining}", fate.TimeRemaining >= 0 ? TimeSpan.FromSeconds(fate.TimeRemaining).ToString(@"mm\:ss") : "∞")
        .Replace("{Distance}", IObjectTable.Get().LocalPlayer?.DistanceTo(fate.Position).ToString("F1") ?? "?")
        .Replace("{State}", fate.State.ToString());
}
