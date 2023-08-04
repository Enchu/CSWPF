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
using CSWPF.Helpers;
using CSWPF.Utils;
using CSWPF.Direct;
using CSWPF.Boost;
using CSWPF.Boost.Models;

namespace CSWPF.Directory;

public class AccountHelper
  {
    private static GameStateListener gsl;
    private static Regex pattern_login = new Regex("\\ LOGIN: (?<val>.*?)\\ ", RegexOptions.Compiled | RegexOptions.Singleline);

    public static Account MoveAccount { get; set; } = (Account) null;

    public static event EventHandler<Account> AccountStatusChanged;

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

    public static List<Tuple<Account, Process>> ActiveProcList { get; set; } = new List<Tuple<Account, Process>>();

    public static List<Tuple<Account, Process>> ActiveSteamList { get; set; } = new List<Tuple<Account, Process>>();

    public static bool CheckExistsAndRunWithoutSandbox(Account account)
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
            List<Account> source = new List<Account>();
            lock (LobbyDb.G)
            {
              foreach (Lobby lobby in (List<Lobby>) LobbyDb.G)
                source.AddRange((IEnumerable<Account>) lobby.Accounts);
            }
            List<Tuple<Account, Process>> tupleList1 = new List<Tuple<Account, Process>>();
            List<Tuple<Account, Process>> tupleList2 = new List<Tuple<Account, Process>>();
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
                  Account account = source.FirstOrDefault<Account>((Func<Account, bool>) (o => o != null && o.IsUse && o.Login.ToLower().Equals(login)));
                  if (account != null)
                    tupleList1.Add(new Tuple<Account, Process>(account, process));
                }
              }
              if (lower.StartsWith("steam_"))
              {
                ulong userid;
                if (ulong.TryParse(lower.Replace("steam_", ""), out userid))
                {
                  Account account = source.FirstOrDefault<Account>((Func<Account, bool>) (o => o.SID == userid));
                  if (account != null)
                    tupleList2.Add(new Tuple<Account, Process>(account, process));
                }
              }
            }
            lock (AccountHelper.ActiveProcList)
            {
              AccountHelper.ActiveProcList.Clear();
              AccountHelper.ActiveProcList.AddRange((IEnumerable<Tuple<Account, Process>>) tupleList1);
            }
            lock (AccountHelper.ActiveSteamList)
            {
              AccountHelper.ActiveSteamList.Clear();
              AccountHelper.ActiveSteamList.AddRange((IEnumerable<Tuple<Account, Process>>) tupleList2);
            }
            for (int index = 0; index < source.Count; ++index)
            {
              Account a = source[index];
              bool flag1 = false;
              Tuple<Account, Process> tuple = tupleList1.FirstOrDefault<Tuple<Account, Process>>((Func<Tuple<Account, Process>, bool>) (o => o != null && o.Item1 != null && o.Item1.Login.Equals(a.Login)));
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
                int num = tupleList2.FirstOrDefault<Tuple<Account, Process>>((Func<Tuple<Account, Process>, bool>) (o => o != null && o.Item1 != null && o.Item1.Login.Equals(a.Login))) != null ? 1 : 0;
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
                    if (a.X != rect.Left || a.Y != rect.Top || Settings.W != num1 || Settings.H != num2)
                    {
                      a.X = rect.Left;
                      a.Y = rect.Top;
                      Settings.W = num1;
                      Settings.H = num2;
                      flag1 = true;
                    }
                  }
                }
                if (flag1)
                {
                  EventHandler<Account> accountStatusChanged = AccountHelper.AccountStatusChanged;
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
        Account account = (Account) null;
        Lobby lobby1 = (Lobby) null;
        lock (LobbyDb.G)
        {
          foreach (Lobby lobby2 in (List<Lobby>) LobbyDb.G)
          {
            account = lobby2.Accounts.FirstOrDefault<Account>((Func<Account, bool>) (o => !string.IsNullOrEmpty(Convert.ToString(o.SteamID)) && o.SteamID.Equals(gs.Player.SteamID)));
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

    public static Process GetProcess(Account account) => AccountHelper.ActiveProcList.FirstOrDefault<Tuple<Account, Process>>((Func<Tuple<Account, Process>, bool>) (o => o.Item1.Login.Equals(account.Login)))?.Item2;

    public static Process GetProcess(User account) => AccountHelper.ActiveProcList.FirstOrDefault<Tuple<Account, Process>>((Func<Tuple<Account, Process>, bool>) (o => o.Item1.Login.Equals(account.Login)))?.Item2;

    public static Process GetProcessSteam(Account account) => AccountHelper.ActiveSteamList.FirstOrDefault<Tuple<Account, Process>>((Func<Tuple<Account, Process>, bool>) (o => o.Item1.Login.Equals(account.Login)))?.Item2;

    public static int KillAll(Lobby lobby)
    {
      int num = 0;
      lock (AccountHelper.ActiveProcList)
      {
        foreach (Account account1 in lobby.Accounts)
        {
          Account account = account1;
          if (AccountHelper.ActiveProcList.FirstOrDefault<Tuple<Account, Process>>((Func<Tuple<Account, Process>, bool>) (o => o.Item1.Login.Equals(account.Login))) != null)
          {
            try
            {
              AccountHelper.Kill(account);
              ++num;
            }
            catch
            {
            }
          }
        }
      }
      return num;
    }

    public static void OpenFolder(Account account)
    {
      string checkedPath = AccountHelper.GetCheckedPath(Options.G.CSGOPath);
      if (!System.IO.Directory.Exists(checkedPath))
        throw new Exception("Не задан путь к папке CSGO или не установлен CSGO");
      AccountHelper.OpenFolder(checkedPath + "\\csgo_" + account.SID.ToString());
    }

    public static void OpenUserDataCfg(Account account) => AccountHelper.OpenFolder(AccountHelper.GetCheckedPath(Options.G.StreamPath) + "\\userdata\\" + account.SID.ToString() + "\\730\\local\\cfg\\");

    public static void OpenFolder(string folder) => new Process()
    {
      StartInfo = new ProcessStartInfo()
      {
        FileName = "explorer.exe",
        Arguments = folder
      }
    }.Start();

    public static void SetupLeaders(Lobby lobby)
    {
      Msg.ShowInfo(nameof (SetupLeaders));
      string checkedPath = AccountHelper.GetCheckedPath(Options.G.CSGOPath);
      if (!System.IO.Directory.Exists(checkedPath))
        throw new Exception("Не задан путь к папке CSGO или не установлен CSGO");
      Account leader1 = lobby.Leader1;
      Account leader2 = lobby.Leader2;
      AccountHelper.UpdateCFG(leader1);
      long sid1 = (long)leader1.SID;
      string login1 = leader1.Login;
      string str1 = checkedPath + "\\csgo_" + sid1.ToString();
      AccountHelper.lineChanger("\tgame\t\"@ ACCOUNT: " + sid1.ToString() + " | LOGIN: " + login1 + " | LEADER #1\"", str1 + "/gameinfo.txt", 3);
      AccountHelper.FixAutoexec(leader1);
      File.Copy(Environment.CurrentDirectory + "\\data\\gamestate_integration_boost.cfg", str1 + "\\cfg\\gamestate_integration_boost.cfg", true);
      AccountHelper.UpdateCFG(leader2);
      long sid2 = (long)leader2.SID;
      string login2 = leader2.Login;
      string str2 = checkedPath + "\\csgo_" + sid2.ToString();
      AccountHelper.lineChanger("\tgame\t\"@ ACCOUNT: " + sid2.ToString() + " | LOGIN: " + login2 + " | LEADER #2\"", str2 + "/gameinfo.txt", 3);
      AccountHelper.FixAutoexec(leader2);
      File.Copy(Environment.CurrentDirectory + "\\data\\gamestate_integration_boost.cfg", str2 + "\\cfg\\gamestate_integration_boost.cfg", true);
      foreach (Account account in lobby.Accounts.Where<Account>((Func<Account, bool>) (o => o.IsUse && !o.IsLeader)))
      {
        AccountHelper.UpdateCFG(account);
        File.Copy(Environment.CurrentDirectory + "\\data\\gamestate_integration_boost.cfg", checkedPath + "\\csgo_" + account.SID.ToString() + "\\cfg\\gamestate_integration_boost.cfg", true);
      }
    }

    public static string GetCheckedPath(string path)
    {
      string checkedPath = path + "\\";
      if (checkedPath[checkedPath.Length - 1].Equals('\\'))
        checkedPath = checkedPath.Substring(0, checkedPath.Length - 1);
      return checkedPath;
    }

    private static void FixAutoexec(Account account)
    {
      string path = AccountHelper.GetCheckedPath(Options.G.CSGOPath) + "\\csgo_" + account.SID.ToString();
      if (!System.IO.Directory.Exists(path))
        throw new Exception("Не найдена папка \\csgo_" + account.SID.ToString() + " для аккаунта " + account.Login + "!");
      AccountHelper.lineChanger(string.Format("mat_setvideomode {0} {1} 1", (object) account.Lobby.Width, (object) account.Lobby.Height), path + "/cfg/autoexec.cfg", 12);
      if (account.Lobby.Leader1 == account)
        AccountHelper.lineChanger("con_logfile log/1.log", path + "/cfg/autoexec.cfg", 15);
      else if (account.Lobby.Leader2 == account)
        AccountHelper.lineChanger("con_logfile log/2.log", path + "/cfg/autoexec.cfg", 15);
      else
        AccountHelper.lineChanger("//con_logfile log/0.log", path + "/cfg/autoexec.cfg", 15);
    }

    public static void Start(Account account)
    {
      AccountHelper.CheckAccount(account);
      if (!System.IO.Directory.Exists(AccountHelper.GetCheckedPath(Options.G.CSGOPath) + "\\csgo_" + account.SID.ToString()))
        throw new Exception("Не найдена папка \\csgo_" + account.SID.ToString() + " для аккаунта " + account.Login + "!");
      AccountHelper.FixAutoexec(account);
      if (!AccountHelper.IsNamedPipeExist(account.Login))
      {
        string str1 = System.IO.Directory.GetCurrentDirectory() + "/launcher/Launcher.exe";
        string checkedPath = AccountHelper.GetCheckedPath(Options.G.StreamPath);
        string str2 = Options.G.IsHideLauncher ? "1" : "0";
        string str3 = " \"" + account.Login + "\"" + (" \"" + account.Login + "\"") + (" \"" + account.Password + "\"") + (" \"" + account.Lobby.Parameters + "\"") + (" " + account.SteamID) + string.Format(" {0}", (object) account.SID) + (" \"" + account.Lobby.Leader1.Login + "\"") + (" \"" + account.Lobby.Leader2.Login + "\"") + string.Format(" {0}", (object) account.X) + string.Format(" {0}", (object) account.Y) + " 640" + " 480" + (" " + str2) + (" \"" + checkedPath + "\"");
        new Process()
        {
          StartInfo = new ProcessStartInfo()
          {
            FileName = str1,
            Arguments = str3,
            CreateNoWindow = true,
            UseShellExecute = false
          }
        }.Start();
      }
      else
        AccountHelper.Pipe(account.Login, "start");
      account.WaitToClick1 = true;
      account.Lobby.StartWorkers();
    }

    public static void Kill(Account account)
    {
      try
      {
        AccountHelper.CheckAccountAndPipe(account);
        AccountHelper.Pipe(account.Login, "kill");
      }
      catch
      {
      }
      try
      {
        Process process = AccountHelper.GetProcess(account);
        if (process != null)
          process?.Kill();
      }
      catch
      {
      }
      try
      {
        Process processSteam = AccountHelper.GetProcessSteam(account);
        if (processSteam == null || processSteam == null)
          return;
        processSteam.Kill();
      }
      catch
      {
      }
    }

    public static void Restart(Account account)
    {
      AccountHelper.CheckAccountAndPipe(account);
      AccountHelper.Pipe(account.Login, "restart");
    }

    public static void Hide(Account account)
    {
      AccountHelper.CheckAccountAndPipe(account);
      AccountHelper.Pipe(account.Login, "hide");
    }

    public static void Show(Account account)
    {
      AccountHelper.CheckAccountAndPipe(account);
      AccountHelper.Pipe(account.Login, "show");
    }

    public static void Quit(Account account)
    {
      AccountHelper.CheckAccountAndPipe(account);
      AccountHelper.Pipe(account.Login, "quit");
    }

    public static void SteamOpen(Account account)
    {
      AccountHelper.CheckAccountAndPipe(account);
      AccountHelper.Pipe(account.Login, "steam");
    }

    public static void Clear(Account account)
    {
      AccountHelper.CheckAccountAndPipe(account);
      AccountHelper.Pipe(account.Login, "clear");
    }

    public static void KillAll()
    {
      try
      {
        foreach (Process process in ((IEnumerable<Process>) Process.GetProcesses()).Where<Process>((Func<Process, bool>) (pr => pr.ProcessName.ToLower().Equals("csgo"))))
          process.Kill();
      }
      catch (Exception ex)
      {
      }
      try
      {
        ((IEnumerable<Process>) Process.GetProcesses()).Where<Process>((Func<Process, bool>) (x => x.ProcessName.ToLower().StartsWith("steam"))).ToList<Process>().ForEach((Action<Process>) (x => x.Kill()));
      }
      catch (Exception ex)
      {
      }
      try
      {
        foreach (Process process in ((IEnumerable<Process>) Process.GetProcesses()).Where<Process>((Func<Process, bool>) (pr => pr.ProcessName.ToLower().Equals("launcher"))))
          process.Kill();
      }
      catch (Exception ex)
      {
      }
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

    private static void CheckAccount(Account account)
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

    private static void CheckAccountAndPipe(Account account)
    {
      AccountHelper.CheckAccount(account);
      if (!AccountHelper.IsNamedPipeExist(account.Login))
        throw new Exception("Сервер не запущен");
    }

    public static void UpdateCFG(Account account)
    {
      AccountHelper.CheckAccount(account);
      string lower = account.Login.ToLower();
      string password = account.Password;
      string str1 = Options.G.StreamPath + "\\";
      string path = Options.G.CSGOPath + "\\";
      string str2 = str1 + "\\Steam.exe";
      if (string.IsNullOrEmpty(str1))
        throw new Exception("Укажите папку Steam");
      if (string.IsNullOrEmpty(path))
        throw new Exception("Укажите папку CSGO");
      if (!File.Exists(str2))
        throw new Exception("Не установлен Steam");
      if (!System.IO.Directory.Exists(path))
        throw new Exception("Не установлен CSGO");
      string input = "";
      string str3 = "";
      using (FileStream fileStream = new FileStream(str1 + "\\config\\loginusers.vdf", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
      {
        using (StreamReader streamReader = new StreamReader((Stream) fileStream))
        {
          input = streamReader.ReadToEnd();
          str3 = Regex.Match(input, "AccountName\"\t\t\"" + lower + "([^\n]+)").Groups[0].Value;
        }
      }
      if (string.IsNullOrEmpty(str3))
      {
        new Process()
        {
          StartInfo = new ProcessStartInfo()
          {
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = str1,
            FileName = str2,
            Arguments = ("-login " + lower)
          }
        }.Start();
        Clipboard.SetText(password);
        throw new Exception("Аккаунт не найден, авторизируйтесь перед тем как продолжить");
      }
      string[] array = input.Split(new string[3]
      {
        "\r\n",
        "\r",
        "\n"
      }, StringSplitOptions.None);
      string str4 = Regex.Match(array[Array.IndexOf<string>(array, "\t\t\"AccountName\"\t\t\"" + lower + "\"") - 2], "\"([^\"]+)").Groups[1].Value;
      string s = !string.IsNullOrEmpty(str4) ? str4 : throw new Exception("Не удалось получить STEAMID64");
      long result = 0;
      if (!long.TryParse(s, out result))
        throw new Exception("Не удалось получить STEAMID64");
      result -= 76561197960265728L;
      if (result == 0L)
        throw new Exception("Не удалось получить STEAMID64");
      Account leader1 = account.Lobby.Leader1;
      Account leader2 = account.Lobby.Leader2;
      account.SteamID = Convert.ToUInt64(s);
      account.SID = (ulong)result;
      string str5 = path + "csgo_" + result.ToString();
      if (!File.Exists(str1 + "Steam_" + result.ToString() + ".exe"))
        File.Copy(str2, str1 + "Steam_" + result.ToString() + ".exe");
      AccountHelper.copyDirectory("data/game/", path + "csgo_" + result.ToString());
      if (leader1.Login.Equals(account.Login))
        AccountHelper.lineChanger("\tgame\t\"@ LOGIN: " + account.Login + " | LEADER #1\"", str5 + "/gameinfo.txt", 3);
      else if (leader2.Login.Equals(account.Login))
        AccountHelper.lineChanger("\tgame\t\"@ LOGIN: " + account.Login + " | LEADER #2\"", str5 + "/gameinfo.txt", 3);
      else
        AccountHelper.lineChanger("\tgame\t\"@ LOGIN: " + account.Login + " | BOT\"", str5 + "/gameinfo.txt", 3);
      if (!System.IO.Directory.Exists(str1 + "userdata\\" + result.ToString() + "\\"))
        System.IO.Directory.CreateDirectory(str1 + "userdata\\" + result.ToString() + "\\");
      if (!System.IO.Directory.Exists(str1 + "userdata\\" + result.ToString() + "\\730\\"))
        System.IO.Directory.CreateDirectory(str1 + "userdata\\" + result.ToString() + "\\730\\");
      if (!System.IO.Directory.Exists(str1 + "userdata\\" + result.ToString() + "\\730\\local\\"))
        System.IO.Directory.CreateDirectory(str1 + "userdata\\" + result.ToString() + "\\730\\local\\");
      if (!System.IO.Directory.Exists(str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\"))
        System.IO.Directory.CreateDirectory(str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\");
      int num = File.Exists(str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\config.cfg") ? 1 : 0;
      bool flag1 = File.Exists(str1 + "userdata\\" + result.ToString() + "\\730/local\\cfg\\video.txt");
      bool flag2 = File.Exists(str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\videodefaults.txt");
      if (num != 0)
      {
        if (!AccountHelper.IsFileReadOnly(str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\config.cfg"))
        {
          File.Copy("data/userdata/config.cfg", str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\config.cfg", true);
        }
        else
        {
          AccountHelper.SetFileReadAccess(str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\config.cfg", false);
          File.Copy("data/userdata/config.cfg", str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\config.cfg", true);
        }
        AccountHelper.SetFileReadAccess(str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\config.cfg", true);
      }
      else
      {
        File.Copy("data/userdata/config.cfg", str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\config.cfg", true);
        AccountHelper.SetFileReadAccess(str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\config.cfg", true);
      }
      if (flag1)
      {
        if (!AccountHelper.IsFileReadOnly(str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\video.txt"))
        {
          File.Copy("data/userdata/video.txt", str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\video.txt", true);
        }
        else
        {
          AccountHelper.SetFileReadAccess(str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\video.txt", false);
          File.Copy("data/userdata/video.txt", str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\video.txt", true);
        }
        AccountHelper.SetFileReadAccess(str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\video.txt", true);
      }
      else
      {
        File.Copy("data/userdata/video.txt", str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\video.txt", true);
        AccountHelper.SetFileReadAccess(str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\video.txt", true);
      }
      if (flag2)
      {
        if (!AccountHelper.IsFileReadOnly(str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\videodefaults.txt"))
        {
          File.Copy("data/userdata/videodefaults.txt", str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\videodefaults.txt", true);
        }
        else
        {
          AccountHelper.SetFileReadAccess(str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\videodefaults.txt", false);
          File.Copy("data/userdata/videodefaults.txt", str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\videodefaults.txt", true);
        }
      }
      else
      {
        File.Copy("data/userdata/videodefaults.txt", str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\videodefaults.txt", true);
        AccountHelper.SetFileReadAccess(str1 + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\videodefaults.txt", true);
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