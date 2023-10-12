using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Utility;
using ECommons;
using ECommons.ImGuiMethods;
using ImGuiNET;
using Automaton.Features;
using Automaton.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Automaton.UI;

internal class MainWindow : Window
{
    public OpenWindow OpenWindow { get; private set; } = OpenWindow.None;

    public MainWindow() : base($"{Name} {P.GetType().Assembly.GetName().Version}###{Name}")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public static void Dispose()
    {

    }

    private string searchString = string.Empty;
    private List<BaseFeature> FilteredFeatures = new();
    private bool hornybonk;

    public override void Draw()
    {
        var region = ImGui.GetContentRegionAvail();
        var itemSpacing = ImGui.GetStyle().ItemSpacing;

        var topLeftSideHeight = region.Y;

        if (ImGui.BeginTable($"${Name}TableContainer", 2, ImGuiTableFlags.Resizable))
        {
            try
            {
                ImGui.TableSetupColumn($"###LeftColumn", ImGuiTableColumnFlags.WidthFixed, ImGui.GetWindowWidth() / 2);
                ImGui.TableNextColumn();

                var regionSize = ImGui.GetContentRegionAvail();
                ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));
                if (ImGui.BeginChild($"###{Name}Left", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
                {
                    foreach (var window in Enum.GetValues(typeof(OpenWindow)))
                    {
                        if ((OpenWindow)window == OpenWindow.None) continue;

                        if (ImGui.Selectable($"{window}", OpenWindow == (OpenWindow)window))
                        {
                            OpenWindow = (OpenWindow)window;
                        }
                    }

                    ImGui.Spacing();

                    ImGui.SetCursorPosY(ImGui.GetContentRegionMax().Y - 45f);
                    ImGuiEx.ImGuiLineCentered("###Search", () => { ImGui.Text($"Search"); ImGuiComponents.HelpMarker("Searches feature names and descriptions for a given word or phrase."); });
                    ImGuiEx.SetNextItemFullWidth();
                    if (ImGui.InputText("###FeatureSearch", ref searchString, 500))
                    {
                        if (searchString.Equals("ERP", StringComparison.CurrentCultureIgnoreCase) && !hornybonk)
                        {
                            hornybonk = true;
                            Util.OpenLink("https://www.youtube.com/watch?v=oO-gc3Lh-oI");
                        }
                        else
                        {
                            hornybonk = false;
                        }
                        FilteredFeatures.Clear();
                        if (searchString.Length > 0)
                        {
                            foreach (var feature in P.Features)
                            {
                                if (feature.FeatureType == FeatureType.Commands || feature.FeatureType == FeatureType.Disabled) continue;

                                if (feature.Description.Contains(searchString, StringComparison.CurrentCultureIgnoreCase) ||
                                    feature.Name.Contains(searchString, StringComparison.CurrentCultureIgnoreCase))
                                    FilteredFeatures.Add(feature);
                            }
                        }
                    }

                }
                ImGui.EndChild();
                ImGui.PopStyleVar();
                ImGui.TableNextColumn();
                if (ImGui.BeginChild($"###{Name}Right", Vector2.Zero, false, (false ? ImGuiWindowFlags.AlwaysVerticalScrollbar : ImGuiWindowFlags.None) | ImGuiWindowFlags.NoDecoration))
                {
                    if (FilteredFeatures.Count() > 0)
                    {
                        DrawFeatures(FilteredFeatures.ToArray());
                    }
                    else
                    {
                        switch (OpenWindow)
                        {
                            case OpenWindow.Actions:
                                DrawFeatures(P.Features.Where(x => x.FeatureType == FeatureType.Actions && (!x.isDebug || Config.showDebugFeatures)).ToArray());
                                break;
                            case OpenWindow.UI:
                                DrawFeatures(P.Features.Where(x => x.FeatureType == FeatureType.UI && (!x.isDebug || Config.showDebugFeatures)).ToArray());
                                break;
                            case OpenWindow.Other:
                                DrawFeatures(P.Features.Where(x => x.FeatureType == FeatureType.Other && (!x.isDebug || Config.showDebugFeatures)).ToArray());
                                break;
                            case OpenWindow.Targets:
                                DrawFeatures(P.Features.Where(x => x.FeatureType == FeatureType.Targeting && (!x.isDebug || Config.showDebugFeatures)).ToArray());
                                break;
                            case OpenWindow.Chat:
                                DrawFeatures(P.Features.Where(x => x.FeatureType == FeatureType.ChatFeature && (!x.isDebug || Config.showDebugFeatures)).ToArray());
                                break;
                            case OpenWindow.Achievements:
                                DrawFeatures(P.Features.Where(x => x.FeatureType == FeatureType.Achievements && (!x.isDebug || Config.showDebugFeatures)).ToArray());
                                break;
                            case OpenWindow.Commands:
                                DrawCommands(P.Features.Where(x => x.FeatureType == FeatureType.Commands && (!x.isDebug || Config.showDebugFeatures)).ToArray());
                                break;
                        }
                    }
                }
                ImGui.EndChild();
            }
            catch(Exception ex)
            {
                ex.Log();
                ImGui.EndTable();
            }
            ImGui.EndTable();
        }
    }

    private static void DrawCommands(BaseFeature[] features)
    {
        if (features == null || !features.Any() || features.Length == 0) return;
        ImGuiEx.ImGuiLineCentered($"featureHeader{features.First().FeatureType}", () => ImGui.Text($"{features.First().FeatureType}"));
        ImGui.Separator();

        if (ImGui.BeginTable("###CommandsTable", 5, ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Command");
            ImGui.TableSetupColumn("Parameters");
            ImGui.TableSetupColumn("Description");
            ImGui.TableSetupColumn("Aliases");

            ImGui.TableHeadersRow();
            foreach (var feature in features.Cast<CommandFeature>())
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextWrapped(feature.Name);
                ImGui.TableNextColumn();
                ImGui.TextWrapped(feature.Command);
                ImGui.TableNextColumn();
                ImGui.TextWrapped(string.Join(", ", feature.Parameters));
                ImGui.TableNextColumn();
                ImGui.TextWrapped($"{feature.Description}");
                ImGui.TableNextColumn();
                ImGui.TextWrapped($"{string.Join(", ", feature.Alias)}");

            }

            ImGui.EndTable();
        }
    }

    private void DrawFeatures(IEnumerable<BaseFeature> features)
    {
        if (features == null || !features.Any() || !features.Any()) return;

        ImGuiEx.ImGuiLineCentered($"featureHeader{features.First().FeatureType}", () =>
        {
            if (FilteredFeatures.Count > 0)
            {
                ImGui.Text($"Search Results");
            }
            else
                ImGui.Text($"{features.First().FeatureType}");
        });
        ImGui.Separator();

        foreach (var feature in features)
        {
            var enabled = feature.Enabled;
            if (ImGui.Checkbox($"###{feature.Name}", ref enabled))
            {
                if (enabled)
                {
                    try
                    {
                        feature.Enable();
                        if (feature.Enabled)
                        {
                            Config.EnabledFeatures.Add(feature.GetType().Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Error(ex, $"Failed to enabled {feature.Name}");
                    }
                }
                else
                {
                    try
                    {
                        feature.Disable();
                        Config.EnabledFeatures.RemoveAll(x => x == feature.GetType().Name);

                    }
                    catch (Exception ex)
                    {
                        PluginLog.Error(ex, $"Failed to enabled {feature.Name}");
                    }
                }
                Config.Save();
            }
            ImGui.SameLine();
            feature.DrawConfig(ref enabled);
            ImGui.Spacing();
            ImGui.TextWrapped($"{feature.Description}");

            ImGui.Separator();
        }
    }
}

public enum OpenWindow
{
    None,
    Actions,
    UI,
    Targets,
    Chat,
    Other,
    Achievements,
    Commands
}
