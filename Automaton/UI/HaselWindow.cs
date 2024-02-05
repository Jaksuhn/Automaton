//using System;
//using System.Drawing;
//using System.IO;
//using System.Linq;
//using System.Numerics;
//using System.Reflection;
//using System.Text.RegularExpressions;
//using Automaton.Features;
//using Dalamud.Interface.Internal;
//using Dalamud.Interface.Utility.Raii;
//using Dalamud.Interface.Windowing;
//using ECommons.DalamudServices;
//using ImGuiNET;
//using static System.Net.Mime.MediaTypeNames;

//namespace Automaton.UI;

//public partial class PWindow : Window, IDisposable
//{
//    private const uint SidebarWidth = 250;
//    private const string LogoManifestResource = "Haselfeatures.Assets.Logo.png";

//    private string _selectedfeature = string.Empty;
//    private readonly IDalamudTextureWrap? _logoTextureWrap;
//    private readonly Point _logoSize = new(425, 132);

//    [GeneratedRegex("\\.0$")]
//    private static partial Regex VersionPatchZeroRegex();

//    public PWindow() : base("Haselfeatures")
//    {
//        var width = (SidebarWidth * 3) + ImGui.GetStyle().ItemSpacing.X + ImGui.GetStyle().FramePadding.X * 2;

//        Namespace = "HaselfeaturesConfig";

//        Size = new Vector2(width, 600);
//        SizeConstraints = new()
//        {
//            MinimumSize = new Vector2(width, 600),
//            MaximumSize = new Vector2(4096, 2160)
//        };

//        SizeCondition = ImGuiCond.Always;

//        Flags |= ImGuiWindowFlags.AlwaysAutoResize;
//        Flags |= ImGuiWindowFlags.NoSavedSettings;

//        AllowClickthrough = false;
//        AllowPinning = false;

//        try
//        {
//            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(LogoManifestResource)
//                ?? throw new Exception($"ManifestResource \"{LogoManifestResource}\" not found");

//            using var ms = new MemoryStream();
//            stream.CopyTo(ms);

//            _logoTextureWrap = Svc.PluginInterface.UiBuilder.LoadImage(ms.ToArray());
//        }
//        catch (Exception ex)
//        {
//            Svc.Log.Error(ex, "Error loading logo");
//        }
//    }

//    public void Dispose()
//    {
//        _logoTextureWrap?.Dispose();
//    }

//    private Feature? Selectedfeature => (Feature)P.Features.FirstOrDefault(f => f.Name == _selectedfeature);

//    public override void OnClose()
//    {
//        _selectedfeature = string.Empty;
//        Flags &= ~ImGuiWindowFlags.MenuBar;
//        base.OnClose();
//    }

//    public override void Draw()
//    {
//        DrawSidebar();
//        ImGui.SameLine();
//        DrawConfig();
//    }

//    private void DrawSidebar()
//    {
//        var scale = ImGuiHelpers.GlobalScale;
//        using var child = ImRaii.Child("##Sidebar", new Vector2(SidebarWidth * scale, -1), true);
//        if (!child.Success)
//            return;

//        using var table = ImRaii.Table("##SidebarTable", 2, ImGuiTableFlags.NoSavedSettings);
//        if (!table.Success)
//            return;

//        ImGui.TableSetupColumn("Checkbox", ImGuiTableColumnFlags.WidthFixed);
//        ImGui.TableSetupColumn("feature Name", ImGuiTableColumnFlags.WidthStretch);

//        foreach (var feature in P.Features.OrderBy(t => t.Name))
//        {
//            ImGui.TableNextRow();
//            ImGui.TableNextColumn();

//            var enabled = feature.Enabled;
//            var fixY = false;

//            if (!feature.Ready || feature.Outdated)
//            {
//                var startPos = ImGui.GetCursorPos();
//                var drawList = ImGui.GetWindowDrawList();
//                var pos = ImGui.GetWindowPos() + startPos - new Vector2(0, ImGui.GetScrollY());
//                var frameHeight = ImGui.GetFrameHeight();

//                var size = new Vector2(frameHeight);
//                ImGui.SetCursorPos(startPos);
//                ImGui.Dummy(size);

//                if (ImGui.IsItemHovered())
//                {
//                    var (status, color) = GetfeatureStatus(feature);
//                    using var tooltip = ImRaii.Tooltip();
//                    if (tooltip.Success)
//                    {
//                        using (ImRaii.PushColor(ImGuiCol.Text, color))
//                            ImGui.TextUnformatted(status);
//                    }
//                }

//                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(ImGuiCol.FrameBg), 3f, ImDrawFlags.RoundCornersAll);

//                var pad = frameHeight / 4f;
//                pos += new Vector2(pad);
//                size -= new Vector2(pad) * 2;

//                drawList.PathLineTo(pos);
//                drawList.PathLineTo(pos + size);
//                drawList.PathStroke(Colors.Red, ImDrawFlags.None, frameHeight / 5f * 0.5f);

//                drawList.PathLineTo(pos + new Vector2(0, size.Y));
//                drawList.PathLineTo(pos + new Vector2(size.X, 0));
//                drawList.PathStroke(Colors.Red, ImDrawFlags.None, frameHeight / 5f * 0.5f);

//                fixY = true;
//            }
//            else
//            {
//                if (ImGui.Checkbox($"##Enabled_{feature.Name}", ref enabled))
//                {
//                    if (!enabled)
//                    {
//                        feature.Disable();

//                        if (P.Features.Where(f => f.Enabled).Contains(feature.Name))
//                        {
//                            P.Config.Enabledfeatures.Remove(feature.Name);
//                            P.Config.Save();
//                        }
//                    }
//                    else
//                    {
//                        feature.EnableInternal();

//                        if (!P.Config.Enabledfeatures.Contains(feature.Name))
//                        {
//                            P.Config.Enabledfeatures.Add(feature.Name);
//                            P.Config.Save();
//                        }
//                    }
//                }
//            }

//            ImGui.TableNextColumn();

//            if (fixY)
//            {
//                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);
//            }

//            if (!feature.Ready)
//            {
//                ImGui.PushStyleColor(ImGuiCol.Text, (uint)Colors.Red);
//            }
//            else if (!enabled)
//            {
//                ImGui.PushStyleColor(ImGuiCol.Text, (uint)Colors.Grey);
//            }

//            if (ImGui.Selectable($"{feature.Name}##Selectable_{feature.Name}", _selectedfeature == feature.Name))
//            {
//                Selectedfeature?.OnConfigWindowClose();

//                _selectedfeature = _selectedfeature != feature.Name
//                    ? feature.Name
//                    : string.Empty;
//            }

//            if (!feature.Ready || !enabled)
//            {
//                ImGui.PopStyleColor();
//            }
//        }
//    }

//    private void DrawConfig()
//    {
//        using var child = ImRaii.Child("##Config", new Vector2(-1), true);
//        if (!child.Success)
//            return;

//        var feature = Selectedfeature;
//        if (feature == null)
//        {
//            var cursorPos = ImGui.GetCursorPos();
//            var contentAvail = ImGui.GetContentRegionAvail();

//            if (_logoTextureWrap != null && _logoTextureWrap.ImGuiHandle != 0)
//            {
//                var maxWidth = SidebarWidth * 2 * 0.85f * ImGuiHelpers.GlobalScale;
//                var ratio = maxWidth / _logoSize.X;
//                var scaledLogoSize = new Vector2(_logoSize.X, _logoSize.Y) * ratio;

//                ImGui.SetCursorPos(contentAvail / 2 - scaledLogoSize / 2 + new Vector2(ImGui.GetStyle().ItemSpacing.X, 0));
//                ImGui.Image(_logoTextureWrap.ImGuiHandle, scaledLogoSize);
//            }
//            return;
//        }

//        using var id = ImRaii.PushId(feature.Name);

//        using (ImRaii.PushColor(ImGuiCol.Text, Colors.Gold))
//            ImGui.TextUnformatted(feature.Name);

//        var (status, color) = GetfeatureStatus(feature);

//        var posX = ImGui.GetCursorPosX();
//        var windowX = ImGui.GetContentRegionAvail().X;
//        var textSize = ImGui.CalcTextSize(status);

//        ImGui.SameLine(windowX - textSize.X);

//        using (ImRaii.PushColor(ImGuiCol.Text, color))
//            ImGui.TextUnformatted(status);

//        if (!string.IsNullOrEmpty(feature.Description))
//        {
//            var style = ImGui.GetStyle();
//            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + style.ItemSpacing.Y);
//            ImGui.Separator();
//            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + style.ItemSpacing.Y - 1);
//            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, feature.Description);
//        }

//        if (feature.IncompatibilityWarnings.Any(entry => entry.IsLoaded))
//        {
//            ImGuiUtils.DrawSection(t("Haselfeatures.Config.SectionTitle.IncompatibilityWarning"));
//            Svc.Texture.GetIcon(60073).Draw(24);
//            ImGui.SameLine();
//            var cursorPosX = ImGui.GetCursorPosX();

//            static string getConfigName(string featureName, string configName)
//                => t($"Haselfeatures.Config.IncompatibilityWarning.P.{featureName}.Config.{configName}");

//            if (feature.IncompatibilityWarnings.Length == 1)
//            {
//                var entry = feature.IncompatibilityWarnings[0];
//                var PName = t($"Haselfeatures.Config.IncompatibilityWarning.P.{entry.Name}.Name");

//                if (entry.IsLoaded)
//                {
//                    if (entry.ConfigNames.Length == 0)
//                    {
//                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, t("Haselfeatures.Config.IncompatibilityWarning.Single.P", PName));
//                    }
//                    else if (entry.ConfigNames.Length == 1)
//                    {
//                        var configName = getConfigName(entry.Name, entry.ConfigNames[0]);
//                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, t("Haselfeatures.Config.IncompatibilityWarning.Single.PSetting", configName, PName));
//                    }
//                    else if (entry.ConfigNames.Length > 1)
//                    {
//                        var configNames = entry.ConfigNames.Select((configName) => t($"Haselfeatures.Config.IncompatibilityWarning.P.{entry.Name}.Config.{configName}"));
//                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, t("Haselfeatures.Config.IncompatibilityWarning.Single.PSettings", PName) + $"\n- {string.Join("\n- ", configNames)}");
//                    }
//                }
//            }
//            else if (feature.IncompatibilityWarnings.Length > 1)
//            {
//                ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, t("Haselfeatures.Config.IncompatibilityWarning.Multi.Preface"));

//                foreach (var entry in feature.IncompatibilityWarnings.Where(entry => entry.IsLoaded))
//                {
//                    var PName = t($"Haselfeatures.Config.IncompatibilityWarning.P.{entry.Name}.Name");

//                    if (entry.ConfigNames.Length == 0)
//                    {
//                        ImGui.SetCursorPosX(cursorPosX);
//                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, t("Haselfeatures.Config.IncompatibilityWarning.Multi.P", PName));
//                    }
//                    else if (entry.ConfigNames.Length == 1)
//                    {
//                        ImGui.SetCursorPosX(cursorPosX);
//                        var configName = t($"Haselfeatures.Config.IncompatibilityWarning.P.{entry.Name}.Config.{entry.ConfigNames[0]}");
//                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, t("Haselfeatures.Config.IncompatibilityWarning.Multi.PSetting", configName, PName));
//                    }
//                    else if (entry.ConfigNames.Length > 1)
//                    {
//                        ImGui.SetCursorPosX(cursorPosX);
//                        var configNames = entry.ConfigNames.Select((configName) => t($"Haselfeatures.Config.IncompatibilityWarning.P.{entry.Name}.Config.{configName}"));
//                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, t("Haselfeatures.Config.IncompatibilityWarning.Multi.PSettings", PName) + $"\n    - {string.Join("\n    - ", configNames)}");
//                    }
//                }
//            }
//        }

//        feature.DrawConfig();
//    }

//    private static (string, HaselColor) GetfeatureStatus(Feature feature)
//    {
//        var status = t("Haselfeatures.Config.featureStatus.Unknown");
//        var color = Colors.Grey3;

//        if (!feature.Ready)
//        {
//            status = t("Haselfeatures.Config.featureStatus.InitializationFailed");
//            color = Colors.Red;
//        }
//        else if (feature.Enabled)
//        {
//            status = t("Haselfeatures.Config.featureStatus.Enabled");
//            color = Colors.Green;
//        }
//        else if (!feature.Enabled)
//        {
//            status = t("Haselfeatures.Config.featureStatus.Disabled");
//        }

//        return (status, color);
//    }
//}
