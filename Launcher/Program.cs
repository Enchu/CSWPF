﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

public class Program
{
    private static string leadertext1 = " | LEADER #1";
    private static string leadertext2 = " | LEADER #2";

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
      StringBuilder retVal = new StringBuilder((int) byte.MaxValue);
      Program.GetPrivateProfileString(Section, Key, "", retVal, (int) byte.MaxValue, path);
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
        foreach (Process process in ((IEnumerable<Process>) Process.GetProcesses()).Where<Process>((Func<Process, bool>) (pr => pr.ProcessName.ToLower().Equals("steam_" + accid))))
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
        foreach (Process process in ((IEnumerable<Process>) Process.GetProcesses()).Where<Process>((Func<Process, bool>) (pr => pr.ProcessName.ToLower().Equals("csgo"))))
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
        Console.WriteLine("HELLO 2.0!");
        Console.Clear();
        string str1 = args[0];
        string pipeName = args[1];
        string oldValue = args[2];
        string str2 = args[3];
        string str3 = args[4];
        string accid = args[5];
        string str4 = args[6];
        string str5 = args[7];
        string s1 = args[8];
        string s2 = args[9];
        string s3 = args[10];
        string s4 = args[11];
        string str6 = args[12];
        string str7 = args[13];
        int result1;
        int result2;
        if (!int.TryParse(s1, out result1) || !int.TryParse(s2, out result2))
          throw new Exception("Can't parse X or(and) Y position(s)!");
        int result3;
        int result4;
        if (!int.TryParse(s3, out result3) || !int.TryParse(s4, out result4))
          throw new Exception("Can't parse Width or(and) Height!");
        if (!File.Exists(str7 + "\\steam.exe"))
          throw new Exception("Can't find Steam.exe!");
        bool flag = false;
        string path = str7 + "\\steam_" + accid + ".exe";
        string str8 = " | BOT";
        if (str4 == str1)
          str8 = Program.leadertext1;
        if (str5 == str1)
          str8 = Program.leadertext2;
        if (!File.Exists(path))
          throw new Exception("Can't find " + path);
        string wndTitle = "@ LOGIN: " + pipeName + str8;
        ProcessStartInfo processStartInfo1 = new ProcessStartInfo()
        {
          UseShellExecute = false,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          WorkingDirectory = str7,
          FileName = path,
          Arguments = " -login " + pipeName + " " + oldValue + " -nochatui -nofriendsui -silent"
        };
        string str9 = "\"" + processStartInfo1.FileName + "\" " + processStartInfo1.Arguments.Replace(oldValue, "***");
        Process process1 = new Process()
        {
          StartInfo = processStartInfo1
        };
        process1.OutputDataReceived += (DataReceivedEventHandler) ((sender, e) =>
        {
          if (e.Data == null)
            return;
          Console.WriteLine("[PROC_steam]" + e.Data);
        });
        process1.ErrorDataReceived += (DataReceivedEventHandler) ((sender, e) =>
        {
          if (e.Data == null)
            return;
          Console.WriteLine("[PROC_steam]" + e.Data);
        });
        process1.Exited += (EventHandler) ((sender, e) =>
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
          WorkingDirectory = str7,
          FileName = path,
          Arguments = string.Format("-applaunch 730 -language {0} {1} -w {2} -h {3} -x {4} -y {5} -novid -nosound", (object) accid, (object) str2, (object) result3, (object) result4, (object) result1, (object) result2)
        };
        Process process4 = new Process()
        {
          StartInfo = processStartInfo2
        };
        process4.OutputDataReceived += (DataReceivedEventHandler) ((sender, e) =>
        {
          if (e.Data == null)
            return;
          Console.WriteLine("[PROC_cs]" + e.Data);
        });
        process4.ErrorDataReceived += (DataReceivedEventHandler) ((sender, e) =>
        {
          if (e.Data == null)
            return;
          Console.WriteLine("[PROC_cs]" + e.Data);
        });
        process4.Exited += (EventHandler) ((sender, e) =>
        {
          if (!(sender is Process process6))
            return;
          Console.WriteLine("[PROC_cs] exited with code = " + process6.ExitCode.ToString());
        });
        process4.Start();
        string str10 = "\"" + processStartInfo2.FileName + "\" " + processStartInfo2.Arguments;
        using (NamedPipeServerStream pipeServerStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut))
        {
          using (StreamReader streamReader = new StreamReader((Stream) pipeServerStream))
          {
            using (StreamWriter streamWriter = new StreamWriter((Stream) pipeServerStream))
            {
              Console.WriteLine("[SYSTEM] @ ACCOUNT: " + accid + " | LOGIN: " + pipeName + str8);
              Console.Title = "@ ACCOUNT: " + accid + " | LOGIN: " + pipeName + str8;
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