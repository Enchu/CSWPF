using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
#pragma warning disable SYSLIB0011

namespace CSWPF.Helpers;

[Serializable]
public class Options
{
    private static Options options = (Options) null;
    private static string optionsFileName = string.Empty;

    public string ParametersDefault { get; set; }

    public string CSGOPath { get; set; }

    public string StreamPath { get; set; }

    public bool IsHideLauncher { get; set; } = true;

    public bool IsDisconnectsLoop { get; set; } = true;

    public bool IsFix { get; set; }

    public int CSGOProcessTimeAliveMin { get; set; } = 30000;

    public int TimerDisconnect { get; set; } = 3000;

    public int TimerConnect { get; set; } = 1000;

    public int TimerInsteadConnect { get; set; }

    public int TimerBeforeConnect { get; set; } = 2000;

    public int CyclesCount { get; set; } = 15;

    public string Email { get; set; }

    public bool IsShittyHardware { get; set; }

    public int TimerBtwStartAccounts { get; set; } = 2;

    public static Options G
    {
      get
      {
        if (Options.options == null)
          Options.options = Options.Load();
        if (Options.options == null)
        {
          Options.options = new Options();
          Options.options.Save();
        }
        return Options.options;
      }
    }

    private static string GetOptionsFileName()
    {
      if (Options.optionsFileName == string.Empty)
        Options.optionsFileName = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\.settings.1.5.dat";
      return Options.optionsFileName;
    }

    public void Save()
    {
      using (FileStream serializationStream = new FileStream(Options.GetOptionsFileName(), FileMode.Create, FileAccess.Write))
        new BinaryFormatter().Serialize((Stream) serializationStream, (object) this);
    }
    
    public static Options Load()
    {
      if (!File.Exists(Options.GetOptionsFileName()))
        return new Options();
      try
      {
        using (FileStream serializationStream = new FileStream(Options.GetOptionsFileName(), FileMode.Open, FileAccess.Read))
          return (Options) new BinaryFormatter().Deserialize((Stream) serializationStream);
      }
      catch { }
      return new Options();
    }
}