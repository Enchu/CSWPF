using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CSWPF.Helpers;
using CSWPF.Utils;
using System.Linq;
using CSWPF.Steam;
using Newtonsoft.Json;
using Confirmation = CSWPF.Steam.Security.Confirmation;

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

    public void ClickPassword(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(Password);
    }

    public void ClickStart(object sender, RoutedEventArgs e)
    {
        StartCS();
    }

    public void StartCsLauncher()
    {
        ulong result = SteamID;
        result -= 76561197960265728L;
        //Settings
        Settings.ExchangeCfg(result);
        Settings.FixAutoexec(result);
        //Start
        string str1 = System.IO.Directory.GetCurrentDirectory() + "/Launcher/Launcher.exe";
        string str2 = Options.G.IsHideLauncher ? "1" : "0";
        string str3 = " \"" + Login + "\"" + (" \"" + Login + "\"") + (" \"" + Password + "\"") + (" \"" + Settings.ConfigGame + "\"") + (" " + SteamID) + (" \"" + "princeenchu" + "\"") + (" \"" + "princetankist5" + "\"") + string.Format(" {0}", (object) Settings.X) + string.Format(" {0}", (object) Settings.Y) + " 640" + " 480" + (" " + str2) + (" \"" + Settings.SteamPath + "\"");
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
        //SteamCode
        SteamCode.SteamCodeEnter(this);
    }

    public void StartCS()
    {
        ulong result = SteamID;
        result -= 76561197960265728L;
        //Settings
        Settings.ExchangeCfg(result);
        Settings.FixAutoexec(result);
        //Start
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
                Arguments = $" -login {Login} {Password} {Settings.ConfigBot} {Settings.ConfigGame} {Settings.StartSteam}",
            }; //-noverifyfiles -nofriendsui //-forceservice ?? Run Steam Client Service even if Steam has admin rights.
            Process process1 = new Process()
            {
                StartInfo = startInfo
            };
            process1.Start();
        }).Start();
        //SteamCode
        SteamCode.SteamCodeEnter(this);
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
        CheckInventory();
    }
    
    public async Task CheckInventory()
    {
        IReadOnlyCollection<InventoryResponseCS.Asset> inventory;
        var users = JsonConvert.DeserializeObject<User>(File.ReadAllText(System.IO.Directory.GetCurrentDirectory()+ @"\Account\" + Login + ".json"));
        Bot newBot = new Bot(users);
        await newBot.Start();
        await Task.Delay(30000);
        inventory = await newBot.WebHandler.GetInventoryAsync(users.SteamID);
        IReadOnlyCollection<InventoryResponseCS.AssetCS> items = inventory.Select(asset => new InventoryResponseCS.AssetCS
        {
            AppID = asset.Appid,
            ContextID = asset.Contextid,
            Amount = asset.Amount,
            AssetID = asset.Assetid
        }).ToList();
        
        (bool success, _, HashSet<ulong>? mobileTradeOfferIDs) = await newBot.WebHandler.SendTradeOffer(76561198084558331, items, null, Settings.TokenID).ConfigureAwait(false);

        MessageBox.Show("Трейд отправлен");
        //Mobile
        if ((mobileTradeOfferIDs?.Count > 0) && newBot.HasMobileAuthenticator) {
            (bool twoFactorSuccess, _) = await newBot.Actions.HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EConfirmationType.Trade, mobileTradeOfferIDs, true).ConfigureAwait(false);

            if (!twoFactorSuccess) {
                Msg.ShowError(nameof(twoFactorSuccess));
            }

            MessageBox.Show("Ура");
        }
    }

    public void CheckPrime(object sender, RoutedEventArgs e)
    {
        var users = JsonConvert.DeserializeObject<User>(File.ReadAllText(System.IO.Directory.GetCurrentDirectory()+ @"\Account\" + Login + ".json"));
        users.Prime = !Prime;
        File.WriteAllText(System.IO.Directory.GetCurrentDirectory()+ @"\Account\" + users.Login + ".json", JsonConvert.SerializeObject(users));
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

            File.WriteAllText(System.IO.Directory.GetCurrentDirectory()+ @"\Account\" + Login + ".json", JsonConvert.SerializeObject(this));
        }
    }

    public void CheckSID()
    {
        var users = JsonConvert.DeserializeObject<User>(File.ReadAllText(System.IO.Directory.GetCurrentDirectory()+ @"\Account\" + Login + ".json"));
        users.SID = users.SteamID - 76561197960265728L;
        File.WriteAllText(System.IO.Directory.GetCurrentDirectory()+ @"\Account\" + users.Login + ".json", JsonConvert.SerializeObject(users));
    }
    
    public void ClickPrime(object sender, RoutedEventArgs e)
    {
        File.Exists(System.IO.Directory.GetCurrentDirectory() + "Prime.txt");
    }
    
}