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
    
    public static void Start(User user)
    {
        ulong result = user.SteamID;
        result -= 76561197960265728L;
        //Settings
        Settings.ExchangeCfg(result);
        Settings.FixAutoexec(result);
        //Start
        if (!File.Exists($"{Settings.SteamPath}steam_{user.SID}.exe"))
            File.Copy($"{Settings.SteamFullPath}", $"{Settings.SteamPath}steam_{user.SID}.exe");

        string steamPath = System.IO.Directory.GetCurrentDirectory() + "/Launcher/Launcher.exe";
        string Parametr = $" \"{user.Login}\" \"{user.Password}\" \"{Settings.ConfigGame}\" \"{user.SID}\" \"{user.X}\" \"{user.Y}\" \"{Settings.Width}\" \"{Settings.Height}\"";//\"{Settings.SteamPath}\"
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
        bool flag = false;
        foreach (var _user in user)
        {
            Start(_user);
            await Task.Delay(2000, App.Token);
            flag = true;
        }

        if (flag)
        {
            await Task.Delay(120000, App.Token);
        }

        List<User> user1 = new List<User>();
        user1.Add(user[0]);
        user1.Add(user[1]);
        user1.Add(user[2]);
        user1.Add(user[3]);
        user1.Add(user[4]);
        
        List<User> user2 = new List<User>();
        user2.Add(user[5]);
        user2.Add(user[6]);
        user2.Add(user[7]);
        user2.Add(user[8]);
        user2.Add(user[9]);
        
        await Lobby.AssemblyLobbies(user1, user2);
    }

}