using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CSWPF.Helpers;
using CSWPF.Web;
using Newtonsoft.Json;
using SteamKit2;

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
    [JsonProperty("shared_secret")]
    public string SharedSecret { get; set; }
    public DateTime DateTime { get; set; } = DateTime.Now;

    public User(string login, string password)
    {
        Login = login;
        Password = password;
    }

    public void ClickPassword(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(Password);
    }

    public void ClickStart(object sender, RoutedEventArgs e)
    {
        /*await Task.Run(() => { }); */
        new Thread(() =>
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = "D:\\Steam\\",
                FileName = "D:\\Steam\\steam.exe",
                Arguments = $" -login {Login} {Password} -applaunch 730 -w 640 -h 480",
            };
            Process process1 = new Process()
            {
                StartInfo = startInfo
            };
            process1.Start();
        }).Start();
        //SteamCode dsa = new SteamCode();
        //dsa.SteamCodeEnter(this);
    }

    //https://steamcommunity.com/inventory/76561199198508752/730/2/?l=english
    public void ClickCheckInventory(object sender, RoutedEventArgs e)
    {
        //maFile ss = Helper.ReadFile("D:\\Game\\SteamSDA\\maFiles",Login);
        //Helper.CheckInventory(ss.Session.SteamId);
    }
    
}