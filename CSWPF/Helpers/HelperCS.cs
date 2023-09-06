using System;
using System.Threading.Tasks;
using SteamAuth;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using CSWPF.Directory;
using CSWPF.Directory.Assist;
using CSWPF.Directory.Models;
using CSWPF.Steam;
using CSWPF.Utils;
using Newtonsoft.Json;
using Confirmation = CSWPF.Steam.Security.Confirmation;

namespace CSWPF.Helpers;

public partial class HelperCS : System.Windows.Forms.Form
{
    public static void SaveNew(User user, string steamid, string sharedSecret)
    {
        User allUsers = new User(user.Login, user.Password);
        allUsers.SteamID = Convert.ToUInt64(steamid);
        allUsers.SID = allUsers.SteamID - 76561197960265728L;
        allUsers.SharedSecret = sharedSecret;
        allUsers.Prime = false;
        if(allUsers.SteamID == 0)
        {
            return;
        }

        File.WriteAllText(System.IO.Directory.GetCurrentDirectory()+ @"\Account\" + allUsers.Login + ".json", JsonConvert.SerializeObject(allUsers));
    }

    public static void SaveToDB(User user)
    {
        User allUsers = new User(user.Login, user.Password);
        foreach (string filename in System.IO.Directory.GetFiles($"{Settings.SDA}maFiles", "*.maFile"))
        {
            var currentUsers = JsonConvert.DeserializeObject<maFile>(File.ReadAllText(filename));
            if (currentUsers.AccountName == user.Login)
            {
                allUsers.SteamID = currentUsers.Session.SteamId;
                allUsers.SharedSecret = currentUsers.SharedSecret;
                allUsers.Prime = false;
            }
        }
        if(allUsers.SteamID == 0)
        {
            return;
        }

        File.WriteAllText(System.IO.Directory.GetCurrentDirectory()+ @"\Account\" + allUsers.Login + ".json", JsonConvert.SerializeObject(allUsers));
    }
    
    public static void Start(User user, int x, int y)
    {
        ulong result = user.SteamID;
        result -= 76561197960265728L;
        //Settings
        Settings.ExchangeCfg(result, user.Login);
        Settings.FixAutoexec(result);
        //Start
        if (!File.Exists($"{Settings.SteamPath}steam_{user.SID}.exe"))
            File.Copy($"{Settings.SteamFullPath}", $"{Settings.SteamPath}steam_{user.SID}.exe");

        string steamPath = System.IO.Directory.GetCurrentDirectory() + "/Launcher/Launcher.exe";
        string Parametr = $" \"{user.Login}\" \"{user.Password}\" \"{Settings.ConfigGame}\" \"{user.SID}\" \"{x}\" \"{y}\" \"{Settings.Width}\" \"{Settings.Height}\"";//\"{Settings.SteamPath}\"
        new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = steamPath,
                Arguments = Parametr,
                CreateNoWindow = true,
                UseShellExecute = false
            }
        }.Start();
    }

    public static async void StartLive(List<User> user)
    {
        foreach (var _user in user)
        {
            (int x, int y) windowXY = Settings.SetupXY(_user);
            Start(_user, windowXY.x, windowXY.y);
            
            Process process = AccountHelper.GetProcessNot();
            while(process == null) { 
                await Task.Delay(1000);
                process = AccountHelper.GetProcessNot();
            }

            await LobbyASD.SetupLobby(_user);

            Process processCS = AccountHelper.GetProcessCS(_user.Login);
            while (processCS == null)
            {
                await Task.Delay(3000);
                processCS = AccountHelper.GetProcessCS(_user.Login);
            }

        }

        //await LobbyASD.AssemblyLobby(user);
    }

}