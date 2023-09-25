using CSWPF.Directory.Models;
using CSWPF.Helpers;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Windows;

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
        HelperCS.Start(this, 0 , 0);
    }
    
    public void CsClick(object sender, RoutedEventArgs e)
    {
        AccountHelper.Pipe(Login, "start");
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