using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Automaton.Helpers;

public static class Misc
{
    public static ExcelSheet<Lumina.Excel.GeneratedSheets.Action> Action = null!;
    public static ExcelSheet<AozAction> AozAction = null!;
    public static ExcelSheet<AozActionTransient> AozActionTransient = null!;

    public static uint AozToNormal(uint id) => id == 0 ? 0 : AozAction.GetRow(id)!.Action.Row;

    public static uint NormalToAoz(uint id)
    {
        foreach (var aozAction in AozAction)
        {
            if (aozAction.Action.Row == id) return aozAction.RowId;
        }

        throw new Exception("https://tenor.com/view/8032213");
    }

    public static float IconUnitHeight() => ImGuiHelpers.GetButtonSize(FontAwesomeIcon.Trash.ToIconString()).Y;
    public static float IconUnitWidth() => ImGuiHelpers.GetButtonSize(FontAwesomeIcon.Trash.ToIconString()).X;

    public static bool ApplicationIsActivated()
    {
        var activatedHandle = GetForegroundWindow();
        if (activatedHandle == IntPtr.Zero)
        {
            return false;       // No window is currently activated
        }

        var procId = Process.GetCurrentProcess().Id;
        int activeProcId;
        GetWindowThreadProcessId(activatedHandle, out activeProcId);

        return activeProcId == procId;
    }


    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

    public static unsafe bool IsClickingInGameWorld() =>
        !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow)
        && !ImGui.GetIO().WantCaptureMouse
        && AtkStage.GetSingleton()->RaptureAtkUnitManager->AtkUnitManager.FocusedUnitsList.Count == 0
        && Framework.Instance()->Cursor->ActiveCursorType == 0;
}
