using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CSWPF.Helpers;
using CSWPF.Utils;
using CSWPF.Web;
using System.Linq;
using CSWPF.Steam;
using CSWPF.Steam.Data;
using CSWPF.Steam.Security;
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
        //Settings
         Settings.ExchangeCfg(SteamID);
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
        IReadOnlyCollection<AssetCS> items = inventory.Select(asset => new AssetCS
        {
            AppID = asset.Appid,
            ContextID = asset.Contextid,
            Amount = asset.Amount,
            AssetID = asset.Assetid
        }).ToList();
        
        (bool success, _, HashSet<ulong>? mobileTradeOfferIDs) = await newBot.WebHandler.SendTradeOffer(76561198084558331, items, null, "_TOKyI1G").ConfigureAwait(false);
        
        if ((mobileTradeOfferIDs?.Count > 0) && newBot.HasMobileAuthenticator) {
            (bool twoFactorSuccess, _, _) = await newBot.Actions.HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EType.Trade, mobileTradeOfferIDs, true).ConfigureAwait(false);

            if (!twoFactorSuccess) {
                Msg.ShowError(nameof(twoFactorSuccess));
            }
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
    
    public void ClickPrime(object sender, RoutedEventArgs e)
    {
        File.Exists(System.IO.Directory.GetCurrentDirectory() + "Prime.txt");
    }
    
}