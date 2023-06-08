﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CSWPF.Directory;
using CSWPF.Web.Responses;
using CSWPF.Web;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace CSWPF.Helpers;

public class HelperCS
{
    [PublicAPI]
    public static Uri SteamCommunityURL => new("https://steamcommunity.com");
    private static ushort WebLimiterDelay => ASF.GlobalConfig?.WebLimiterDelay ?? GlobalConfig.DefaultWebLimiterDelay;
    private bool Initialized;
    public static void SaveToDB(User user)
    {
        User allUsers = new User(user.Login, user.Password);
        foreach (string filename in System.IO.Directory.GetFiles(@"D:\Game\SteamSDA\maFiles", "*.maFile"))
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

    /*static HttpClient httpClient = new HttpClient();
    public static async Task<Inventory> CheckInventory(ulong text)
    {
        int rateLimitingDelay = (ASF.GlobalConfig?.InventoryLimiterDelay ?? GlobalConfig.DefaultInventoryLimiterDelay) * 1000;
            
        Uri uri = new(SteamCommunityURL, $"/inventory/{text}/730/2/?l=english");
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        using var response = await httpClient.SendAsync(request);
        var responseText = await response.Content.ReadAsStringAsync();
        
        ObjectResponse<Inventory>? responce = null;
        try
        {
            for (byte i = 0; (i < Web.Web.MaxTries) && (responce == null); i++)
            {
                if ((i > 0) && (rateLimitingDelay > 0))
                {
                    await Task.Delay(rateLimitingDelay).ConfigureAwait(false);
                }
                
                //responce = await 
            }
        }
        catch{}
        return null;
    }*/
}