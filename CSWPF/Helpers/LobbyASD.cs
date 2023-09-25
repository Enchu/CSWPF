using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSWPF.Directory;
using CSWPF.Directory.Assist;
using CSWPF.Directory.Models;

namespace CSWPF.Helpers;

public class LobbyASD
{
    /*
        pt1 - SCHT5-YDZJ
        pt2 - S4E9W-SEZJ
        pt3 - A2UYX-VFZJ
        pt4 - SC7R7-XCWJ

        pt6 - AL3VG-SBYQ
        pt7 - S5XDY-XWXN
        pt8 - SVTHX-UWYQ
        pt9 - SWKK3-ZYZN
     */
    public static async Task SetupLobby(User users)
    {
        await CopyLogAndPasToClipboard(users.Login, users.Password);
        await SteamCode.SteamCodeEnter(users);
    }

    public static async Task AssemblyLobby(List<User> users)
    {
        int index;
        int max = 3;
        Dictionary<User, string> dict = new Dictionary<User, string>();
        
        for (index = 0; index < users.Count; ++index)
        {
            User user = users[index];
            if (AccountHelper.GetProcess(user) != null)
            {
                string code = dict[user];
                await Task.Delay(200);
                await SetupCodeToInvoice(user, code);
                if (index != max)
                    code = (string) null;
                else
                    break;
            }
        }
      
        await Task.Delay(1000);
      
        /*for (index = 0; index < users.Count; ++index)
        {
            bot = users[index];
            Process process = AccountHelper.GetProcess(bot);
            if (process != null)
            {
                botw = process.MainWindowHandle;
                await Task.Delay(200);
                await ApplyBotInvoice(bot, botw, index, token, token2);
                if (index == max)
                {
                    token = new CancellationToken();
                    dict = (Dictionary<User, string>) null;
                    return;
                }
                bot = (User) null;
            }
        }*/
    }

    

    private static async Task SetForeground(Process proc, int timeDelay = 500)
    {
        if (proc == null)
            return;
        WinApi.SetForegroundWindow(proc.MainWindowHandle);
        if (timeDelay <= 0)
            return;
        await Task.Delay(timeDelay);
    }
    
    public static async Task SetForeground(int timeDelay = 500) => await SetForeground(AccountHelper.GetProcessNot(), timeDelay);
    public static async Task SetForegroundCs(string login,int timeDelay = 500) => await SetForeground(AccountHelper.GetProcessCS(login), timeDelay);
    
    public static async Task ClickToAccount(int x, int y, int timeBetweenPosAndClick, int timeOfClick = 20)
    {
        for (int i = 0; i < 3; ++i)
        {
            MoveToAccount( x, y);
            if (timeBetweenPosAndClick > 0)
                await Task.Delay(timeBetweenPosAndClick);
            WinApi.POINT lpPoint;
            if (WinApi.GetCursorPos(out lpPoint) || Math.Abs(lpPoint.X - x) < 3 && Math.Abs(lpPoint.Y - y) < 3)
                break;
        }
        WinApi.mouse_event(2U, 0U, 0U, 0U, 0U);
        await Task.Delay(timeOfClick);
        WinApi.mouse_event(4U, 0U, 0U, 0U, 0U);
    }

    private static void MoveToAccount(int clickX,int clickY)
    {
        WinApi.SetCursorPos(clickX, clickY);
    }

    private static async Task CopyLogAndPasToClipboard(string login, string password)
    {
        await SetForeground();
        
        await ClickToAccount(660, 474, 500);
        await Task.Delay(500);
        WinApi.SendString(login);
        await Task.Delay(500);
      
        await ClickToAccount(660, 551, 300);
        await Task.Delay(500);
        WinApi.SendString(password);
        await Task.Delay(1000);
      
        await ClickToAccount(842, 633, 300);
        await Task.Delay(1000);
    }

    private static async Task SetupCodeToIn()
    {
        await SetForeground();
        
        MoveToAccount( 619, 110);
        await Task.Delay(200);
        await ClickToAccount(660, 474, 500);
    }
    
    private static async Task SetupCodeToInvoice(User account, string code)
    {
      await SetForegroundCs(account.Login);
      
      MoveToAccount( 619, 110);
      await Task.Delay(200);
      int y1 = Size == SizeEnum.Size2K ? 110 : 115;
      await ClickToAccount( 619, y1, 200);
      await ClickToAccount(619, y1, 200);
      await Task.Delay(300);
      MoveToAccount(619, y1 + 20);
      await Task.Delay(200);
      await ClickToAccount( 486, 140, 300);
      await Task.Delay(500);
      await ClickToAccount(285, 232, 500);
      await ClickToAccount( 290, 230, 500);
      await Task.Delay(500);
      WinApi.SendString(code);
      await Task.Delay(500);
      if (LobbyASD.Size == SizeEnum.SizeFHD)
        await ClickToAccount( 356, 232, 500);
      if (LobbyASD.Size == SizeEnum.Size2K)
        await ClickToAccount( 364, 232, 500);
      await Task.Delay(1000);
      await ClickToAccount(265, 251, 500);
      await Task.Delay(1000);
      int y2 = Size == SizeEnum.Size2K ? 245 : 225;
      for (int x = 468; x < 518; x += 5)
      {
        await ClickToAccount(x, y2, 100);
        await Task.Delay(100);
      }
      await Task.Delay(200);
      await ClickToAccount(458, 232, 200);
      await Task.Delay(200);
      await ClickToAccount(398, 287, 500);
      await Task.Delay(200);
    }
    
    public static SizeEnum Size
    {
        get => _sizeEnum;
        set
        {
            _sizeEnum = value;
            switch (_sizeEnum)
            {
                case SizeEnum.Size2K:
                    Width = 500;
                    Height = 375;
                    break;
                case SizeEnum.SizeFHD:
                    Width = 350;
                    Height = Width / 4 * 3;
                    break;
            }
        }
    }
    
    private static SizeEnum _sizeEnum;
    public static int Width { get; set; } = 500;
    public static int Height { get; set; } = 375;

}