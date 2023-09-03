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
using CSWPF.Boost.Models;
using CSWPF.Directory.Assist;
using CSWPF.Directory.Models;
using CSWPF.Helpers;
using CSWPF.Utils;

namespace CSWPF.Directory;

public class AccountHelper
  {
    private static GameStateListener gsl;
    private static Regex pattern_login = new Regex("\\ LOGIN: (?<val>.*?)\\ ", RegexOptions.Compiled | RegexOptions.Singleline);

    public static User MoveAccount { get; set; } = (User) null;

    public static event EventHandler<User> AccountStatusChanged;

    public static event EventHandler SwitchOffAutoButtons;

    public static event EventHandler<string> GameStopwatch;

    public static event EventHandler<string> GameTime;

    public static event EventHandler<Score> ScoreChanged;

    public static event EventHandler WaitForAccept;

    public static event EventHandler AcceptCancelled;

    public static event EventHandler Accepted;

    public static event EventHandler CyclesStarted;

    public static event EventHandler<bool> CyclesEnded;

    public static void SwitchOffAutoButtonsHandler(Lobby lobby)
    {
      EventHandler switchOffAutoButtons = AccountHelper.SwitchOffAutoButtons;
      if (switchOffAutoButtons == null)
        return;
      switchOffAutoButtons((object) lobby, EventArgs.Empty);
    }

    public static void GameStopwatchHandler(Lobby lobby, string time)
    {
      EventHandler<string> gameStopwatch = AccountHelper.GameStopwatch;
      if (gameStopwatch == null)
        return;
      gameStopwatch((object) lobby, time);
    }

    public static void GameTimeHandler(Lobby lobby, string time)
    {
      EventHandler<string> gameTime = AccountHelper.GameTime;
      if (gameTime == null)
        return;
      gameTime((object) lobby, time);
    }

    public static void ScoreChangedHandler(Lobby lobby, Score score)
    {
      EventHandler<Score> scoreChanged = AccountHelper.ScoreChanged;
      if (scoreChanged == null)
        return;
      scoreChanged((object) lobby, score);
    }

    public static void WaitForAcceptHandler(Lobby lobby)
    {
      EventHandler waitForAccept = AccountHelper.WaitForAccept;
      if (waitForAccept == null)
        return;
      waitForAccept((object) lobby, EventArgs.Empty);
    }

    public static void AcceptCancelledHandler(Lobby lobby)
    {
      EventHandler acceptCancelled = AccountHelper.AcceptCancelled;
      if (acceptCancelled == null)
        return;
      acceptCancelled((object) lobby, EventArgs.Empty);
    }

    public static void AcceptedHandler(Lobby lobby)
    {
      EventHandler accepted = AccountHelper.Accepted;
      if (accepted == null)
        return;
      accepted((object) lobby, EventArgs.Empty);
    }

    public static void CyclesStartedHandler(Lobby lobby)
    {
      EventHandler cyclesStarted = AccountHelper.CyclesStarted;
      if (cyclesStarted == null)
        return;
      cyclesStarted((object) lobby, EventArgs.Empty);
    }

    public static void CyclesEndedHandler(Lobby lobby, bool status)
    {
      EventHandler<bool> cyclesEnded = AccountHelper.CyclesEnded;
      if (cyclesEnded == null)
        return;
      cyclesEnded((object) lobby, status);
    }

    public static List<Tuple<User, Process>> ActiveProcList { get; set; } = new List<Tuple<User, Process>>();

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

    public static void CheckCSGOStatus(CancellationToken token)
    {
      Task.Factory.StartNew<Task>((Func<Task>) (async () =>
      {
        while (!token.IsCancellationRequested)
        {
          try
          {
            List<User> source = new List<User>();
            lock (LobbyDb.G)
            {
              foreach (Lobby lobby in (List<Lobby>) LobbyDb.G)
                source.AddRange((IEnumerable<User>) Lobby.Users);
            }
            List<Tuple<User, Process>> tupleList1 = new List<Tuple<User, Process>>();
            List<Tuple<User, Process>> tupleList2 = new List<Tuple<User, Process>>();
            foreach (Process process in Process.GetProcesses())
            {
              string lower = process.ProcessName.ToLower();
              if (lower.Equals("csgo"))
              {
                string login = "";
                for (Match match = AccountHelper.pattern_login.Match(process.MainWindowTitle); match.Success; match = match.NextMatch())
                  login = match.Groups[1].Value.ToLower();
                if (!string.IsNullOrEmpty(login))
                {
                  User account = source.FirstOrDefault<User>((Func<User, bool>) (o => o != null && o.IsUse && o.Login.ToLower().Equals(login)));
                  if (account != null)
                    tupleList1.Add(new Tuple<User, Process>(account, process));
                }
              }
              if (lower.StartsWith("steam_"))
              {
                ulong userid;
                if (ulong.TryParse(lower.Replace("steam_", ""), out userid))
                {
                  User account = source.FirstOrDefault<User>((Func<User, bool>) (o => o.SID == userid));
                  if (account != null)
                    tupleList2.Add(new Tuple<User, Process>(account, process));
                }
              }
            }
            lock (AccountHelper.ActiveProcList)
            {
              AccountHelper.ActiveProcList.Clear();
              AccountHelper.ActiveProcList.AddRange((IEnumerable<Tuple<User, Process>>) tupleList1);
            }
            lock (AccountHelper.ActiveSteamList)
            {
              AccountHelper.ActiveSteamList.Clear();
              AccountHelper.ActiveSteamList.AddRange((IEnumerable<Tuple<User, Process>>) tupleList2);
            }
            for (int index = 0; index < source.Count; ++index)
            {
              User a = source[index];
              bool flag1 = false;
              Tuple<User, Process> tuple = tupleList1.FirstOrDefault<Tuple<User, Process>>((Func<Tuple<User, Process>, bool>) (o => o != null && o.Item1 != null && o.Item1.Login.Equals(a.Login)));
              bool flag2 = tuple != null;
              try
              {
                if (flag2 && !a.IsStarted)
                {
                  a.IsStarted = true;
                  flag1 = true;
                }
                if (!flag2 && a.IsStarted)
                {
                  a.IsStarted = false;
                  flag1 = true;
                }
                int num = tupleList2.FirstOrDefault<Tuple<User, Process>>((Func<Tuple<User, Process>, bool>) (o => o != null && o.Item1 != null && o.Item1.Login.Equals(a.Login))) != null ? 1 : 0;
                if (num != 0 && !a.IsStartedSteam)
                {
                  a.IsStartedSteam = true;
                  flag1 = true;
                }
                if (num == 0)
                {
                  if (a.IsStartedSteam)
                  {
                    a.IsStartedSteam = false;
                    flag1 = true;
                  }
                }
              }
              finally
              {
                if (flag2)
                {
                  IntPtr mainWindowHandle = tuple.Item2.MainWindowHandle;
                  WinApi.RECT rect = new WinApi.RECT();
                  WinApi.RECT local = rect;
                  WinApi.GetWindowRect(mainWindowHandle, ref local);
                  if (rect.Right - rect.Left > 0 && rect.Bottom - rect.Top > 0 && rect.Left >= 0 && rect.Top >= 0)
                  {
                    int num1 = rect.Right - rect.Left;
                    int num2 = rect.Bottom - rect.Top;
                    if (a.X != rect.Left || a.Y != rect.Top || a.W != num1 || a.H != num2)
                    {
                      a.X = rect.Left;
                      a.Y = rect.Top;
                      a.W = num1;
                      a.H = num2;
                      flag1 = true;
                    }
                  }
                }
                if (flag1)
                {
                  EventHandler<User> accountStatusChanged = AccountHelper.AccountStatusChanged;
                  if (accountStatusChanged != null)
                    accountStatusChanged((object) null, a);
                }
              }
            }
          }
          catch
          {
          }
          finally
          {
            await Task.Delay(500);
          }
        }
      }));
      AccountHelper.gsl = new GameStateListener(3000);
      AccountHelper.gsl.NewGameState += new NewGameStateHandler(AccountHelper.OnNewGameState);
      if (!AccountHelper.gsl.Start())
        throw new Exception("Не удалось запустить GSI");
    }

    public static void StopGameStateListener() => AccountHelper.gsl?.Stop();

    private static void OnNewGameState(GameState gs)
    {
      try
      {
        User account = (User) null;
        Lobby lobby1 = (Lobby) null;
        lock (LobbyDb.G)
        {
          foreach (Lobby lobby2 in (List<Lobby>) LobbyDb.G)
          {
            account = Lobby.Users.FirstOrDefault<User>((Func<User, bool>) (o => !string.IsNullOrEmpty(Convert.ToString(o.SteamID)) && o.SteamID.Equals(gs.Player.SteamID)));
            if (account != null)
            {
              lobby1 = lobby2;
              break;
            }
          }
        }
        account?.Lobby.TaskGameState(gs, lobby1);
      }
      catch (Exception ex)
      {
        Msg.ShowError($"{ex}");
      }
    }

    public static Process GetProcess(User account) => AccountHelper.ActiveProcList.FirstOrDefault<Tuple<User, Process>>((Func<Tuple<User, Process>, bool>) (o => o.Item1.Login.Equals(account.Login)))?.Item2;

    public static Process GetProcessSteam(User account) => AccountHelper.ActiveSteamList.FirstOrDefault<Tuple<User, Process>>((Func<Tuple<User, Process>, bool>) (o => o.Item1.Login.Equals(account.Login)))?.Item2;


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

    private static void CheckAccount(User account)
    {
      if (account == null)
        throw new Exception("Не задан аккаунт! Обратитесь к разработчику");
      if (string.IsNullOrEmpty(account.Login))
        throw new Exception("Не задан логин для аккаунта!");
      if (string.IsNullOrEmpty(account.Password))
        throw new Exception("Не задан пароль для аккаунта!");
      if (account.Lobby == null)
        throw new Exception("Не задано лобби для аккаунта " + account.Login + "! Обратитесь к разработчику");
    }

    private static void CheckAccountAndPipe(User account)
    {
      AccountHelper.CheckAccount(account);
      if (!AccountHelper.IsNamedPipeExist(account.Login))
        throw new Exception("Сервер не запущен");
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