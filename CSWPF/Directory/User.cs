using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CSWPF.Helpers;
using CSWPF.Web;
using Newtonsoft.Json;

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
    [JsonProperty("prime")]
    public bool Prime { get; set; }
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
        if(!File.Exists($"{Settings.SteamPath}steam_{SteamID}.exe"))
            File.Copy($"{Settings.SteamFullPath}",$"{Settings.SteamPath}steam_{SteamID}.exe");
        new Thread(() =>
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = $"{Settings.SteamPath}",
                FileName = $"{Settings.SteamPath}steam_{SteamID}.exe",
                Arguments = $" -login {Login} {Password} {Settings.ConfigBot}",
            };
            Process process1 = new Process()
            {
                StartInfo = startInfo
            };
            process1.Start();
        }).Start();
        
        SteamCode.SteamCodeEnter(this);
    }

    //https://steamcommunity.com/inventory/76561199198508752/730/2/?l=english
    public void ClickCheckInventory(object sender, RoutedEventArgs e)
    {
        
    }

    public void CheckAccount()
    {
        if (SharedSecret == null)
        {
            foreach (string filename in System.IO.Directory.GetFiles(@"D:\Game\SteamSDA\maFiles", "*.maFile"))
            {
                var currentUsers = JsonConvert.DeserializeObject<maFile>(File.ReadAllText(filename));
                if (currentUsers.AccountName == Login)
                {
                    SharedSecret = currentUsers.SharedSecret;
                }
            }

            File.WriteAllText(System.IO.Directory.GetCurrentDirectory()+ @"\Account\" + Login + ".json", JsonConvert.SerializeObject(this));
        }
    }
    
}