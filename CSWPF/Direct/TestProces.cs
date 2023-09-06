using System.Diagnostics;
using System.Linq;
using CSWPF.Directory.Assist;

namespace CSWPF.Directory;

public class TestProces
{
    public static void GetProcess()
    {
        foreach (var process in Process.GetProcesses().Where(p => p.ProcessName.ToLower().Contains($"steam")))
        {
            WinApi.SetForegroundWindow(process.MainWindowHandle);
        }
    }
}