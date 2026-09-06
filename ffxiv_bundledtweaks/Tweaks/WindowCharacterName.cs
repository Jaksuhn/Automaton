using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ComplexTweaks.Tweaks;

public partial class WindowCharacterName : Tweak {
    public override string Name => "Window Character Name";
    public override string Description => "Adds CharacterName@World to the window title.";

    private const string DefaultTitle = "FINAL FANTASY XIV";
    private IntPtr _handle;

    [LibraryImport("user32.dll", EntryPoint = "SetWindowTextW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowText(IntPtr hwnd, string lpString);

    public override void OnEnable() {
        using var process = Process.GetCurrentProcess();
        _handle = process.MainWindowHandle;

        IClientState.Get().Login += UpdateTitle;
        IClientState.Get().Logout += (_, _) => RestoreTitle();

        if (IClientState.Get().IsLoggedIn)
            UpdateTitle();
    }

    public override void OnDisable() {
        IClientState.Get().Login -= UpdateTitle;
        IClientState.Get().Logout -= (_, _) => RestoreTitle();
        RestoreTitle();
    }

    private void UpdateTitle() {
        if (IPlayerState.Get() is { IsLoaded: true, CharacterName: { IsEmpty: false } name, HomeWorld: { IsValid: true, Value.Name: var worldName } })
            SetTitle($"{name}@{worldName} - FFXIV");
        else
            RestoreTitle();
    }

    private void RestoreTitle() => SetTitle(DefaultTitle);

    private void SetTitle(string title) {
        try {
            SetWindowText(_handle, title);
        }
        catch (Exception ex) {
            Error(ex, $"Failed to set window title to \"{title}\"");
        }
    }
}
