using CSGSI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CSWPF.Directory.Assist;
using CSWPF.Directory.Models;
using CSWPF.Helpers;
using CSWPF.Utils;

namespace CSWPF.Directory;

public class AccountHelper
  {
    private static GameStateListener gsl;
    private static Regex pattern_login = new Regex("\\ LOGIN: (?<val>.*?)\\ ", RegexOptions.Compiled | RegexOptions.Singleline);


    public static List<Tuple<User, Process>> ActiveProcList { get; set; } = new List<Tuple<User, Process>>();
    public static List<Tuple<User, Process>> ActiveProcListUser { get; set; } = new List<Tuple<User, Process>>();
    public static List<Tuple<User, Process>> ActiveSteamList { get; set; } = new List<Tuple<User, Process>>();

    public static bool CheckExistsAndRunWithoutSandbox(User account)
    {
      string str1 = Options.G.StreamPath + "\\";
      string str2 = Options.G.CSGOPath + "\\";
      string path = str1 + "\\Steam.exe";
      if (string.IsNullOrEmpty(str1))
        throw new Exception("Укажите папку Steam");
      if (!File.Exists(path))
        throw new Exception("Не установлен Steam или указана не верная папка Steam");
      if (account == null || string.IsNullOrEmpty(account.Login) || string.IsNullOrEmpty(account.Password))
        throw new Exception("Не задан логин или пароль для аккаунта");
      string lower = account.Login.ToLower();
      string password = account.Password;
      string str3 = "";
      try
      {
        using (FileStream fileStream = new FileStream(str1 + "\\config\\loginusers.vdf", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
          using (StreamReader streamReader = new StreamReader((Stream) fileStream))
            str3 = Regex.Match(streamReader.ReadToEnd(), "AccountName\"\t\t\"" + lower + "([^\n]+)").Groups[0].Value;
        }
      }
      catch
      {
      }
      if (!string.IsNullOrEmpty(str3))
        return false;
      ProcessStartInfo processStartInfo = new ProcessStartInfo()
      {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        WorkingDirectory = str1,
        FileName = path,
        Arguments = " -login " + lower + " " + password
      };
      Process process = new Process()
      {
        StartInfo = processStartInfo
      };
      process.Start();
      for (int index = 0; index < 20; ++index)
      {
        Thread.Sleep(1000);
        if (process.HasExited)
          break;
      }
      try
      {
        process.Kill();
      }
      catch
      {
      }
      return true;
    }
    

    public static void StopGameStateListener() => AccountHelper.gsl?.Stop();
    

    public static Process GetProcess(User account) => AccountHelper.ActiveProcList.FirstOrDefault<Tuple<User, Process>>((Func<Tuple<User, Process>, bool>) (o => o.Item1.Login.Equals(account.Login)))?.Item2;
    public static Process GetProcessSteam(User account) => AccountHelper.ActiveSteamList.FirstOrDefault<Tuple<User, Process>>((Func<Tuple<User, Process>, bool>) (o => o.Item1.Login.Equals(account.Login)))?.Item2;
    
    public static Process GetProcessNot() => Process.GetProcesses().FirstOrDefault(p => p.MainWindowTitle == "Войти в Steam");
    public static Process GetProcessCS(string login) => Process.GetProcesses().FirstOrDefault(p => p.MainWindowTitle.Contains($"{login}"));

    public static void OpenFolder(string folder) => new Process()
    {
      StartInfo = new ProcessStartInfo()
      {
        FileName = "explorer.exe",
        Arguments = folder
      }
    }.Start();

    public static string GetCheckedPath(string path)
    {
      string checkedPath = path + "\\";
      if (checkedPath[checkedPath.Length - 1].Equals('\\'))
        checkedPath = checkedPath.Substring(0, checkedPath.Length - 1);
      return checkedPath;
    }


    

    public static void KillAll()
    {
      try
      {
        foreach (Process process in ((IEnumerable<Process>) Process.GetProcesses()).Where<Process>((Func<Process, bool>) (pr => pr.ProcessName.ToLower().Equals("csgo"))))
          process.Kill();
      }
      catch (Exception ex){}
      try
      {
        ((IEnumerable<Process>) Process.GetProcesses()).Where<Process>((Func<Process, bool>) (x => x.ProcessName.ToLower().StartsWith("steam"))).ToList<Process>().ForEach((Action<Process>) (x => x.Kill()));
      }
      catch (Exception ex){}
      try
      {
        foreach (Process process in ((IEnumerable<Process>) Process.GetProcesses()).Where<Process>((Func<Process, bool>) (pr => pr.ProcessName.ToLower().Equals("launcher"))))
          process.Kill();
      }
      catch (Exception ex){}
    }

    public static void Pipe(string name, string command)
    {
      using (NamedPipeClientStream pipeClientStream = new NamedPipeClientStream(".", name, PipeDirection.InOut))
      {
        using (StreamWriter streamWriter = new StreamWriter((Stream) pipeClientStream))
        {
          pipeClientStream.Connect();
          streamWriter.Write(command);
          streamWriter.Dispose();
          pipeClientStream.Dispose();
        }
      }
    }

    public static bool IsNamedPipeExist(string pipeName)
    {
      try
      {
        int timeout = 0;
        if (!WinApi.WaitNamedPipe(Path.GetFullPath(string.Format("\\\\.\\pipe\\{0}", (object) pipeName)), timeout))
        {
          switch (Marshal.GetLastWin32Error())
          {
            case 0:
              return false;
            case 2:
              return false;
          }
        }
        return true;
      }
      catch (Exception ex)
      {
        return false;
      }
    }

    private static void copyDirectory(string strSource, string strDestination)
    {
      System.IO.Directory.CreateDirectory(strDestination);
      DirectoryInfo directoryInfo = new DirectoryInfo(strSource);
      foreach (FileInfo file in directoryInfo.GetFiles())
        file.CopyTo(Path.Combine(strDestination, file.Name), true);
      foreach (DirectoryInfo directory in directoryInfo.GetDirectories())
        AccountHelper.copyDirectory(Path.Combine(strSource, directory.Name), Path.Combine(strDestination, directory.Name));
    }

    public static void lineChanger(string newText, string fileName, int line_to_edit)
    {
      string[] contents = File.ReadAllLines(fileName);
      contents[line_to_edit - 1] = newText;
      File.WriteAllLines(fileName, contents);
    }

    public static void SetFileReadAccess(string FileName, bool SetReadOnly) => new FileInfo(FileName).IsReadOnly = SetReadOnly;

    public static bool IsFileReadOnly(string FileName) => new FileInfo(FileName).IsReadOnly;

    public static void DeleteDirectory(string path)
    {
      foreach (string directory in System.IO.Directory.GetDirectories(path))
        AccountHelper.DeleteDirectory(directory);
      try
      {
        System.IO.Directory.Delete(path, true);
      }
      catch (IOException ex)
      {
        System.IO.Directory.Delete(path, true);
      }
      catch (UnauthorizedAccessException ex)
      {
        System.IO.Directory.Delete(path, true);
      }
    }
  }