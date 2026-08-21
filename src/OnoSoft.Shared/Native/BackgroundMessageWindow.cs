using System;
using System.Collections.Generic;
using System.Windows.Interop;

namespace OnoSoft.Shared.Native;

/// <summary>
/// A message-only native window used to receive WM_CLIPBOARDUPDATE and WM_HOTKEY
/// without showing any visible UI. Shared by every OnoSoft app that needs to react
/// to clipboard changes and/or a global hotkey.
/// </summary>
public sealed class BackgroundMessageWindow : IDisposable
{
    private static readonly IntPtr HwndMessage = new(-3);

    private readonly HwndSource _hwndSource;
    private readonly HashSet<int> _registeredHotkeyIds = new();
    private bool _clipboardListenerRegistered;

    /// <summary>Raised whenever the system clipboard content changes.</summary>
    public event Action? ClipboardChanged;

    /// <summary>Raised with the hotkey id whenever a registered hotkey is pressed.</summary>
    public event Action<int>? HotkeyPressed;

    public IntPtr Handle => _hwndSource.Handle;

    public BackgroundMessageWindow(string name = "OnoSoftMessageWindow")
    {
        var parameters = new HwndSourceParameters(name)
        {
            ParentWindow = HwndMessage,
            WindowStyle = 0,
            Width = 0,
            Height = 0
        };
        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
    }

    public void StartClipboardListener()
    {
        _clipboardListenerRegistered = NativeMethods.AddClipboardFormatListener(Handle);
    }

    /// <summary>Registers a global hotkey. Returns false if another app already owns the combination.</summary>
    public bool RegisterHotkey(int id, uint modifiers, uint virtualKey)
    {
        var ok = NativeMethods.RegisterHotKey(Handle, id, modifiers, virtualKey);
        if (ok) _registeredHotkeyIds.Add(id);
        return ok;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case NativeMethods.WM_CLIPBOARDUPDATE:
                ClipboardChanged?.Invoke();
                break;
            case NativeMethods.WM_HOTKEY:
                HotkeyPressed?.Invoke(wParam.ToInt32());
                break;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_clipboardListenerRegistered)
            NativeMethods.RemoveClipboardFormatListener(Handle);
        foreach (var id in _registeredHotkeyIds)
            NativeMethods.UnregisterHotKey(Handle, id);
        _hwndSource.RemoveHook(WndProc);
        _hwndSource.Dispose();
    }
}
