using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CSWPF.CSB;

public class Sandboxie : IDisposable
  {
    private const string kernal32dll = "kernel32.dll";
    private bool disposed;
    private IntPtr dllPtr = IntPtr.Zero;
    private static string SandboxiePath = "c:\\Program Files\\Sandboxie";
    private static Sandboxie _sand = (Sandboxie) null;

    [DllImport("kernel32.dll")]
    private static extern IntPtr LoadLibrary(string dllToLoad);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

    [DllImport("kernel32.dll")]
    private static extern bool FreeLibrary(IntPtr hModule);

    public static void SetSandboxiePath(string path)
    {
      Sandboxie.SandboxiePath = path;
      if (Sandboxie._sand == null || Sandboxie._sand.disposed)
        return;
      Sandboxie._sand = (Sandboxie) null;
    }

    public static Sandboxie G
    {
      get
      {
        if (Sandboxie._sand == null)
        {
          if (!System.IO.Directory.Exists(Sandboxie.SandboxiePath))
            throw new System.Exception("Установите Sandboxie и укажите папку в настойках программы!");
          try
          {
            Sandboxie._sand = new Sandboxie(Sandboxie.SandboxiePath + "\\32\\SbieDll.dll");
          }
          catch (FileNotFoundException ex)
          {
            throw new System.Exception("Установите Sandboxie и укажите папку в настойках программы!");
          }
          catch (FileLoadException ex)
          {
            throw new System.Exception("Установите Sandboxie и укажите папку в настойках программы!");
          }
        }
        return Sandboxie._sand;
      }
    }

    public Sandboxie(string sbieDllPath)
    {
      if (string.IsNullOrWhiteSpace(sbieDllPath))
        throw new ArgumentNullException(nameof (sbieDllPath));
      this.dllPtr = File.Exists(sbieDllPath) ? Sandboxie.LoadLibrary(sbieDllPath) : throw new FileNotFoundException("Supplied SbieDll.dll not found", sbieDllPath);
      if (this.dllPtr == IntPtr.Zero)
        throw new FileLoadException("Unable to load supplied SbieDll.dll", sbieDllPath);
    }

    ~Sandboxie() => this.Dispose(false);

    public virtual void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    public async Task Start(string box_name, string commandLine)
    {
      string str = Sandboxie.SandboxiePath + "\\Start.exe";
      ProcessStartInfo processStartInfo = new ProcessStartInfo()
      {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        WindowStyle = ProcessWindowStyle.Hidden,
        FileName = str,
        Arguments = "/nosbiectrl /silent /box:" + box_name + " " + commandLine
      };
      Process process1 = new Process();
      process1.StartInfo = processStartInfo;
      process1.OutputDataReceived += (DataReceivedEventHandler) ((sender, e) =>
      {
        if (e.Data == null)
          return;
        Console.WriteLine("[PROC]" + e.Data);
      });
      process1.ErrorDataReceived += (DataReceivedEventHandler) ((sender, e) =>
      {
        if (e.Data == null)
          return;
        Console.WriteLine("[PROC]" + e.Data);
      });
      process1.Exited += (EventHandler) ((sender, e) =>
      {
        if (!(sender is Process process3))
          return;
        Console.WriteLine("[PROC] exited with code = " + process3.ExitCode.ToString());
      });
      process1.Start();
      process1.BeginErrorReadLine();
      process1.BeginOutputReadLine();
      await process1.WaitForExitAsync();
    }

    public bool RunProcess(string box_name, string cmd, string dir)
    {
      Sandboxie.SbieDll_RunSandboxed externalMethodDelegate = (Sandboxie.SbieDll_RunSandboxed) this.GetExternalMethodDelegate<Sandboxie.SbieDll_RunSandboxed>();
      byte[] bytes1 = Encoding.Unicode.GetBytes(box_name);
      byte[] bytes2 = Encoding.Unicode.GetBytes(cmd);
      byte[] bytes3 = Encoding.Unicode.GetBytes(dir);
      byte[] box_name1 = bytes1;
      byte[] cmd1 = bytes2;
      byte[] dir1 = bytes3;
      IntPtr zero1 = IntPtr.Zero;
      IntPtr zero2 = IntPtr.Zero;
      return externalMethodDelegate(box_name1, cmd1, dir1, 0UL, zero1, zero2);
    }

    public string[] EnumBoxes()
    {
      Sandboxie.SbieApi_EnumBoxes externalMethodDelegate = (Sandboxie.SbieApi_EnumBoxes) this.GetExternalMethodDelegate<Sandboxie.SbieApi_EnumBoxes>();
      List<string> stringList = new List<string>();
      int index = -1;
      while (true)
      {
        byte[] numArray = new byte[34];
        index = externalMethodDelegate(index, numArray);
        if (index != -1)
        {
          string str = this.ConvertFromWChar(numArray);
          stringList.Add(str);
        }
        else
          break;
      }
      return stringList.ToArray();
    }

    /*
    public SandboxPaths QueryBoxPath(string box_name)
    {
      if (string.IsNullOrWhiteSpace(box_name))
        throw new ArgumentNullException(nameof (box_name));
      Sandboxie.SbieApi_QueryBoxPath externalMethodDelegate = (Sandboxie.SbieApi_QueryBoxPath) this.GetExternalMethodDelegate<Sandboxie.SbieApi_QueryBoxPath>();
      ulong file_path_len = 0;
      ulong key_path_len = 0;
      ulong ipc_path_len = 0;
      byte[] bytes = Encoding.Unicode.GetBytes(box_name);
      int num1 = externalMethodDelegate(bytes, (byte[]) null, (byte[]) null, (byte[]) null, ref file_path_len, ref key_path_len, ref ipc_path_len);
      if (num1 != 0)
        throw new ApplicationException(externalMethodDelegate.GetType().Name + " returned " + num1.ToString());
      byte[] numArray1 = new byte[file_path_len];
      byte[] numArray2 = new byte[key_path_len];
      byte[] numArray3 = new byte[ipc_path_len];
      int num2 = externalMethodDelegate(bytes, numArray1, numArray2, numArray3, ref file_path_len, ref key_path_len, ref ipc_path_len);
      if (num2 != 0)
        throw new ApplicationException(externalMethodDelegate.GetType().Name + " returned " + num2.ToString());
      string str1 = this.ConvertFromWChar(numArray1);
      string str2 = this.ConvertFromWChar(numArray2);
      string str3 = this.ConvertFromWChar(numArray3);
      return new SandboxPaths()
      {
        FilePath = str1,
        KeyPath = str2,
        IpcPath = str3
      };
    }

    public SandboxPaths QueryProcessPath(uint process_id)
    {
      Sandboxie.SbieApi_QueryProcessPath externalMethodDelegate = (Sandboxie.SbieApi_QueryProcessPath) this.GetExternalMethodDelegate<Sandboxie.SbieApi_QueryProcessPath>();
      ulong file_path_len = 0;
      ulong key_path_len = 0;
      ulong ipc_path_len = 0;
      int num1 = externalMethodDelegate(process_id, (byte[]) null, (byte[]) null, (byte[]) null, ref file_path_len, ref key_path_len, ref ipc_path_len);
      if (num1 != 0)
        throw new ApplicationException(externalMethodDelegate.GetType().Name + " returned " + num1.ToString());
      byte[] numArray1 = new byte[file_path_len];
      byte[] numArray2 = new byte[key_path_len];
      byte[] numArray3 = new byte[ipc_path_len];
      int num2 = externalMethodDelegate(process_id, numArray1, numArray2, numArray3, ref file_path_len, ref key_path_len, ref ipc_path_len);
      if (num2 != 0)
        throw new ApplicationException(externalMethodDelegate.GetType().Name + " returned " + num2.ToString());
      string str1 = this.ConvertFromWChar(numArray1);
      string str2 = this.ConvertFromWChar(numArray2);
      string str3 = this.ConvertFromWChar(numArray3);
      return new SandboxPaths()
      {
        FilePath = str1,
        KeyPath = str2,
        IpcPath = str3
      };
    }
    
    */

    public uint[] EnumProcess(string box_name, bool all_sessions = true, int which_session = -1)
    {
      if (string.IsNullOrWhiteSpace(box_name))
        throw new ArgumentNullException(nameof (box_name));
      Sandboxie.SbieApi_EnumProcessEx externalMethodDelegate = (Sandboxie.SbieApi_EnumProcessEx) this.GetExternalMethodDelegate<Sandboxie.SbieApi_EnumProcessEx>();
      byte[] bytes = Encoding.Unicode.GetBytes(box_name);
      uint[] boxed_pids = new uint[512];
      int num = externalMethodDelegate(bytes, all_sessions, which_session, boxed_pids);
      if (num != 0)
        throw new ApplicationException(externalMethodDelegate.GetType().Name + " returned " + num.ToString());
      List<uint> uintList = new List<uint>();
      for (int index = 1; (long) index <= (long) boxed_pids[0]; ++index)
      {
        if (boxed_pids[0] > 0U)
          uintList.Add(boxed_pids[index]);
      }
      return uintList.ToArray();
    }

    public ProcessInformation QueryProcess(uint process_id)
    {
      Sandboxie.SbieApi_QueryProcess externalMethodDelegate = (Sandboxie.SbieApi_QueryProcess) this.GetExternalMethodDelegate<Sandboxie.SbieApi_QueryProcess>();
      byte[] numArray1 = new byte[34];
      byte[] numArray2 = new byte[96];
      byte[] numArray3 = new byte[96];
      UIntPtr session_id = new UIntPtr();
      int num = externalMethodDelegate(process_id, numArray1, numArray2, numArray3, session_id);
      if (num != 0)
        throw new ApplicationException(externalMethodDelegate.GetType().Name + " returned " + num.ToString());
      string str1 = this.ConvertFromWChar(numArray1);
      string str2 = this.ConvertFromWChar(numArray2);
      string str3 = this.ConvertFromWChar(numArray3);
      return new ProcessInformation()
      {
        ProcessId = process_id,
        BoxName = str1,
        ImageName = str2,
        SidString = str3,
        SessionId = session_id.ToUInt32()
      };
    }

    public bool KillOne(uint process_id) => ((Sandboxie.SbieDll_KillOne) this.GetExternalMethodDelegate<Sandboxie.SbieDll_KillOne>())(process_id);

    public bool KillAll(string box_name) => this.KillAll(-1, box_name);

    public bool KillAll(int session_id, string box_name)
    {
      if (string.IsNullOrWhiteSpace(box_name))
        throw new ArgumentNullException(nameof (box_name));
      Sandboxie.SbieDll_KillAll externalMethodDelegate = (Sandboxie.SbieDll_KillAll) this.GetExternalMethodDelegate<Sandboxie.SbieDll_KillAll>();
      byte[] bytes = Encoding.Unicode.GetBytes(box_name);
      int session_id1 = session_id;
      byte[] box_name1 = bytes;
      return externalMethodDelegate(session_id1, box_name1);
    }

    public string QueryConf(string section_name, string setting_name, uint setting_index = 0)
    {
      if (string.IsNullOrWhiteSpace(section_name))
        throw new ArgumentNullException(nameof (section_name));
      if (string.IsNullOrWhiteSpace(setting_name))
        throw new ArgumentNullException(nameof (setting_name));
      Sandboxie.SbieApi_QueryConf externalMethodDelegate = (Sandboxie.SbieApi_QueryConf) this.GetExternalMethodDelegate<Sandboxie.SbieApi_QueryConf>();
      byte[] bytes1 = Encoding.Unicode.GetBytes(section_name);
      byte[] bytes2 = Encoding.Unicode.GetBytes(setting_name);
      byte[] stringBytes = new byte[8000];
      int num = externalMethodDelegate(bytes1, bytes2, setting_index, stringBytes, (uint) stringBytes.Length);
      if (num != 0)
        throw new ApplicationException(externalMethodDelegate.GetType().Name + " returned " + num.ToString());
      return this.ConvertFromWChar(stringBytes);
    }

    public void ReloadConf(int session_id = -1)
    {
      Sandboxie.SbieApi_ReloadConf externalMethodDelegate = (Sandboxie.SbieApi_ReloadConf) this.GetExternalMethodDelegate<Sandboxie.SbieApi_ReloadConf>();
      int num = externalMethodDelegate(session_id);
      if (num != 0)
        throw new ApplicationException(externalMethodDelegate.GetType().Name + " returned " + num.ToString());
    }

    public ProcessInformation[] QueryEnumProcess(
      string box_name,
      bool all_sessions = true,
      int which_session = -1)
    {
      if (string.IsNullOrWhiteSpace(box_name))
        throw new ArgumentNullException(nameof (box_name));
      return ((IEnumerable<uint>) this.EnumProcess(box_name, all_sessions, which_session)).Select<uint, ProcessInformation>((Func<uint, ProcessInformation>) (pid => this.QueryProcess(pid))).ToArray<ProcessInformation>();
    }

    public bool BoxIsIdle(string box_name)
    {
      if (string.IsNullOrWhiteSpace(box_name))
        throw new ArgumentNullException(nameof (box_name));
      return this.EnumProcess(box_name).Length == 0;
    }

    public bool BoxIsBusy(string box_name) => !this.BoxIsIdle(box_name);

    public string[] EnumIdleBoxes() => ((IEnumerable<string>) this.EnumBoxes()).Where<string>((Func<string, bool>) (box => this.BoxIsIdle(box))).ToArray<string>();

    private void Dispose(bool disposing)
    {
      if (this.disposed)
        return;
      try
      {
        if (!disposing)
          return;
        if (this.dllPtr != IntPtr.Zero)
          Sandboxie.FreeLibrary(this.dllPtr);
        this.dllPtr = IntPtr.Zero;
      }
      finally
      {
        this.disposed = true;
      }
    }

    private Delegate GetExternalMethodDelegate<T>()
    {
      Type t = typeof (T);
      IntPtr procAddress = Sandboxie.GetProcAddress(this.dllPtr, t.Name);
      return !(procAddress == IntPtr.Zero) ? Marshal.GetDelegateForFunctionPointer(procAddress, t) : throw new ApplicationException(string.Format("External method '{0}' not found", (object) t.Name));
    }

    private string ConvertFromWChar(byte[] stringBytes)
    {
      string str = Encoding.Unicode.GetString(stringBytes);
      int startIndex = str.IndexOf("\0");
      if (startIndex != -1)
        str = str.Remove(startIndex);
      return str;
    }

    private delegate int SbieApi_EnumBoxes(int index, byte[] box_name);

    private delegate int SbieApi_QueryBoxPath(
      byte[] box_name,
      byte[] file_path,
      byte[] key_path,
      byte[] ipc_path,
      ref ulong file_path_len,
      ref ulong key_path_len,
      ref ulong ipc_path_len);

    private delegate int SbieApi_QueryProcessPath(
      uint process_id,
      byte[] file_path,
      byte[] key_path,
      byte[] ipc_path,
      ref ulong file_path_len,
      ref ulong key_path_len,
      ref ulong ipc_path_len);

    private delegate int SbieApi_EnumProcessEx(
      byte[] box_name,
      bool all_sessions,
      int which_session,
      [MarshalAs(UnmanagedType.LPArray)] uint[] boxed_pids);

    private delegate int SbieApi_QueryProcess(
      uint process_id,
      byte[] box_name,
      byte[] image_name,
      byte[] sid_string,
      UIntPtr session_id);

    private delegate bool SbieDll_KillOne(uint process_id);

    private delegate bool SbieDll_KillAll(int session_id, byte[] box_name);

    private delegate bool SbieDll_RunSandboxed(
      byte[] box_name,
      byte[] cmd,
      byte[] dir,
      ulong creation_flags,
      IntPtr si,
      IntPtr pi);

    private delegate int SbieApi_QueryConf(
      byte[] section_name,
      byte[] setting_name,
      uint setting_index,
      byte[] value,
      uint value_len);

    private delegate int SbieApi_ReloadConf(int session_id);
  }