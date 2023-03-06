using System.Diagnostics;
using System.Threading;

namespace CSWPF.Helpers;

public static class SDA
{
    public static void StartSDA()
    {
        new Thread(() =>
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = "D:\\Game\\SteamSDA\\",
                FileName = "D:\\Game\\SteamSDA\\Steam Desktop Authenticator.exe",
                Arguments = "",
            };
            Process process1 = new Process()
            {
                StartInfo = startInfo
            };
            process1.Start();
        }).Start();
    }
}