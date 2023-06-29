using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using CSWPF.Directory;
using CSWPF.Utils;
using SteamKit2;
using Point = System.Drawing.Point;

namespace CSWPF.Helpers;

public static class SteamCode
{
    private static int defaulttime = 300;
    public const int WM_CHAR = 0x102;
    [DllImport("user32.dll")]
    public static extern void SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    
    public static async Task SteamCodeEnter(User user)
    {
        await Task.Delay(15000);
        IEnumerable<Process> SteamProcesses = Process.GetProcesses().Where(pr => pr.ProcessName.ToLower().Contains($"steam_{user.SteamID}"));
        string code = await TwoFactory.GenerateSteamGuardCode(TwoFactory.GetSteamTime(), user.SharedSecret);
        await Task.Delay(defaulttime);
        
        SetForegroundWindow(SteamProcesses.First().MainWindowHandle);
        await Task.Delay(defaulttime);
        foreach (char item in code)
        {
            SendMessage(SteamProcesses.First().MainWindowHandle, WM_CHAR, new IntPtr((Int32)item), (IntPtr)0);
            await Task.Delay(defaulttime);
        }
        SendKeys.SendWait("{ENTER}");
    }

    public static async Task<string> SteamCodeCreate(User user)
    {
        string code = await TwoFactory.GenerateSteamGuardCode(TwoFactory.GetSteamTime(), user.SharedSecret);
        return code;
    }
}