using System;
using System.Threading.Tasks;
using SteamAuth;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CSWPF.Directory;
using CSWPF.Steam;
using CSWPF.Utils;
using Newtonsoft.Json;
using Confirmation = CSWPF.Steam.Security.Confirmation;

namespace CSWPF.Helpers;

public partial class HelperCS : System.Windows.Forms.Form
{
    public User user;
    private SteamGuardAccount _steamAccount;
    
    public HelperCS(User user, SteamGuardAccount steamAccount)
    {
        this.user = user;
        this._steamAccount = steamAccount;
    }

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

    public async Task TradeInventory(User user)
    {
        List<InventoryResponseCS.Asset> inventory;
        var users = JsonConvert.DeserializeObject<User>(File.ReadAllText(System.IO.Directory.GetCurrentDirectory()+ @"\Account\" + user.Login + ".json"));
        Bot newBot = new Bot(users);
        await newBot.Start();
        await Task.Delay(30000);
        inventory = await newBot.WebHandler.GetInventoryAsync(users.SteamID);
        if (inventory.Count > 0)
        {
            //WebHandler.SendTradeOffer(inventory.);
        }
    }
}