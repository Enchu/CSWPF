using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CSWPF.Helpers.Data;

public class InterceptKeys
  {
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 256;
    private const int WM_KEYUP = 257;
    private static InterceptKeys.LowLevelKeyboardProc _proc = new InterceptKeys.LowLevelKeyboardProc(InterceptKeys.HookCallback);
    private static IntPtr _hookID = IntPtr.Zero;

    public static event EventHandler<Keys> KeyPressed;

    public static event EventHandler<Keys> KeyUnpressed;

    public static void SetHook() => InterceptKeys._hookID = InterceptKeys.SetHook(InterceptKeys._proc);

    public static void RemoveHook()
    {
      if (!(InterceptKeys._hookID != IntPtr.Zero))
        return;
      InterceptKeys.UnhookWindowsHookEx(InterceptKeys._hookID);
      InterceptKeys._hookID = IntPtr.Zero;
      InterceptKeys.KeyPressed = (EventHandler<Keys>) null;
      InterceptKeys.KeyUnpressed = (EventHandler<Keys>) null;
    }

    private static IntPtr SetHook(InterceptKeys.LowLevelKeyboardProc proc)
    {
      using (Process currentProcess = Process.GetCurrentProcess())
      {
        using (ProcessModule mainModule = currentProcess.MainModule)
          return InterceptKeys.SetWindowsHookEx(13, proc, InterceptKeys.GetModuleHandle(mainModule.ModuleName), 0U);
      }
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
      if (nCode >= 0 && wParam == (IntPtr) 256)
      {
        Keys e = (Keys) Marshal.ReadInt32(lParam);
        EventHandler<Keys> keyPressed = InterceptKeys.KeyPressed;
        if (keyPressed != null)
          keyPressed((object) null, e);
      }
      if (nCode >= 0 && wParam == (IntPtr) 257)
      {
        Keys e = (Keys) Marshal.ReadInt32(lParam);
        EventHandler<Keys> keyUnpressed = InterceptKeys.KeyUnpressed;
        if (keyUnpressed != null)
          keyUnpressed((object) null, e);
      }
      return InterceptKeys.CallNextHookEx(InterceptKeys._hookID, nCode, wParam, lParam);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
      int idHook,
      InterceptKeys.LowLevelKeyboardProc lpfn,
      IntPtr hMod,
      uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(
      IntPtr hhk,
      int nCode,
      IntPtr wParam,
      IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
  }