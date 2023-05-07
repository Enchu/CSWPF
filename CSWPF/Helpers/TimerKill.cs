using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using CSWPF.Utils;

namespace CSWPF.Helpers;

public class TimerKill
{
    private static void KillAll()
    {
        try
        {
            foreach (Process process in ((IEnumerable<Process>) Process.GetProcesses()).Where<Process>((Func<Process, bool>) (pr => pr.ProcessName.ToLower().Equals("csgo"))))
                process.Kill();
        }
        catch (Exception ex) { }
        try
        {
            ((IEnumerable<Process>) Process.GetProcesses()).Where<Process>((Func<Process, bool>) 
                (x => x.ProcessName.ToLower().StartsWith("steam"))).ToList<Process>().ForEach((Action<Process>) (x => x.Kill()));
        }
        catch (Exception ex) { }
    }
    
    public static void KillSteam(ulong steamid)
    {
        try
        {
            ((IEnumerable<Process>) Process.GetProcesses()).Where<Process>((Func<Process, bool>) 
                (x => x.ProcessName.ToLower().StartsWith($"steam_{steamid}"))).ToList<Process>().ForEach((Action<Process>) (x => x.Kill()));
        }
        catch (Exception ex) { Msg.ShowError(""+ ex.ToString());}
    }

    public static void OpenSteam(string pathSteam)
    {
        new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                Arguments = ("/C call \"" + pathSteam + "\" steam://open/games")
            }
        }.Start();
    }

    public static void Timer()
    {
        int num = 0;
        TimerCallback tm = new TimerCallback(Count);
        Timer timer = new Timer(tm, num, 0, 14500000);
    }
    private static void Count(object obj)
    {
        KillAll();
    }
}