using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;

public class Program
{

    [DllImport("kernel32.dll")]
    private static extern int GetPrivateProfileSection(
      string lpAppName,
      byte[] lpszReturnBuffer,
      int nSize,
      string lpFileName);

    private List<string> GetKeys(string iniFile, string category)
    {
        byte[] numArray = new byte[2048];
        Program.GetPrivateProfileSection(category, numArray, 2048, iniFile);
        string[] strArray = Encoding.ASCII.GetString(numArray).Trim(new char[1]).Split(new char[1]);
        List<string> keys = new List<string>();
        foreach (string str in strArray)
            keys.Add(str.Substring(0, str.IndexOf("=")));
        return keys;
    }

    [DllImport("kernel32")]
    private static extern long WritePrivateProfileString(
      string section,
      string key,
      string val,
      string filePath);

    [DllImport("kernel32")]
    private static extern int GetPrivateProfileString(
      string section,
      string key,
      string def,
      StringBuilder retVal,
      int size,
      string filePath);

    public void IniWriteValue(string Section, string Key, string Value, string path) => Program.WritePrivateProfileString(Section, Key, Value, path);

    public static string IniReadValue(string Section, string Key, string path)
    {
        StringBuilder retVal = new StringBuilder((int)byte.MaxValue);
        Program.GetPrivateProfileString(Section, Key, "", retVal, (int)byte.MaxValue, path);
        return retVal.ToString();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
      IntPtr hWnd,
      IntPtr hWndInsertAfter,
      int X,
      int Y,
      int cx,
      int cy,
      int uFlags);

    public static void SetWindowPosition(Process p, int x, int y)
    {
        IntPtr mainWindowHandle = p.MainWindowHandle;
        if (!(mainWindowHandle != IntPtr.Zero))
            return;
        Program.SetWindowPos(mainWindowHandle, IntPtr.Zero, x, y, 0, 0, 69);
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    private static void Hide() => Program.ShowWindowAsync(Program.GetConsoleWindow(), 2);

    private static void Show() => Program.ShowWindowAsync(Program.GetConsoleWindow(), 1);

    private static void KillSteam(string accid)
    {
        try
        {
            foreach (Process process in ((IEnumerable<Process>)Process.GetProcesses()).Where<Process>((Func<Process, bool>)(pr => pr.ProcessName.ToLower().Equals("steam_" + accid))))
                process.Kill();
        }
        catch (Exception ex)
        {
        }
    }

    private static void KillCSGO(string wndTitle)
    {
        try
        {
            foreach (Process process in ((IEnumerable<Process>)Process.GetProcesses()).Where<Process>((Func<Process, bool>)(pr => pr.ProcessName.ToLower().Equals("csgo"))))
            {
                if (process.MainWindowTitle.Contains(wndTitle))
                    process.Kill();
            }
        }
        catch (Exception ex)
        {
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowPlacement(IntPtr hWnd, ref Program.WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPlacement(IntPtr hWnd, ref Program.WINDOWPLACEMENT lpwndpl);

    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();
            string loginValue = args[0];
            string passwordValue = args[1];
            string str2 = args[2];
            string accid = args[3];
            string xValue = args[4];
            string yValue = args[5];
            string widthValue = args[6];
            string heightValue = args[7];
            string steamPath = @"D:\Steam\";
            int result1;
            int result2;
            if (!int.TryParse(xValue, out result1) || !int.TryParse(yValue, out result2))
                throw new Exception("Can't parse X or(and) Y position(s)!");
            int result3;
            int result4;
            if (!int.TryParse(widthValue, out result3) || !int.TryParse(heightValue, out result4))
                throw new Exception("Can't parse widthValue or(and) Height!");
            if (!File.Exists(steamPath + "steam.exe"))
                throw new Exception("Can't find Steam.exe!");
            bool flag = false;
            string path = steamPath + "steam_" + accid + ".exe";
            if (!File.Exists(path))
                throw new Exception("Can't find " + path);
            string wndTitle = "@ LOGIN: " + loginValue;
            ProcessStartInfo processStartInfo1 = new ProcessStartInfo()
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = steamPath,
                FileName = path,
                Arguments = " -login " + loginValue + " " + passwordValue + " -nochatui -nofriendsui -silent"
            };
            Process process1 = new Process()
            {
                StartInfo = processStartInfo1
            };
            process1.OutputDataReceived += (DataReceivedEventHandler)((sender, e) =>
            {
                if (e.Data == null)
                    return;
                Console.WriteLine("[PROC_steam]" + e.Data);
            });
            process1.ErrorDataReceived += (DataReceivedEventHandler)((sender, e) =>
            {
                if (e.Data == null)
                    return;
                Console.WriteLine("[PROC_steam]" + e.Data);
            });
            process1.Exited += (EventHandler)((sender, e) =>
            {
                if (!(sender is Process process3))
                    return;
                Console.WriteLine("[PROC_steam] exited with code = " + process3.ExitCode.ToString());
            });
            process1.Start();
            process1.BeginErrorReadLine();
            process1.BeginOutputReadLine();
            Thread.Sleep(6000);
            ProcessStartInfo processStartInfo2 = new ProcessStartInfo()
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = steamPath,
                FileName = path,
                Arguments = string.Format("-applaunch 730 -language {0} {1} -w {2} -h {3} -x {4} -y {5} -novid -nosound", (object)accid, (object)str2, (object)result3, (object)result4, (object)result1, (object)result2)
            };
            Process process4 = new Process()
            {
                StartInfo = processStartInfo2
            };
            process4.OutputDataReceived += (DataReceivedEventHandler)((sender, e) =>
            {
                if (e.Data == null)
                    return;
                Console.WriteLine("[PROC_cs]" + e.Data);
            });
            process4.ErrorDataReceived += (DataReceivedEventHandler)((sender, e) =>
            {
                if (e.Data == null)
                    return;
                Console.WriteLine("[PROC_cs]" + e.Data);
            });
            process4.Exited += (EventHandler)((sender, e) =>
            {
                if (!(sender is Process process6))
                    return;
                Console.WriteLine("[PROC_cs] exited with code = " + process6.ExitCode.ToString());
            });
            process4.Start();
            string str10 = "\"" + processStartInfo2.FileName + "\" " + processStartInfo2.Arguments;
            using (NamedPipeServerStream pipeServerStream = new NamedPipeServerStream(loginValue, PipeDirection.InOut))
            {
                using (StreamReader streamReader = new StreamReader((Stream)pipeServerStream))
                {
                    using (StreamWriter streamWriter = new StreamWriter((Stream)pipeServerStream))
                    {
                        Console.WriteLine("[SYSTEM] @ ACCOUNT: " + accid + " | LOGIN: " + loginValue);
                        Console.Title = "@ ACCOUNT: " + accid + " | LOGIN: " + loginValue;
                        Console.WriteLine("[SYSTEM] SERVER IS RUNNING..");
                        if (result1 > 0 || result2 > 0)
                            Program.SetWindowPosition(Process.GetCurrentProcess(), result1, result2);
                        label_35:
                        try
                        {
                            pipeServerStream.WaitForConnection();
                            streamWriter.AutoFlush = true;
                            string str11;
                            while ((str11 = streamReader.ReadLine()) != null)
                            {
                                switch (str11)
                                {
                                    case "clear":
                                        if (!flag)
                                        {
                                            Console.Clear();
                                            Console.WriteLine("[CLIENT] REQUEST: CLEAR CONSOLE");
                                            break;
                                        }
                                        continue;
                                    case "hide":
                                        if (!flag)
                                        {
                                            Console.WriteLine("[CLIENT] REQUEST: HIDE CONSOLE");
                                            flag = true;
                                            Program.Hide();
                                            break;
                                        }
                                        continue;
                                    case "kill":
                                        Console.WriteLine("[CLIENT] REQUEST: CLOSE ACCOUNT");
                                        Program.KillCSGO(wndTitle);
                                        Program.KillSteam(accid);
                                        Environment.Exit(0);
                                        break;
                                    case "quit":
                                        Console.WriteLine("[CLIENT] REQUEST: STOP SERVER");
                                        Console.WriteLine("[SYSTEM] STOPPING SERVER...");
                                        Program.KillCSGO(wndTitle);
                                        Program.KillSteam(accid);
                                        Environment.Exit(0);
                                        break;
                                    case "restart":
                                        Console.WriteLine("[CLIENT] REQUEST: RESTART ACCOUNT");
                                        Program.KillCSGO(wndTitle);
                                        Program.KillSteam(accid);
                                        Thread.Sleep(5555);
                                        new Process()
                                        {
                                            StartInfo = processStartInfo1
                                        }.Start();
                                        break;
                                    case "show":
                                        if (flag)
                                        {
                                            Console.WriteLine("[CLIENT] REQUEST: SHOW CONSOLE");
                                            flag = false;
                                            Program.Show();
                                            break;
                                        }
                                        continue;
                                    case "start":
                                        Console.WriteLine("[CLIENT] REQUEST: START ACCOUNT");
                                        new Process()
                                        {
                                            StartInfo = processStartInfo1
                                        }.Start();
                                        break;
                                    case "steam":
                                        Console.WriteLine("[CLIENT] REQUEST: OPEN STEAM");
                                        new Process()
                                        {
                                            StartInfo = new ProcessStartInfo()
                                            {
                                                WindowStyle = ProcessWindowStyle.Hidden,
                                                FileName = "cmd.exe",
                                                Arguments = ("/C call \"" + path + "\" steam://open/games")
                                            }
                                        }.Start();
                                        break;
                                    default:
                                        Console.WriteLine("[SYSTEM] REQUESTED UNKNOWN COMMAND (ERROR CODE: 0)");
                                        break;
                                }
                                streamReader.DiscardBufferedData();
                            }
                            goto label_35;
                        }
                        finally
                        {
                            pipeServerStream.Disconnect();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: " + ex.Message);
            Console.ReadKey();
            Environment.ExitCode = 1;
        }
    }

    private struct POINTAPI
    {
        public int x;
        public int y;
    }

    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    private struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public int showCmd;
        public Program.POINTAPI ptMinPosition;
        public Program.POINTAPI ptMaxPosition;
        public Program.RECT rcNormalPosition;
    }
}