using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CSWPF.Directory;
using CSWPF.Steam;
using CSWPF.Web.Responses;
using CSWPF.Web;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace CSWPF.Helpers;

public class HelperCS
{
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
    
    public static List<User> ReadAllFromDB(string filePath)
    {
        string json = File.ReadAllText(System.IO.Directory.GetCurrentDirectory() + filePath);
        List<User> currentUsers = JsonConvert.DeserializeObject<List<User>>(json);
        return currentUsers ?? new List<User>();
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