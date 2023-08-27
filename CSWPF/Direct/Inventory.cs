using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CSWPF.Helpers;
using CSWPF.Steam;
using CSWPF.Steam.Security;
using CSWPF.Utils;
using Newtonsoft.Json;

namespace CSWPF.Directory;

public static class Inventory
{
    public static async Task CheckInventory(User user)
    {
        IReadOnlyCollection<InventoryResponseCS.Asset> inventory;
        var users = JsonConvert.DeserializeObject<User>(File.ReadAllText(System.IO.Directory.GetCurrentDirectory()+ @"\Account\" + user.Login + ".json"));
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
        newBot.Stop();
    }
}