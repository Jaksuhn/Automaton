using Automaton.Debugging;
using ImGuiNET;
using System;
using System.Diagnostics;
using System.Numerics;

namespace Automaton.Features.Debugging;

public unsafe class SliderCanvas : DebugHelper
{
    public override string Name => $"{nameof(SliderCanvas).Replace("Debug", "")} Debugging";

    private readonly Stopwatch sw = new();
    private const int BarSpacing = 4;
    private const int BarCount = 64;


    public override void Draw()
    {
        if (!sw.IsRunning) sw.Restart();
        var tSpace = ImGui.GetContentRegionAvail();
        var size = tSpace.X > tSpace.Y ? tSpace.Y : tSpace.X;

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ((tSpace.X / 2) - (size / 2)));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, 0xFFFFFFFF);

        if (ImGui.BeginChild("sliderLand", new Vector2(size) * 0.8f, true))
        {
            var dl = ImGui.GetForegroundDrawList();
            var space = ImGui.GetContentRegionAvail();
            var barSize = (space.X / BarCount) - BarSpacing;
            var p0 = ImGui.GetCursorScreenPos();
            dl.AddRectFilled(p0 - new Vector2(50), p0 + space + new Vector2(100), 0xFFFFFFFF);
            for (var i = 0; i < BarCount; i++) dl.AddRectFilled(p0 + new Vector2((i * BarSpacing) + (i * barSize), 0), p0 + new Vector2((i * BarSpacing) + ((i + 1) * barSize), space.Y), 0x33555555);
            var t = (float)(sw.Elapsed.TotalSeconds);
            for (var i = 0; i < BarCount; i++)
            {
                var v = Math.Clamp(GetSliderValue(t, i / (float)BarCount, i), 0f, 1f);
                dl.AddRectFilled(p0 + new Vector2((i * BarSpacing) + (i * barSize), 0 + ((1 - v) * space.Y)), p0 + new Vector2((i * BarSpacing) + ((i + 1) * barSize), space.Y), 0xFFEE5500);
                dl.AddCircleFilled(p0 + new Vector2((barSize / 2f) + (i * BarSpacing) + (i * barSize), 0 + ((1 - v) * space.Y)), barSize, 0xFFEE5500);
            }
        }
        ImGui.EndChild();
        ImGui.PopStyleColor();

    }

    public static float GetSliderValue(float t, float x, int i)
    {
        return (float)Doom.GetSliderValue(t, x, i);
    }
}
