using CSGSI;
using CSGSI.Nodes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSWPF.Helpers;
using CSWPF.Helpers.Data;

namespace CSWPF.Directory.Models;

public class Lobby
  {
    [DataMember(Name = "account")]
    public List<Account> Accounts = new List<Account>();
    [NonSerialized]
    private bool _isNeedRestartCycles;
    [DataMember(Name = "sizeEnum")]
    private SizeEnum _sizeEnum;
    [NonSerialized]
    private int _gamesCounter;
    [NonSerialized]
    private bool _isWaitAutoApply;
    [NonSerialized]
    private bool _isWaitCycles;
    [NonSerialized]
    private bool _isWorkerStarted;
    private static Point pointButtonSearch = new Point(457, 459);
    private MapMode _mapMode;
    private MapPhase _mapPhase;
    private RoundWinTeam _winTeam;
    private bool _isGameOver;
    private bool _mapPhaseLive;
    private RoundPhase _roundPhase;
    private int _currentRound;
    private int score0;
    private int score1;
    private bool? _loseLobbyIsCT;
    private int _gs_counter;
    private int logPos1;
    private int logPos2;
    private static string Match1 = "Received Steam datagram ticket for server steamid:([0-9]+) vport ([0-9]+)";
    private static string Match2 = "numMachines int([(]) ([^\\ =]+)";
    [NonSerialized]
    private Account _loseLeader;
    [NonSerialized]
    private Account _winLeader;
    [NonSerialized]
    private Dictionary<Account, GameState> _gameStates = new Dictionary<Account, GameState>();
    private int _needRestart1;
    private int _needRestart2;

    public static Lobby Current { get; set; }

    public int Id { get; set; }

    [DataMember(Name = "guid")]
    public string Guid { get; set; }

    public bool IsNeedRestartCycles
    {
      get => this._isNeedRestartCycles;
      set => this._isNeedRestartCycles = value;
    }

    public string Parameters => Options.G.ParametersDefault;

    public Lobby.LobbyKindEnum LobbyKind { get; set; }

    public bool IsAutoStartGame { get; set; }

    public bool IsAutoApplyGame { get; set; }

    public SizeEnum Size
    {
      get => this._sizeEnum;
      set
      {
        this._sizeEnum = value;
        switch (this._sizeEnum)
        {
          case SizeEnum.Size2K:
            this.Width = 500;
            this.Height = 375;
            break;
          case SizeEnum.SizeFHD:
            this.Width = 350;
            this.Height = this.Width / 4 * 3;
            break;
        }
      }
    }

    [DataMember(Name = "width")]
    public int Width { get; set; } = 500;

    [DataMember(Name = "height")]
    public int Height { get; set; } = 375;

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "isLobby0Only")]
    public bool IsLobby0Only { get; set; }

    [DataMember(Name = "isLobby1Only")]
    public bool IsLobby1Only { get; set; }

    public bool IsAutoDisconnectLobbies { get; set; } = true;

    public Lobby()
    {
      this.Guid = System.Guid.NewGuid().ToString();
      for (int index = 0; index < 10; ++index)
        this.Accounts.Add(new Account(this)
        {
          IsLeader = index == 0 || index == 5
        });
    }

    private IEnumerable<Account> GetLeaders()
    {
      IEnumerable<Account> source = this.Accounts.Where<Account>((Func<Account, bool>) (o => o.IsLeader));
      return source.Count<Account>() == 2 ? source : throw new Exception("Лобби не настроено! Необходимо установить двух лидеров!");
    }

    public Account Leader1 => this.GetLeaders().First<Account>();

    public Account Leader2 => this.GetLeaders().Last<Account>();

    public void StartWorkers()
    {
      if (this._isWorkerStarted)
        return;
      CancellationToken token = App.Token;
      Task.Factory.StartNew<Task>((Func<Task>) (async () =>
      {
        try
        {
          while (!token.IsCancellationRequested)
          {
            await Task.Delay(3000, token);
            if (this._isWaitAutoApply)
            {
              try
              {
                Log.Write(string.Format("Task ACCEPT - {0} {1} {2} - START", (object) this.Name, (object) this.Leader1, (object) this.Leader2));
                await this.TaskApply(token);
              }
              catch (Exception ex)
              {
                Log.WriteException(ex);
              }
            }
          }
        }
        catch (Exception ex)
        {
          Log.WriteException(ex);
        }
      }), token);
      this._isWorkerStarted = true;
    }

    public async Task SetForegroundAccount(Process proc, CancellationToken token, int timeDelay = 500)
    {
      if (proc == null)
        return;
      WinApi.SetForegroundWindow(proc.MainWindowHandle);
      if (timeDelay <= 0)
        return;
      await Task.Delay(timeDelay, token);
    }

    public async Task SetForegroundAccount(Account account, CancellationToken token, int timeDelay = 500) => await this.SetForegroundAccount(AccountHelper.GetProcess(account), token, timeDelay);

    public Color GetColor(Account account, int x, int y)
    {
      int clickX;
      int clickY;
      this.RecalcPositions(account, x, y, out clickX, out clickY);
      Rectangle bounds1 = Screen.PrimaryScreen.Bounds;
      int width = bounds1.Width;
      bounds1 = Screen.PrimaryScreen.Bounds;
      int height = bounds1.Height;
      using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb))
      {
        using (Graphics graphics1 = Graphics.FromImage((Image) bitmap))
        {
          Graphics graphics2 = graphics1;
          int x1 = Screen.PrimaryScreen.Bounds.X;
          Rectangle bounds2 = Screen.PrimaryScreen.Bounds;
          int y1 = bounds2.Y;
          bounds2 = Screen.PrimaryScreen.Bounds;
          System.Drawing.Size size = bounds2.Size;
          graphics2.CopyFromScreen(x1, y1, 0, 0, size, CopyPixelOperation.SourceCopy);
          return bitmap.GetPixel(clickX, clickY);
        }
      }
    }

    public void GetDetectButtonState(Account account, int x, int y, out bool g, out bool r)
    {
      Color color = this.GetColor(account, x, y);
      g = color.G > (byte) 60 && (int) color.G > (int) color.R;
      r = color.R > (byte) 40 && (int) color.R > (int) color.G;
    }

    public async Task StartAndGoLeaders()
    {
      Lobby lobby = this;
      Log.Write(nameof (StartAndGoLeaders));
      CancellationToken token = App.Token;
      int counter = 0;
      lobby.StartWorkers();
      WinApi.POINT lpPoint;
      WinApi.GetCursorPos(out lpPoint);
      await lobby.SetForegroundAccount(lobby.Leader1, token);
      await Lobby.SendKeyAsync(lobby.Leader1, 27, 200, token);
      await lobby.ClickToAccount(lobby.Leader1, 19, 62, 200, token);
      await lobby.SetForegroundAccount(lobby.Leader2, token);
      await Lobby.SendKeyAsync(lobby.Leader2, 27, 200, token);
      await lobby.ClickToAccount(lobby.Leader2, 19, 62, 200, token);
      await Task.Delay(500, token);
      if (lobby.Accounts.Count<Account>((Func<Account, bool>) (o => o.IsStarted)) == 4)
      {
        await lobby.ClickToAccount(lobby.Leader1, 133, 84, 200, token);
        await lobby.ClickToAccount(lobby.Leader2, 133, 84, 200, token);
        await Task.Delay(500, token);
      }
      bool b1 = false;
      bool b2 = false;
      do
      {
        if (!b1)
        {
          await lobby.SetForegroundAccount(lobby.Leader1, token);
          bool g;
          lobby.GetDetectButtonState(lobby.Leader1, Lobby.pointButtonSearch.X, Lobby.pointButtonSearch.Y, out g, out bool _);
          if (g)
            await lobby.ClickToAccount(lobby.Leader1, Lobby.pointButtonSearch.X, Lobby.pointButtonSearch.Y, 300, token);
        }
        if (!b2)
        {
          await lobby.SetForegroundAccount(lobby.Leader2, token);
          bool g;
          lobby.GetDetectButtonState(lobby.Leader2, Lobby.pointButtonSearch.X, Lobby.pointButtonSearch.Y, out g, out bool _);
          if (g)
            await lobby.ClickToAccount(lobby.Leader2, Lobby.pointButtonSearch.X, Lobby.pointButtonSearch.Y, 300, token);
        }
        await Task.Delay(1000, token);
        if (!b1)
        {
          bool r;
          lobby.GetDetectButtonState(lobby.Leader1, Lobby.pointButtonSearch.X, Lobby.pointButtonSearch.Y, out bool _, out r);
          if (r)
            b1 = true;
        }
        if (!b2)
        {
          bool r;
          lobby.GetDetectButtonState(lobby.Leader2, Lobby.pointButtonSearch.X, Lobby.pointButtonSearch.Y, out bool _, out r);
          if (r)
            b2 = true;
        }
        ++counter;
        if (counter == 100)
          goto label_19;
      }
      while (!b1 || !b2);
      goto label_28;
label_19:
      token = new CancellationToken();
      return;
label_28:
      await lobby.ClearLogs();
      WinApi.SetCursorPos(lpPoint.X, lpPoint.Y);
      lobby._isWaitAutoApply = true;
      AccountHelper.WaitForAcceptHandler(lobby);
      token = new CancellationToken();
    }

    public async Task RestartLeader(Account account)
    {
      CancellationToken token = App.Token;
      WinApi.POINT lpPoint;
      WinApi.GetCursorPos(out lpPoint);
      bool b1 = false;
      while (this._isWaitAutoApply)
      {
        if (!b1)
        {
          await this.SetForegroundAccount(account, token);
          bool g;
          this.GetDetectButtonState(account, Lobby.pointButtonSearch.X, Lobby.pointButtonSearch.Y, out g, out bool _);
          if (g)
            await this.ClickToAccount(account, Lobby.pointButtonSearch.X, Lobby.pointButtonSearch.Y, 300, token);
        }
        await Task.Delay(1000, token);
        if (!b1)
        {
          bool r;
          this.GetDetectButtonState(account, Lobby.pointButtonSearch.X, Lobby.pointButtonSearch.Y, out bool _, out r);
          if (r)
            b1 = true;
        }
        if (b1)
        {
          WinApi.SetCursorPos(lpPoint.X, lpPoint.Y);
          token = new CancellationToken();
          return;
        }
      }
      token = new CancellationToken();
    }

    public void CancelAccept()
    {
      Log.Write(nameof (CancelAccept));
      this._isWaitAutoApply = false;
      AccountHelper.AcceptCancelledHandler(this);
    }

    public void CancelCycles()
    {
      Log.Write(nameof (CancelCycles));
      this._isWaitCycles = false;
    }

    public async Task AssemblyLobbies(bool l1, bool l2)
    {
      Lobby lobby1 = this;
      try
      {
        Log.Write(nameof (AssemblyLobbies));
        CancellationTokenSource cts = new CancellationTokenSource();
        bool ctrl = false;
        bool f3 = false;
        InterceptKeys.KeyPressed += (EventHandler<Keys>) ((sender, e) =>
        {
          if (e == Keys.LControlKey)
            ctrl = true;
          if (e == Keys.F3)
            f3 = true;
          if (!(f3 & ctrl))
            return;
          cts.Cancel();
        });
        InterceptKeys.KeyUnpressed += (EventHandler<Keys>) ((sender, e) =>
        {
          if (e == Keys.LControlKey)
            ctrl = false;
          if (e != Keys.F3)
            return;
          f3 = false;
        });
        InterceptKeys.SetHook();
        await lobby1.MoveAllAccounts(cts.Token);
        if (l1)
        {
          Lobby lobby2 = lobby1;
          Account leader1 = lobby1.Leader1;
          List<Account> accounts = new List<Account>();
          accounts.Add(lobby1.Accounts[1]);
          accounts.Add(lobby1.Accounts[2]);
          accounts.Add(lobby1.Accounts[3]);
          accounts.Add(lobby1.Accounts[4]);
          CancellationToken token = cts.Token;
          await lobby2.AssemblyLobby(leader1, accounts, token);
        }
        if (l2)
        {
          Lobby lobby3 = lobby1;
          Account leader2 = lobby1.Leader2;
          List<Account> accounts = new List<Account>();
          accounts.Add(lobby1.Accounts[6]);
          accounts.Add(lobby1.Accounts[7]);
          accounts.Add(lobby1.Accounts[8]);
          accounts.Add(lobby1.Accounts[9]);
          CancellationToken token = cts.Token;
          await lobby3.AssemblyLobby(leader2, accounts, token);
        }
      }
      finally
      {
        InterceptKeys.RemoveHook();
      }
    }

    private async Task CopyBotNumberToClipboard(
      Account account,
      IntPtr hwnd,
      CancellationToken token,
      CancellationToken token2)
    {
      await this.SetForegroundAccount(account, token);
      this.MoveToAccount(account, 619, 110);
      await this.ClickToAccount(account, 619, 110, 500, token);
      token2.ThrowIfCancellationRequested();
      await this.ClickToAccount(account, 619, 111, 500, token);
      token2.ThrowIfCancellationRequested();
      await Task.Delay(300, token);
      this.MoveToAccount(account, 619, 140);
      await Task.Delay(200, token);
      token2.ThrowIfCancellationRequested();
      await this.ClickToAccount(account, 493, 140, 300, token);
      token2.ThrowIfCancellationRequested();
      await this.ClickToAccount(account, 493, 141, 300, token);
      token2.ThrowIfCancellationRequested();
      await Task.Delay(100, token);
      token2.ThrowIfCancellationRequested();
      await this.ClickToAccount(account, 342, 272, 200, token);
      token2.ThrowIfCancellationRequested();
      await Task.Delay(200, token);
      token2.ThrowIfCancellationRequested();
      WinApi.SendKey(hwnd, 27, false);
      await Task.Delay(100, token);
      token2.ThrowIfCancellationRequested();
    }

    private async Task ApplyBotInvoice(
      Account account,
      IntPtr hwnd,
      int index,
      CancellationToken token,
      CancellationToken token2)
    {
      await this.SetForegroundAccount(account, token);
      this.MoveToAccount(account, 619, 120);
      await Task.Delay(1000, token);
      token2.ThrowIfCancellationRequested();
      int[] numArray = new int[4]{ 521, 553, 585, 615 };
      await this.ClickToAccount(account, numArray[index], 126, 500, token);
      token2.ThrowIfCancellationRequested();
      await Task.Delay(500, token);
      token2.ThrowIfCancellationRequested();
    }

    private async Task SetupCodeToInvoice(
      Account account,
      IntPtr hwnd,
      int index,
      string code,
      CancellationToken token,
      CancellationToken token2)
    {
      //new int[5]{ 110, 115, 150, 180, 210 };
      //new int[5]{ 140, 145, 180, 210, 240 };
      await this.SetForegroundAccount(account, token);
      index = 0;
      this.MoveToAccount(account, 619, 110);
      await Task.Delay(200, token);
      token2.ThrowIfCancellationRequested();
      int y1 = this.Size == SizeEnum.Size2K ? 110 : 115;
      await this.ClickToAccount(account, 619, y1, 200, token);
      token2.ThrowIfCancellationRequested();
      await this.ClickToAccount(account, 619, y1, 200, token);
      token2.ThrowIfCancellationRequested();
      await Task.Delay(300, token);
      token2.ThrowIfCancellationRequested();
      this.MoveToAccount(account, 619, y1 + 20);
      await Task.Delay(200, token);
      token2.ThrowIfCancellationRequested();
      await this.ClickToAccount(account, 486, 140, 300, token);
      token2.ThrowIfCancellationRequested();
      await Task.Delay(500, token);
      token2.ThrowIfCancellationRequested();
      await this.ClickToAccount(account, 285, 232, 500, token);
      token2.ThrowIfCancellationRequested();
      await this.ClickToAccount(account, 290, 230, 500, token);
      token2.ThrowIfCancellationRequested();
      await Task.Delay(500, token);
      token2.ThrowIfCancellationRequested();
      WinApi.SendString(code);
      await Task.Delay(500, token);
      token2.ThrowIfCancellationRequested();
      if (account.Lobby.Size == SizeEnum.SizeFHD)
        await this.ClickToAccount(account, 356, 232, 500, token);
      if (account.Lobby.Size == SizeEnum.Size2K)
        await this.ClickToAccount(account, 364, 232, 500, token);
      token2.ThrowIfCancellationRequested();
      await Task.Delay(1000, token);
      token2.ThrowIfCancellationRequested();
      await this.ClickToAccount(account, 265, 251, 500, token);
      token2.ThrowIfCancellationRequested();
      await Task.Delay(1000, token);
      int y2 = this.Size == SizeEnum.Size2K ? 245 : 225;
      for (int x = 468; x < 518; x += 5)
      {
        await this.ClickToAccount(account, x, y2, 100, token);
        token2.ThrowIfCancellationRequested();
        await Task.Delay(100, token);
        token2.ThrowIfCancellationRequested();
      }
      await Task.Delay(200, token);
      token2.ThrowIfCancellationRequested();
      await this.ClickToAccount(account, 458, 232, 200, token);
      token2.ThrowIfCancellationRequested();
      await Task.Delay(200);
      token2.ThrowIfCancellationRequested();
      await this.ClickToAccount(account, 398, 287, 500, token);
      token2.ThrowIfCancellationRequested();
      await Task.Delay(200, token);
      token2.ThrowIfCancellationRequested();
    }

    public async Task MoveAllAccounts(CancellationToken token)
    {
      foreach (Account account in this.Accounts)
      {
        Process process = AccountHelper.GetProcess(account);
        if (process != null)
        {
          WinApi.ShowWindow(process.MainWindowHandle, 9);
          await this.SetForegroundAccount(account, token, 50);
        }
      }
    }

    public static async Task SendKeyAsync(
      Account account,
      int keyCode,
      int timeBetween,
      CancellationToken token)
    {
      Process process = AccountHelper.GetProcess(account);
      if (process == null)
        return;
      Log.Write(string.Format("Send {0} to {1}", (object) (Keys) keyCode, (object) account.Login));
      await Lobby.SendKeyAsync(process.MainWindowHandle, keyCode, timeBetween, token);
    }

    public static async Task SendKeyAsync(
      IntPtr hwnd,
      int keyCode,
      int timeBetween,
      CancellationToken token)
    {
      uint lParam = (uint) (1 | (int) WinApi.MapVirtualKey((uint) keyCode, 0U) << 16);
      WinApi.PostMessage(hwnd, 256U, (IntPtr) keyCode, (IntPtr) (long) lParam);
      await Task.Delay(timeBetween, token);
      WinApi.PostMessage(hwnd, 257U, (IntPtr) keyCode, (IntPtr) (long) lParam);
    }

    public async Task AssemblyLobby(
      Account leader,
      List<Account> accounts,
      CancellationToken token2)
    {
      Log.Write("AssemblyLobby " + leader.Login);
      CancellationToken token = App.Token;
      IntPtr leaderw = AccountHelper.GetProcess(leader).MainWindowHandle;
      Dictionary<Account, string> dict = new Dictionary<Account, string>();
      int max = 3;
      int index;
      Account bot;
      IntPtr botw;
      for (index = 0; index < accounts.Count; ++index)
      {
        bot = accounts[index];
        string str = bot.SecretCode;
        if (string.IsNullOrEmpty(str))
        {
          Process process = AccountHelper.GetProcess(bot);
          if (process != null)
          {
            botw = process.MainWindowHandle;
            await Task.Delay(200);
            await this.CopyBotNumberToClipboard(bot, botw, token, token2);
            str = Clipboard.GetText();
            Log.Write("Acc: " + bot.Login + " got code " + str);
            if (bot.SecretCode != str)
            {
              bot.SecretCode = str;
              LobbyDb.G.Save();
            }
          }
          else
            continue;
        }
        else
          Log.Write("Acc: " + bot.Login + " had code " + bot.SecretCode);
        dict.Add(bot, str);
        if (index != max)
          bot = (Account) null;
        else
          break;
      }
      for (index = 0; index < accounts.Count; ++index)
      {
        Account account = accounts[index];
        if (AccountHelper.GetProcess(account) != null)
        {
          string code = dict[account];
          await Task.Delay(200);
          await this.SetupCodeToInvoice(leader, leaderw, index, code, token, token2);
          if (index != max)
            code = (string) null;
          else
            break;
        }
      }
      await Task.Delay(1000);
      for (index = 0; index < accounts.Count; ++index)
      {
        bot = accounts[index];
        Process process = AccountHelper.GetProcess(bot);
        if (process != null)
        {
          botw = process.MainWindowHandle;
          await Task.Delay(200);
          await this.ApplyBotInvoice(bot, botw, index, token, token2);
          if (index == max)
          {
            token = new CancellationToken();
            dict = (Dictionary<Account, string>) null;
            return;
          }
          bot = (Account) null;
        }
      }
      token = new CancellationToken();
      dict = (Dictionary<Account, string>) null;
    }

    public void TaskGameState(GameState gs, Lobby lobby)
    {
      if (lobby != this || this._gameStates == null)
        return;
      string str = lobby != null ? " - " + lobby.Name : " - undefined";
      string streamId = gs.Player.SteamID;
      Account key = this.Accounts.FirstOrDefault<Account>((Func<Account, bool>) (o => !string.IsNullOrEmpty(o.Steamid64) && o.Steamid64.Equals(streamId)));
      lock (this._gameStates)
      {
        if (key != null)
        {
          if (this._gameStates.ContainsKey(key))
          {
            this._gameStates[key] = gs;
            if (this._isWaitCycles)
              Log.Write(string.Format("[OnNewGameState{0}] => \ngs.Map.Mode={1}\n{2}", (object) str, (object) gs.Map.Mode, (object) gs.JSON));
          }
        }
      }
      if (string.IsNullOrEmpty(streamId) || !streamId.Equals(this.Accounts[0].Steamid64) && !streamId.Equals(this.Accounts[5].Steamid64))
        return;
      this._mapMode = gs.Map.Mode;
      this._mapPhase = gs.Map.Phase;
      if (this._winLeader != null && this._winLeader.Steamid64.Equals(gs.Player.SteamID))
      {
        this._winTeam = gs.Round.WinTeam;
        this._roundPhase = gs.Round.Phase;
        ++this._gs_counter;
        if (gs.Map.Phase == MapPhase.GameOver)
        {
          Log.Write("[GameState" + str + "] GAME OVER");
          this._isGameOver = true;
        }
        if (gs.Map.Mode == MapMode.Competitive || gs.Map.Mode == MapMode.ScrimComp2v2)
        {
          this._currentRound = gs.Map.Round;
          Log.Write(string.Format("[GameState{0}] Round={1}", (object) str, (object) this._currentRound));
        }
        if (gs.Player.Team == PlayerTeam.T)
        {
          this.score1 = gs.Map.TeamT.Score;
          this.score0 = gs.Map.TeamCT.Score;
        }
        else
        {
          this.score1 = gs.Map.TeamCT.Score;
          this.score0 = gs.Map.TeamT.Score;
        }
        AccountHelper.ScoreChangedHandler(this, new Score()
        {
          T = this.score0,
          CT = this.score1
        });
      }
      if (this._loseLeader == null || !this._loseLeader.Steamid64.Equals(gs.Player.SteamID))
        return;
      if (gs.Player.Team == PlayerTeam.T || gs.Player.Team == PlayerTeam.CT)
        this._loseLobbyIsCT = new bool?(gs.Player.Team == PlayerTeam.CT);
      this._mapPhaseLive = gs.Map.Phase == MapPhase.Live;
      if (gs.Round.Phase == RoundPhase.Undefined)
        return;
      AccountHelper.GameTimeHandler(this, gs.Round.Phase.ToString());
    }

    private async Task Disconnect(List<Account> accounts, CancellationToken token)
    {
      foreach (Account account in accounts.Where<Account>((Func<Account, bool>) (o => o.IsStarted)))
      {
        Log.Write("Disconnect " + account.Login);
        await this.SetForegroundAccount(account, token, 50);
        await Lobby.SendKeyAsync(account, 84, 100, token);
      }
    }

    private async Task Connect(List<Account> accounts, CancellationToken token)
    {
      AccountData[] accs = accounts.Select<Account, AccountData>((Func<Account, AccountData>) (o => o.GetData())).ToArray<AccountData>();
      int num = await Task.Run<int>((Func<int>) (() => CPPImport.Connect(accs, accs.Length, this.Width, this.Height, (IsCancellationRequested) (() => token.IsCancellationRequested), (GetMainWindowHandle) (acc => this.GetWindowHandle(acc)), (Notification) (log => Log.Write(log)))));
    }

    public IntPtr GetWindowHandle(AccountData acc)
    {
      Process process = AccountHelper.GetProcess(acc);
      return process == null ? IntPtr.Zero : process.MainWindowHandle;
    }

    private async Task ClearLogs()
    {
      string path1 = Options.G.CSGOPath + "\\csgo\\log\\";
      if (!System.IO.Directory.Exists(path1))
        System.IO.Directory.CreateDirectory(path1);
      string csgo1log = path1 + "1.log";
      string path2 = path1 + "2.log";
      if (!File.Exists(csgo1log))
        File.Create(csgo1log).Close();
      if (!File.Exists(path2))
        File.Create(path2).Close();
      FileStream fileStream_clear_leader2 = new FileStream(path2, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite);
      StreamWriter sw_leader2;
      try
      {
        sw_leader2 = new StreamWriter((Stream) fileStream_clear_leader2);
        try
        {
          await sw_leader2.WriteAsync("");
          this.logPos1 = 0;
        }
        finally
        {
          sw_leader2?.Dispose();
        }
        sw_leader2 = (StreamWriter) null;
      }
      finally
      {
        fileStream_clear_leader2?.Dispose();
      }
      fileStream_clear_leader2 = (FileStream) null;
      fileStream_clear_leader2 = new FileStream(csgo1log, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite);
      try
      {
        sw_leader2 = new StreamWriter((Stream) fileStream_clear_leader2);
        try
        {
          await sw_leader2.WriteAsync("");
          this.logPos2 = 0;
        }
        finally
        {
          sw_leader2?.Dispose();
        }
        sw_leader2 = (StreamWriter) null;
      }
      finally
      {
        fileStream_clear_leader2?.Dispose();
      }
      fileStream_clear_leader2 = (FileStream) null;
      this._needRestart1 = 0;
      this._needRestart2 = 0;
      csgo1log = (string) null;
    }

    public async Task<Lobby.ReadLogsResult> ReadLogs()
    {
      Lobby.ReadLogsResult res = new Lobby.ReadLogsResult();
      string path1 = Options.G.CSGOPath + "\\csgo\\log\\";
      if (!System.IO.Directory.Exists(path1))
        System.IO.Directory.CreateDirectory(path1);
      string path2 = path1 + "1.log";
      string csgo2log = path1 + "2.log";
      if (!File.Exists(path2) || !File.Exists(csgo2log))
        return res;
      FileStream fileStream_leader1 = new FileStream(path2, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
      StreamReader reader_leader1;
      try
      {
        reader_leader1 = new StreamReader((Stream) fileStream_leader1);
        try
        {
          string input = await reader_leader1.ReadToEndAsync();
          int length = input.Length;
          if (this.logPos1 > 0)
            input = input.Substring(this.logPos1);
          for (Match match = Regex.Match(input, Lobby.Match1); match.Success; match = match.NextMatch())
          {
            if (match.Groups.Count >= 2)
              res.MatchIdLeader1 = match.Groups[1].Value;
          }
          for (Match match = Regex.Match(input, Lobby.Match2); match.Success; match = match.NextMatch())
          {
            if (match.Groups.Count >= 3)
              res.McLeader1 = match.Groups[2].Value;
          }
          this.logPos1 = length;
        }
        finally
        {
          reader_leader1?.Dispose();
        }
        reader_leader1 = (StreamReader) null;
      }
      finally
      {
        fileStream_leader1?.Dispose();
      }
      fileStream_leader1 = (FileStream) null;
      fileStream_leader1 = new FileStream(csgo2log, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
      try
      {
        reader_leader1 = new StreamReader((Stream) fileStream_leader1);
        try
        {
          string input = await reader_leader1.ReadToEndAsync();
          int length = input.Length;
          if (this.logPos2 > 0)
            input = input.Substring(this.logPos2);
          for (Match match = Regex.Match(input, Lobby.Match1); match.Success; match = match.NextMatch())
          {
            if (match.Groups.Count >= 2)
              res.MatchIdLeader2 = match.Groups[1].Value;
          }
          for (Match match = Regex.Match(input, Lobby.Match2); match.Success; match = match.NextMatch())
          {
            if (match.Groups.Count >= 3)
              res.McLeader2 = match.Groups[2].Value;
          }
          this.logPos2 = length;
        }
        finally
        {
          reader_leader1?.Dispose();
        }
        reader_leader1 = (StreamReader) null;
      }
      finally
      {
        fileStream_leader1?.Dispose();
      }
      fileStream_leader1 = (FileStream) null;
      return res;
    }

    private GameState GetGameState(Account acc)
    {
      lock (this._gameStates)
      {
        if (this._gameStates.ContainsKey(acc))
          return this._gameStates[acc];
      }
      return new GameState("{}");
    }

    public async Task TaskCycles(CancellationToken token)
    {
      Lobby lobby1 = this;
      string lobbyName = "";
      bool _nextCycle = false;
      List<Account> team1 = null;
      List<Account> team2 = null;
      List<Account>[] teams = null;
      bool onelobby = false;
      int maxScores = 0;
      bool? currentLobbyIsCTStart;
      int num1 = 0;
      if ((uint) (num1 - 1) > 16U)
      {
        if (lobby1._gameStates == null)
          lobby1._gameStates = new Dictionary<Account, GameState>();
        lobby1._gameStates.Clear();
        foreach (Account account in lobby1.Accounts)
          lobby1._gameStates.Add(account, new GameState("{}"));
        lobbyName = "[TaskCycles - " + lobby1.Name + "] ";
        lobby1.IsNeedRestartCycles = false;
        lobby1._gs_counter = 0;
        lobby1._loseLeader = (Account) null;
        lobby1._loseLobbyIsCT = new bool?();
        AccountHelper.CyclesStartedHandler(lobby1);
        await Task.Delay(4000, token);
        _nextCycle = false;
        team1 = lobby1.Accounts.Take<Account>(5).Where<Account>((Func<Account, bool>) (o => o.IsStarted)).ToList<Account>();
        team2 = lobby1.Accounts.Skip<Account>(5).Take<Account>(5).Where<Account>((Func<Account, bool>) (o => o.IsStarted)).ToList<Account>();
        teams = new List<Account>[2]{ team1, team2 };
        onelobby = lobby1.IsLobby0Only != lobby1.IsLobby1Only && (lobby1.IsLobby0Only || lobby1.IsLobby1Only);
        lobby1.score0 = 0;
        lobby1.score1 = 0;
        lobby1._isGameOver = false;
        lobby1._currentRound = 0;
        lobby1._mapMode = MapMode.Undefined;
        lobby1._mapPhase = MapPhase.Undefined;
        if (!lobby1.IsLobby0Only && !lobby1.IsLobby1Only)
          onelobby = false;
        maxScores = lobby1.Accounts.Count<Account>((Func<Account, bool>) (o => o.IsStarted)) == 4 ? 8 : 15;
        if (onelobby)
          ++maxScores;
        currentLobbyIsCTStart = new bool?();
      }
      try
      {
        lobby1._isWaitCycles = true;
        //Uniso.Log.Write(lobbyName + " - Started cycles!!!");
        while (lobby1._mapMode != MapMode.Competitive && lobby1._mapMode != MapMode.ScrimComp2v2 || lobby1._mapPhase != MapPhase.Live)
        {
          Log.Write(string.Format("{0}Map mode = {1} phase = {2}", (object) lobbyName, (object) lobby1._mapMode, (object) lobby1._mapPhase));
          if (!lobby1._isWaitCycles)
          {
            Log.Write("[TaskCycles] EXIT _isWaitCycles 1");
            lobbyName = (string) null;
            team1 = (List<Account>) null;
            team2 = (List<Account>) null;
            teams = (List<Account>[]) null;
            return;
          }
          await Task.Delay(100, token);
        }
        Log.Write(string.Format("{0}Map mode = {1} phase = {2} correct", (object) lobbyName, (object) lobby1._mapMode, (object) lobby1._mapPhase));
        int timerDisconnect = Options.G.TimerDisconnect;
        int timerConnect = Options.G.TimerConnect;
        int index = -1;
        List<Account> team = teams[0];
        if (onelobby && lobby1.IsLobby1Only)
          team = teams[1];
        ++index;
        lobby1._loseLeader = team[0];
        lobby1._winLeader = team1[0] == lobby1._loseLeader ? team2[0] : team1[0];
        while (!lobby1._loseLobbyIsCT.HasValue)
        {
          Log.Write(string.Format("{0}_currentLobbyIsCT = {1}", (object) lobbyName, (object) lobby1._loseLobbyIsCT));
          if (!lobby1._isWaitCycles)
          {
            Log.Write("[TaskCycles] EXIT _isWaitCycles 2");
            lobbyName = (string) null;
            team1 = (List<Account>) null;
            team2 = (List<Account>) null;
            teams = (List<Account>[]) null;
            return;
          }
          await Task.Delay(100, token);
        }
        Log.Write(string.Format("{0}_currentLobbyIsCT = {1}", (object) lobbyName, (object) lobby1._loseLobbyIsCT));
        currentLobbyIsCTStart = lobby1._loseLobbyIsCT;
        Log.Write(lobbyName + "Cycles for:");
        foreach (Account account in team)
          Log.Write(lobbyName + " - " + account.Login + " " + account.Steamid64);
label_96:
        while (lobby1._isWaitCycles && !token.IsCancellationRequested)
        {
          if (!lobby1.IsAutoDisconnectLobbies)
          {
            if (lobby1._mapMode == MapMode.Competitive || lobby1._mapMode == MapMode.ScrimComp2v2)
            {
              if (onelobby)
              {
                if (lobby1.score1 >= maxScores || lobby1._isGameOver)
                {
                  _nextCycle = Options.G.IsDisconnectsLoop;
                  ++lobby1._gamesCounter;
                  Log.Write(string.Format("[TaskCycles] EXIT 4 index={0} gameover={1} {2}-{3}", (object) index, (object) lobby1._isGameOver, (object) lobby1.score0, (object) lobby1.score1));
                  lobbyName = (string) null;
                  team1 = (List<Account>) null;
                  team2 = (List<Account>) null;
                  teams = (List<Account>[]) null;
                  return;
                }
              }
              else if (lobby1.score0 >= maxScores && lobby1.score1 >= maxScores || lobby1._isGameOver)
              {
                _nextCycle = Options.G.IsDisconnectsLoop;
                ++lobby1._gamesCounter;
                Log.Write(string.Format("[TaskCycles] EXIT 5 index={0} gameover={1} {2}-{3}", (object) index, (object) lobby1._isGameOver, (object) lobby1.score0, (object) lobby1.score1));
                lobbyName = (string) null;
                team1 = (List<Account>) null;
                team2 = (List<Account>) null;
                teams = (List<Account>[]) null;
                return;
              }
            }
            await Task.Delay(1000);
          }
          else
          {
            while (lobby1._mapMode != MapMode.Competitive && lobby1._mapMode != MapMode.ScrimComp2v2 || lobby1._mapPhase != MapPhase.Live)
            {
              Log.Write(string.Format("{0}Map mode = {1} phase = {2}", (object) lobbyName, (object) lobby1._mapMode, (object) lobby1._mapPhase));
              if (!lobby1._isWaitCycles)
              {
                Log.Write("[TaskCycles] EXIT _isWaitCycles 3");
                lobbyName = (string) null;
                team1 = (List<Account>) null;
                team2 = (List<Account>) null;
                teams = (List<Account>[]) null;
                return;
              }
              await Task.Delay(100, token);
            }
            Log.Write(string.Format("{0}Map mode = {1} phase = {2} correct", (object) lobbyName, (object) lobby1._mapMode, (object) lobby1._mapPhase));
            int sc0 = 0;
            int sc1 = 0;
            await lobby1.Disconnect(team, token);
            Log.Write(">>!Disconnected all");
            int score_counter;
            for (score_counter = 10; sc0 == lobby1.score0 && sc1 == lobby1.score1 && lobby1._isWaitCycles && !lobby1._isGameOver && score_counter >= 0; --score_counter)
              await Task.Delay(1000, token);
            sc0 = lobby1.score0;
            sc1 = lobby1.score1;
            int index_acc = 0;
            while (true)
            {
              if (lobby1._isWaitCycles && !token.IsCancellationRequested)
              {
                Account acc_current = team[index_acc];
                int i;
                for (i = 0; i < Options.G.TimerBeforeConnect; i += 100)
                {
                  await Task.Delay(100, token);
                  if (!lobby1._isWaitCycles || lobby1._isGameOver)
                    break;
                }
                Lobby lobby2 = lobby1;
                List<Account> accounts1 = new List<Account>();
                accounts1.Add(acc_current);
                CancellationToken token1 = token;
                await lobby2.Connect(accounts1, token1);
                Log.Write(">>>Connected " + team[index_acc].Login);
                if (Options.G.TimerInsteadConnect == 0)
                {
                  while (lobby1._isWaitCycles && !token.IsCancellationRequested && !lobby1._isGameOver)
                  {
                    GameState gameState = lobby1.GetGameState(acc_current);
                    if (lobby1._winTeam == RoundWinTeam.Undefined && gameState.Map.Phase == MapPhase.Live && gameState.Player.Activity == PlayerActivity.Playing && (gameState.Round.Phase == RoundPhase.Live || gameState.Round.Phase == RoundPhase.FreezeTime))
                    {
                      Log.Write(string.Format("After connect gs.Map.Phase={0} gs.Player.Activity={1} gs.Round.Phase={2}", (object) gameState.Map.Phase, (object) gameState.Player.Activity, (object) gameState.Round.Phase));
                      if (gameState.Round.Phase == RoundPhase.FreezeTime)
                      {
                        await Task.Delay(Options.G.TimerConnect, token);
                        break;
                      }
                      break;
                    }
                    await Task.Delay(100, token);
                  }
                }
                else
                {
                  for (i = 0; i < Options.G.TimerInsteadConnect; i += 100)
                  {
                    await Task.Delay(100, token);
                    if (!lobby1._isWaitCycles || lobby1._isGameOver)
                      break;
                  }
                }
                int num2 = onelobby || index < 1 || lobby1.score0 < maxScores ? 0 : (lobby1.score1 >= maxScores ? 1 : 0);
                bool flag1 = index == 0 && (lobby1.score0 >= maxScores || lobby1.score1 >= maxScores) && !lobby1._isGameOver && !onelobby;
                if (num2 == 0 && !flag1)
                {
                  Lobby lobby3 = lobby1;
                  List<Account> accounts2 = new List<Account>();
                  accounts2.Add(acc_current);
                  CancellationToken token2 = token;
                  await lobby3.Disconnect(accounts2, token2);
                  Log.Write(">>>Disconnected " + team[index_acc].Login);
                  for (score_counter = 10; sc0 == lobby1.score0 && sc1 == lobby1.score1 && lobby1._isWaitCycles && !lobby1._isGameOver && score_counter >= 0; --score_counter)
                  {
                    Log.Write(string.Format("After diconnect score0={0} score1={1}", (object) lobby1.score0, (object) lobby1.score1));
                    await Task.Delay(1000, token);
                  }
                  sc0 = lobby1.score0;
                  sc1 = lobby1.score1;
                  ++index_acc;
                  if (index_acc >= team.Count)
                    index_acc = 0;
                }
                bool flag2 = !onelobby && index >= 1 && lobby1.score0 >= maxScores && lobby1.score1 >= maxScores;
                Log.Write(string.Format("{0}Wait cond3={1} score0 == {2} && score1 == {3} maxScores={4} && _currentLobbyIsCT={5} currentLobbyIsCTStart={6}", (object) lobbyName, (object) flag2, (object) lobby1.score0, (object) lobby1.score1, (object) maxScores, (object) lobby1._loseLobbyIsCT, (object) currentLobbyIsCTStart));
                if (!flag2)
                {
                  if (lobby1._isWaitCycles)
                  {
                    if (!lobby1._isGameOver)
                    {
                      Log.Write(string.Format("{0}[TaskCycles]0 Score {1}-{2} Round={3} index={4}", (object) lobbyName, (object) lobby1.score0, (object) lobby1.score1, (object) lobby1._currentRound, (object) index));
                      if (index == 0 && (lobby1.score0 >= maxScores || lobby1.score1 >= maxScores) && !lobby1._isGameOver && !onelobby)
                      {
                        await lobby1.Connect(team, token);
                        Log.Write(">>!Connected all");
                        team = teams[1];
                        ++index;
                        lobby1._loseLeader = team[0];
                        lobby1._winLeader = team1[0] == lobby1._loseLeader ? team2[0] : team1[0];
                        Log.Write(lobbyName + " Cycles for:");
                        foreach (Account account in team)
                          Log.Write(lobbyName + " - " + account.Login + " " + account.Steamid64);
                        for (i = 0; i < 15000; i += 100)
                        {
                          if (!lobby1._isWaitCycles)
                          {
                            Log.Write("[TaskCycles] EXIT _isWaitCycles 5");
                            lobbyName = (string) null;
                            team1 = (List<Account>) null;
                            team2 = (List<Account>) null;
                            teams = (List<Account>[]) null;
                            return;
                          }
                          await Task.Delay(100, token);
                        }
                        await lobby1.Disconnect(team, token);
                        Log.Write(">>!Disconnected all");
                        for (score_counter = 10; sc0 == lobby1.score0 && sc1 == lobby1.score1 && lobby1._isWaitCycles && !lobby1._isGameOver && score_counter >= 0; --score_counter)
                          await Task.Delay(1000, token);
                        sc0 = lobby1.score0;
                        sc1 = lobby1.score1;
                        index_acc = 0;
                      }
                      acc_current = (Account) null;
                    }
                    else
                      goto label_78;
                  }
                  else
                    goto label_76;
                }
                else
                  break;
              }
              else
                goto label_96;
            }
            _nextCycle = Options.G.IsDisconnectsLoop;
            ++lobby1._gamesCounter;
            Log.Write(string.Format("[TaskCycles] EXIT cond1 index={0} gameover={1} {2}-{3}", (object) index, (object) lobby1._isGameOver, (object) lobby1.score0, (object) lobby1.score1));
            lobbyName = (string) null;
            team1 = (List<Account>) null;
            team2 = (List<Account>) null;
            teams = (List<Account>[]) null;
            return;
label_76:
            Log.Write("[TaskCycles] EXIT _isWaitCycles 4");
            lobbyName = (string) null;
            team1 = (List<Account>) null;
            team2 = (List<Account>) null;
            teams = (List<Account>[]) null;
            return;
label_78:
            ++lobby1._gamesCounter;
            _nextCycle = Options.G.IsDisconnectsLoop;
            Log.Write("[TaskCycles] EXIT GAMEOVER");
            lobbyName = (string) null;
            team1 = (List<Account>) null;
            team2 = (List<Account>) null;
            teams = (List<Account>[]) null;
            return;
          }
        }
        team = (List<Account>) null;
      }
      finally
      {
        lobby1._loseLeader = (Account) null;
        lobby1._winLeader = (Account) null;
        lobby1._isWaitCycles = false;
        lobby1.IsNeedRestartCycles = _nextCycle;
        Log.Write(string.Format("Before end cycles {0}", (object) lobby1.IsNeedRestartCycles));
        AccountHelper.CyclesEndedHandler(lobby1, lobby1.IsNeedRestartCycles);
        Log.Write("Ended cycles");
      }
      currentLobbyIsCTStart = new bool?();
      lobbyName = (string) null;
      team1 = (List<Account>) null;
      team2 = (List<Account>) null;
      teams = (List<Account>[]) null;
    }

    private async Task TaskApply(CancellationToken token, bool is_disconnect = false)
    {
      Lobby lobby = this;
      if (!lobby._isWaitAutoApply)
        return;
      Lobby.ReadLogsResult parse = await lobby.ReadLogs();
      int count = lobby.Accounts.Count<Account>((Func<Account, bool>) (o => o.IsStarted));
      Log.Write(string.Format("[TaskApply] => Count={0} matchid_leader1={1} matchid_leader2={2} mc1={3} mc2={4}", (object) count, (object) parse.MatchIdLeader1, (object) parse.MatchIdLeader2, (object) parse.McLeader1, (object) parse.McLeader2));
      bool flag = !string.IsNullOrEmpty(parse.MatchIdLeader1);
      bool lobby2 = !string.IsNullOrEmpty(parse.MatchIdLeader2);
      bool serverParity = !string.IsNullOrEmpty(parse.MatchIdLeader1) && parse.MatchIdLeader1.Equals(parse.MatchIdLeader2);
      Log.Write(string.Format("lobby1={0} lobby2={1} serverParity={2} _needRestart1={3} _needRestart2={4} logPos1={5} logPos2={6}", (object) flag, (object) flag, (object) serverParity, (object) lobby._needRestart1, (object) lobby._needRestart2, (object) lobby.logPos1, (object) lobby.logPos2));
      if (lobby._needRestart1 > 0)
        --lobby._needRestart1;
      if (!serverParity & flag && lobby._needRestart1 == 0)
      {
        lobby._needRestart1 = 8;
        Log.Write("[TaskApply] Need to restart lobby 1");
      }
      if (lobby._needRestart1 == 1 && !flag)
      {
        lobby._needRestart1 = 0;
        Log.Write("[TaskApply] RestartLeader 1");
        await lobby.RestartLeader(lobby.Leader1);
      }
      else
      {
        bool g;
        bool r;
        lobby.GetDetectButtonState(lobby.Leader1, Lobby.pointButtonSearch.X, Lobby.pointButtonSearch.Y, out g, out r);
        if (g || !r)
        {
          await lobby.RestartLeader(lobby.Leader1);
          return;
        }
      }
      if (lobby._needRestart2 > 0)
        --lobby._needRestart2;
      if (!serverParity & lobby2 && lobby._needRestart2 == 0)
      {
        lobby._needRestart2 = 8;
        Log.Write("[TaskApply] Need to restart lobby 2");
      }
      if (lobby._needRestart2 == 1 && !lobby2)
      {
        lobby._needRestart2 = 0;
        Log.Write("[TaskApply] RestartLeader 2");
        await lobby.RestartLeader(lobby.Leader2);
      }
      else
      {
        bool g;
        bool r;
        lobby.GetDetectButtonState(lobby.Leader2, Lobby.pointButtonSearch.X, Lobby.pointButtonSearch.Y, out g, out r);
        if (g || !r)
        {
          await lobby.RestartLeader(lobby.Leader2);
          return;
        }
      }
      if (("5".Equals(parse.McLeader1) && "5".Equals(parse.McLeader2) && count == 10 || "2".Equals(parse.McLeader1) && "2".Equals(parse.McLeader2) && count == 4) && !string.IsNullOrEmpty(parse.MatchIdLeader1) && parse.MatchIdLeader1.Equals(parse.MatchIdLeader2))
      {
        Log.Write("CLICK to APPLY");
        await Task.Delay(3500, token);
        WinApi.POINT lpPoint;
        WinApi.GetCursorPos(out lpPoint);
        foreach (Account account in lobby.Accounts.Where<Account>((Func<Account, bool>) (o => o.IsStarted)))
        {
          await lobby.SetForegroundAccount(account, token);
          await lobby.ClickToAccount(account, 320, 268, 100, token);
        }
        lobby._isWaitAutoApply = false;
        AccountHelper.AcceptedHandler(lobby);
        WinApi.SetCursorPos(lpPoint.X, lpPoint.Y);
        if (is_disconnect)
          await lobby.TaskCycles(token);
      }
      parse = (Lobby.ReadLogsResult) null;
    }

    private void RecalcPositions(Account account, int x, int y, out int clickX, out int clickY)
    {
      x = Convert.ToInt32((float) x / 640f * (float) account.Lobby.Width);
      y = Convert.ToInt32((float) y / 480f * (float) account.Lobby.Height);
      int num1 = (account.W - account.Lobby.Width) / 2;
      int num2 = account.H - account.Lobby.Height - num1;
      clickX = x + num1 + account.X;
      clickY = y + num2 + account.Y;
      if (num1 < 0 || num2 < 0)
        throw new Exception("Необходимо перезапустить аккаунты, т.к. размеры окон не соответствуют заданным в настройках CSGO Boost");
    }

    private void MoveToAccount(Account account, int x, int y, out int clickX, out int clickY)
    {
      int clickX1;
      int clickY1;
      this.RecalcPositions(account, x, y, out clickX1, out clickY1);
      clickX = clickX1;
      clickY = clickY1;
      WinApi.SetCursorPos(clickX1, clickY1);
      Log.Write(string.Format("Move to {0} Pos={1},{2},{3},{4} Sz={5},{6}", (object) account.Login, (object) x, (object) y, (object) account.W, (object) account.H, (object) account.Lobby.Width, (object) account.Lobby.Height));
    }

    private void MoveToAccount(Account account, int x, int y) => this.MoveToAccount(account, x, y, out int _, out int _);

    private async Task ClickToAccount(
      Account account,
      int x,
      int y,
      int timeBetweenPosAndClick,
      CancellationToken token,
      int timeOfClick = 20)
    {
      for (int i = 0; i < 3; ++i)
      {
        int cx;
        int cy;
        this.MoveToAccount(account, x, y, out cx, out cy);
        if (timeBetweenPosAndClick > 0)
          await Task.Delay(timeBetweenPosAndClick, token);
        WinApi.POINT lpPoint;
        if (WinApi.GetCursorPos(out lpPoint) || Math.Abs(lpPoint.X - cx) < 3 && Math.Abs(lpPoint.Y - cy) < 3)
          break;
      }
      WinApi.mouse_event(2U, 0U, 0U, 0U, 0U);
      await Task.Delay(timeOfClick, token);
      WinApi.mouse_event(4U, 0U, 0U, 0U, 0U);
      Log.Write("Click");
    }

    private async Task ClickRToAccount(
      Account account,
      int x,
      int y,
      int timeBetweenPosAndClick,
      CancellationToken token)
    {
      this.MoveToAccount(account, x, y);
      if (timeBetweenPosAndClick > 0)
        await Task.Delay(timeBetweenPosAndClick, token);
      WinApi.mouse_event(8U, 0U, 0U, 0U, 0U);
      WinApi.mouse_event(16U, 0U, 0U, 0U, 0U);
    }

    public enum LobbyKindEnum
    {
      x55,
      x22,
    }

    public class ReadLogsResult
    {
      public string MatchIdLeader1 { get; set; }

      public string MatchIdLeader2 { get; set; }

      public string McLeader1 { get; set; }

      public string McLeader2 { get; set; }
    }
}