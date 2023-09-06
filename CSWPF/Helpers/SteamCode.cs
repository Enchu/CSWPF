using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using CSWPF.Directory;
using CSWPF.Directory.Assist;
using CSWPF.Utils;
using SteamKit2;
using Point = System.Drawing.Point;

namespace CSWPF.Helpers;

public static class SteamCode
{
    private static int defaulttime = 300;
    public static async Task SteamCodeEnter(User user)
    {
        string code = await TwoFactory.GenerateSteamGuardCode(TwoFactory.GetSteamTime(), user.SharedSecret);
        await Task.Delay(defaulttime);
        await LobbyASD.SetForeground();
        await LobbyASD.ClickToAccount(838, 498, 500);
        WinApi.SendString(code);
    }

    public static async Task<string> SteamCodeCreate(User user)
    {
        string code = await TwoFactory.GenerateSteamGuardCode(TwoFactory.GetSteamTime(), user.SharedSecret);
        return code;
    }
}