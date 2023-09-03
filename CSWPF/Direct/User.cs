using CSWPF.Directory.Models;
using CSWPF.Helpers;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Windows;
using CSWPF.Boost.Models;

namespace CSWPF.Directory;

[DataContract]
[Serializable]
public class User
{
    [DataMember(Name = "Login")]
    public string Login { get; set; }
    [DataMember(Name = "Password")]
    public string Password { get; set; }
    [JsonProperty("SteamID")]
    public ulong SteamID { get; set; }
    [DataMember(Name = "Sid")]
    public ulong SID { get; set; }
    [JsonProperty("shared_secret")]
    public string SharedSecret { get; set; }
    [JsonProperty("prime")]
    public bool Prime { get; set; }
    public DateTime DateTime { get; set; } = DateTime.Now;

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

    public User(Lobby lobby) => this._lobby = lobby;

    public int Index => Lobby.Users.IndexOf(this);

    public override string ToString() => this.Login;

    public User(string login, string password)
    {
        Login = login;
        Password = password;
    }

    [JsonConstructor]
    public User() { }

    public void ClickLogin(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(Login);
    }

    public void ClickPassword(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(Password);
    }

    public void ClickStart(object sender, RoutedEventArgs e)
    {
        HelperCS.Start(this);
    }

    public void ClickOpenSteam(object sender, RoutedEventArgs e)
    {
        TimerKill.OpenSteam($"{Settings.SteamPath}steam_{SteamID}.exe");
    }

    public void ClickKill(object sender, RoutedEventArgs e)
    {
        TimerKill.KillSteam(SteamID);
    }

    //https://steamcommunity.com/inventory/76561199198508752/730/2?l=english&count=5000
    //https://steamcommunity.com/inventory/76561199198508752/730/2/?l=english
    public void ClickCheckInventory(object sender, RoutedEventArgs e)
    {
        Inventory.CheckInventory(this);
    }

    public void CheckPrime(object sender, RoutedEventArgs e)
    {
        var users = JsonConvert.DeserializeObject<User>(File.ReadAllText(System.IO.Directory.GetCurrentDirectory() + @"\Account\" + Login + ".json"));
        users.Prime = !Prime;
        File.WriteAllText(System.IO.Directory.GetCurrentDirectory() + @"\Account\" + users.Login + ".json", JsonConvert.SerializeObject(users));
    }

    public void CheckAccount()
    {
        if (SharedSecret == null)
        {
            foreach (string filename in System.IO.Directory.GetFiles($"{Settings.SDA}maFiles", "*.maFile"))
            {
                var currentUsers = JsonConvert.DeserializeObject<maFile>(File.ReadAllText(filename));
                if (currentUsers.AccountName == Login)
                {
                    SharedSecret = currentUsers.SharedSecret;
                }
            }
        }

        var users = JsonConvert.DeserializeObject<User>(File.ReadAllText(System.IO.Directory.GetCurrentDirectory() + @"\Account\" + Login + ".json"));
        users.SID = users.SteamID - 76561197960265728L;
        File.WriteAllText(System.IO.Directory.GetCurrentDirectory() + @"\Account\" + Login + ".json", JsonConvert.SerializeObject(this));
    }

    public void ClickPrime(object sender, RoutedEventArgs e)
    {
        File.Exists(System.IO.Directory.GetCurrentDirectory() + "Prime.txt");
    }

}