using Dalamud.Hooking;
using System;

namespace Automaton.Helpers;

public interface IHookWrapper : IDisposable
{
    public void Enable();
    public void Disable();

    public bool IsEnabled { get; }
    public bool IsDisposed { get; }
}

public class HookWrapper<T> : IHookWrapper where T : Delegate
{

    private readonly Hook<T> wrappedHook;

    private bool disposed;

    public HookWrapper(Hook<T> hook) => wrappedHook = hook;

    public void Enable()
    {
        if (disposed) return;
        wrappedHook?.Enable();
    }

    public void Disable()
    {
        if (disposed) return;
        wrappedHook?.Disable();
    }

    public void Dispose()
    {
        Disable();
        disposed = true;
        wrappedHook?.Dispose();
    }

    public nint Address => wrappedHook.Address;
    public T Original => wrappedHook.Original;
    public bool IsEnabled => wrappedHook.IsEnabled;
    public bool IsDisposed => wrappedHook.IsDisposed;
}
