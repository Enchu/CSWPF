using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using CSWPF.CSB;
using CSWPF.Helpers.Data;

namespace CSWPF.Directory.Models;

public class Account : UserCS
{
    [DataMember(Name = "isuse")]
    private bool _isUse = true;
    private Lobby _lobby;
    [NonSerialized]
    private Stopwatch _timerAlive;
    [NonSerialized]
    private bool _isStarted;
    [NonSerialized]
    private bool _isStartedSteam;
    [NonSerialized]
    private bool _waitToClick1;


    public AccountData GetData() => new AccountData()
    {
        h = this.H,
        isLeader = this.IsLeader,
        isStarted = this.IsStarted,
        isStartedSteam = this.IsStartedSteam,
        isUse = this.IsUse,
        login = this.Login,
        passw = this.Password,
        sid = this.SID,
        steamid64 = this.Steamid64,
        w = this.W,
        x = this.X,
        y = this.Y,
        secretCode = this.SecretCode
    };
        
    [DataMember(Name = "isleader")]
    public bool IsLeader { get; set; }

    public bool IsUse
    {
        get => this._isUse && !string.IsNullOrEmpty(this.Login);
        set => this._isUse = value;
    } 
        
    [DataMember(Name = "x")]
    public int X { get; set; }

    [DataMember(Name = "y")]
    public int Y { get; set; }

    [DataMember(Name = "w")]
    public int W { get; set; }

    [DataMember(Name = "h")]
    public int H { get; set; }

    [DataMember(Name = "steamid64")]
    public string Steamid64 { get; set; }

    [DataMember(Name = "sud")]
    public long SID { get; set; }

    [DataMember(Name = "secretcode")]
    public string SecretCode { get; set; }

    [DataMember(Name = "info")]
    public string Info { get; set; }

    [DataMember(Name = "accountsecretcode")]
    public string AccountSecretCode { get; set; }

    [DataMember(Name = "guardcode")]
    public string AccountSteamGuardCode { get; set; }

    public Lobby Lobby
    {
        get => this._lobby;
        set => this._lobby = value;
    }
    
    [DataMember(Name = "isstarted")]
    public bool IsStarted
    {
        get => this._isStarted;
        set
        {
            if (this._isStarted == value)
                return;
            this._isStarted = value;
            if (this._isStarted)
            {
                if (this._timerAlive == null)
                    this._timerAlive = new Stopwatch();
                this._timerAlive.Restart();
            }
            else
                this._timerAlive?.Stop();
        }
    }

    [DataMember(Name = "isstartedstream")]
    public bool IsStartedSteam
    {
        get => this._isStartedSteam;
        set
        {
            if (this._isStartedSteam == value)
                return;
            this._isStartedSteam = value;
        }
    }

    public int TimeAlive => this._timerAlive != null && this._timerAlive.IsRunning ? Convert.ToInt32(this._timerAlive.ElapsedMilliseconds) : 0;

    public bool WaitToClick1
    {
        get => this._waitToClick1;
        set => this._waitToClick1 = value;
    }

    public Account(Lobby lobby) => this._lobby = lobby;

    public int Index => this._lobby.Accounts.IndexOf(this);

    public override string ToString() => this.Login;
}