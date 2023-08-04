using System;
using System.Diagnostics;
using Microsoft.VisualBasic.ApplicationServices;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CSWPF.Directory;

namespace CSWPF.Helpers;

public class Settings
{
    public static string SteamPath = @"D:\Steam\";
    public static string SteamFullPath = @"D:\Steam\steam.exe";
    public static string SteamGamePath = @"D:\Steam\steamapps\common\Counter-Strike Global Offensive\";
    public static string SDA = @"D:\Game\SteamSDA\";
    public static readonly string ConfigBot = "-applaunch 730 -w 640 -h 480";
    public static readonly string ConfigGame = "-novid -console +fps_max 1";
    public static readonly string StartSteam = "-silent -vgui";
    public static readonly string ImgEconomy = "https://community.cloudflare.steamstatic.com/economy/image/";
    public static readonly string TokenID = "_TOKyI1G";
    public static int X = 0;
    public static int Y = 0;
    public static int W = 320;
    public static int H = 320;
    private static void SetFileReadAccess(string FileName, bool SetReadOnly) => new FileInfo(FileName).IsReadOnly = SetReadOnly;
    private static bool IsFileReadOnly(string FileName) => new FileInfo(FileName).IsReadOnly;

    
    public static void FixAutoexec(ulong SID)
    {
        string path = $"{SteamGamePath}csgo_{SID}";
        AccountHelper.lineChanger(string.Format("mat_setvideomode {0} {1} 1", (object) 360, (object) 270), path + "/cfg/autoexec.cfg", 12);
    }
    public static void ExchangeCfg(ulong result)
    {
        if (result == 0L)
        throw new Exception("Не удалось получить STEAMID64");

      if (!System.IO.Directory.Exists(SteamPath + "userdata\\" + result.ToString() + "\\"))
            System.IO.Directory.CreateDirectory(SteamPath + "userdata\\" + result.ToString() + "\\");
      if (!System.IO.Directory.Exists(SteamPath + "userdata\\" + result.ToString() + "\\730\\"))
          System.IO.Directory.CreateDirectory(SteamPath + "userdata\\" + result.ToString() + "\\730\\");
      if (!System.IO.Directory.Exists(SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\"))
          System.IO.Directory.CreateDirectory(SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\");
      if (!System.IO.Directory.Exists(SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\"))
          System.IO.Directory.CreateDirectory(SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\");
      int num = File.Exists(SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\config.cfg") ? 1 : 0;
      bool flag1 = File.Exists(SteamPath + "userdata\\" + result.ToString() + "\\730/local\\cfg\\video.txt");
      bool flag2 = File.Exists(SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\videodefaults.txt");
        
      if (num != 0)
      {
          if (!IsFileReadOnly(SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\config.cfg"))
          {
              File.Copy("data/userdata/config.cfg", SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\config.cfg", true);
          }
          else
          {
              SetFileReadAccess(SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\config.cfg", false);
              File.Copy("data/userdata/config.cfg", SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\config.cfg", true);
          }
          SetFileReadAccess(SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\config.cfg", true);
      }
      else
      {
          File.Copy("data/userdata/config.cfg", SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\config.cfg", true);
          SetFileReadAccess(SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\config.cfg", true);
      }
      if (flag1)
      {
          if (!IsFileReadOnly(SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\video.txt"))
          {
              File.Copy("data/userdata/video.txt", SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\video.txt", true);
          }
          else
          {
              SetFileReadAccess(SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\video.txt", false);
              File.Copy("data/userdata/video.txt", SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\video.txt", true);
          }
          SetFileReadAccess(SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\video.txt", true);
      }
      else
      {
          File.Copy("data/userdata/video.txt", SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\video.txt", true);
          SetFileReadAccess(SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\video.txt", true);
      }
      if (flag2)
      {
          if (!IsFileReadOnly(SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\videodefaults.txt"))
          {
              File.Copy("data/userdata/videodefaults.txt", SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\videodefaults.txt", true);
          }
          else
          {
              SetFileReadAccess(SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\videodefaults.txt", false);
              File.Copy("data/userdata/videodefaults.txt", SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\videodefaults.txt", true);
          }
      }
      else
      {
          File.Copy("data/userdata/videodefaults.txt", SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\videodefaults.txt", true);
          SetFileReadAccess(SteamPath + "userdata\\" + result.ToString() + "\\730\\local\\cfg\\videodefaults.txt", true);
      }
    }
    
    private static void lineChanger(string newText, string fileName, int line_to_edit)
    {
        string[] contents = File.ReadAllLines(fileName);
        contents[line_to_edit - 1] = newText;
        File.WriteAllLines(fileName, contents);
    }
    
    private static void copyDirectory(string strSource, string strDestination)
    {
        System.IO.Directory.CreateDirectory(strDestination);
        DirectoryInfo directoryInfo = new DirectoryInfo(strSource);
        foreach (FileInfo file in directoryInfo.GetFiles())
            file.CopyTo(Path.Combine(strDestination, file.Name), true);
        foreach (DirectoryInfo directory in directoryInfo.GetDirectories())
            copyDirectory(Path.Combine(strSource, directory.Name), Path.Combine(strDestination, directory.Name));
    }
}