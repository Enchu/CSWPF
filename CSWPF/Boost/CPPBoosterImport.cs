using CSWPF.Boost.Deleg;
using CSWPF.Directory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static SteamKit2.Internal.CMsgClientUserNotifications;

namespace CSWPF.Boost
{
    public class CPPBoosterImport
    {
        [DllImport("CPPBooster-x86", EntryPoint = "init_data1", CallingConvention = CallingConvention.StdCall)]
        public static extern int InitData(string email, string hwid);

        [DllImport("CPPBooster-x86", EntryPoint = "reg_user1", CallingConvention = CallingConvention.StdCall)]
        public static extern int RegUser();

        [DllImport("CPPBooster-x86", EntryPoint = "applyCheck", CallingConvention = CallingConvention.StdCall)]
        public static extern int ApplyCheck(
          string name,
          User[] lobby,
          int count,
          int width,
          int height,
          User leader1,
          string mcLeader1,
          string matchIdLeader1,
          int needRestart1,
          SetIntValue writeNeedRestart1,
          User leader2,
          string mcLeader2,
          string matchIdLeader2,
          int needRestart2,
          SetIntValue writeNeedRestart2,
          IsCancellationRequested token,
          GetMainWindowHandle proc,
          IsWaitAutoApply wait,
          GetColorFlags getcolors,
          Notification log);

        [DllImport("CPPBooster-x86", EntryPoint = "runCycle1", CallingConvention = CallingConvention.StdCall)]
        public static extern int RunCycle1(
          User[] team,
          int count,
          int index,
          int width,
          int height,
          bool onelobby,
          bool isDropFirstWhenOneLobby,
          int maxScores,
          int tc,
          int td,
          IsCancellationRequested token,
          GetMainWindowHandle proc,
          GetIntValue score0,
          GetIntValue score1,
          GetIntValue sc0,
          GetIntValue sc1,
          GetIntValue currentRound,
          IsWaitAutoApply isWaitCycles,
          GetFlagValue isLive,
          GetFlagValue isLiveNow,
          GetFlagValue isGameOver,
          GetFlagValue currentLobbyIsCT,
          GetFlagValue currentLobbyIsCTStart,
          Notification log);

        [DllImport("CPPBooster-x86", EntryPoint = "runCycle2", CallingConvention = CallingConvention.StdCall)]
        public static extern int RunCycle2(
          User[] team,
          int count,
          int index,
          int width,
          int height,
          bool onelobby,
          bool isDropFirstWhenOneLobby,
          int maxScores,
          int tc,
          int td,
          IsCancellationRequested token,
          GetMainWindowHandle proc,
          GetIntValue score0,
          GetIntValue score1,
          GetIntValue sc0,
          GetIntValue sc1,
          GetIntValue currentRound,
          IsWaitAutoApply isWaitCycles,
          GetFlagValue isLive,
          GetFlagValue isLiveNow,
          GetFlagValue isGameOver,
          GetFlagValue currentLobbyIsCT,
          GetFlagValue currentLobbyIsCTStart);

        [DllImport("CPPBooster-x86", EntryPoint = "connectLobby", CallingConvention = CallingConvention.StdCall)]
        public static extern int Connect(
          User[] acc,
          int count,
          int width,
          int height,
          IsCancellationRequested token,
          GetMainWindowHandle proc);

        [DllImport("CPPBooster-x86", EntryPoint = "connectLobbyOnce", CallingConvention = CallingConvention.StdCall)]
        public static extern int Connect(
          User acc,
          int count,
          int width,
          int height,
          IsCancellationRequested token,
          GetMainWindowHandle proc,
          Notification log);

        [DllImport("CPPBooster-x86", EntryPoint = "disconnectLobby", CallingConvention = CallingConvention.StdCall)]
        public static extern int Disconnect(
          User[] acc,
          int count,
          int width,
          int height,
          IsCancellationRequested token,
          GetMainWindowHandle proc,
          Notification log);

        [DllImport("CPPBooster-x86", EntryPoint = "disconnectLobbyOnce", CallingConvention = CallingConvention.StdCall)]
        public static extern int Disconnect(
          User acc,
          int count,
          int width,
          int height,
          IsCancellationRequested token,
          GetMainWindowHandle proc,
          Notification log);

        [DllImport("CPPBooster-x86", EntryPoint = "copyBotNumberToClipboard", CallingConvention = CallingConvention.StdCall)]
        public static extern int CopyBotNumberToClipboard(
          User bot,
          int width,
          int height,
          IsCancellationRequested token,
          GetMainWindowHandle proc,
          Notification log);

        [DllImport("CPPBooster-x86", EntryPoint = "setupCodeToInvoice", CallingConvention = CallingConvention.StdCall)]
        public static extern int SetupCodeToInvoice(
          User leader,
          string code,
          int code_length,
          bool is_2k,
          int width,
          int height,
          bool is_full_lobby,
          IsCancellationRequested token,
          GetMainWindowHandle proc,
          Notification sendString,
          Notification log);

        [DllImport("CPPBooster-x86", EntryPoint = "applyBotInvoice", CallingConvention = CallingConvention.StdCall)]
        public static extern int ApplyBotInvoice(
          User leader,
          int index,
          int width,
          int height,
          bool is_full_lobby,
          IsCancellationRequested token,
          GetMainWindowHandle proc,
          Notification log);

        [DllImport("CPPBooster-x86", EntryPoint = "startAndGoLeaders", CallingConvention = CallingConvention.StdCall)]
        public static extern int StartAndGoLeaders(
          User leader1,
          User leader2,
          int accounts,
          int width,
          int height,
          IsCancellationRequested token,
          GetMainWindowHandle proc,
          IsWaitAutoApply wait,
          GetColorFlags getcolors,
          Notification log);

        [DllImport("CPPBooster-x86", EntryPoint = "restartLeader", CallingConvention = CallingConvention.StdCall)]
        public static extern int RestartLeader(
          User account,
          int width,
          int height,
          IsCancellationRequested token,
          GetMainWindowHandle proc,
          IsWaitAutoApply wait,
          GetColorFlags getcolors,
          Notification log);

        [DllImport("CPPBooster-x86", EntryPoint = "needRestartLeader", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool NeedRestartLeader(
          User account,
          int width,
          int height,
          IsCancellationRequested token,
          GetMainWindowHandle proc,
          GetColorFlags getcolors,
          Notification log);
    }
}
