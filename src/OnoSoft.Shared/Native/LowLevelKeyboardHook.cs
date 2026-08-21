using System;
using System.Runtime.InteropServices;

namespace OnoSoft.Shared.Native;

/// <summary>
/// システム全体のキー入力を監視するだけの低レベルキーボードフック(WH_KEYBOARD_LL)。
/// 常に CallNextHookEx を呼んで素通しするため、キー入力を一切ブロック/変更しない
/// (＝どのアプリの通常のショートカット・入力にも影響を与えない)。
///
/// RegisterHotKey では「特定のキーの組み合わせが押された」ことしか分からず、
/// 「修飾キーだけを押して他のキーに触れずに離した」といったジェスチャーは検知できない。
/// このフックはキーの押下/解放をすべて観測できるので、そうしたジェスチャーの判定に使う。
/// </summary>
public sealed class LowLevelKeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYUP = 0x0105;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    // フック用デリゲートはGCされるとコールバックがクラッシュするため、フィールドで保持し続ける。
    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;

    /// <summary>キーが押された(vkCodeを渡す)。フックを設定したスレッド(通常はUIスレッド)で同期的に呼ばれる。</summary>
    public event Action<int>? KeyDown;

    /// <summary>キーが離された(vkCodeを渡す)。</summary>
    public event Action<int>? KeyUp;

    public LowLevelKeyboardHook()
    {
        _proc = HookCallback;
    }

    public void Start()
    {
        if (_hookId != IntPtr.Zero) return;

        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var vkCode = Marshal.ReadInt32(lParam);
            var msg = wParam.ToInt32();

            if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                KeyDown?.Invoke(vkCode);
            else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
                KeyUp?.Invoke(vkCode);
        }

        // 必ず次のフックへ渡す。ここで止める(0を返す)とキー入力自体を潰してしまうため絶対にしない。
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}
