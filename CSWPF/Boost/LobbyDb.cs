using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using CSWPF.Boost;
#pragma warning disable SYSLIB0011

namespace CSWPF.Directory;

[Serializable]
public class LobbyDb : List<Lobby>
{
    private static LobbyDb options = (LobbyDb) null;
    private static string optionsFileName = string.Empty;

    public static LobbyDb G
    {
        get
        {
            if (LobbyDb.options == null)
                LobbyDb.options = LobbyDb.Load();
            if (LobbyDb.options == null)
            {
                LobbyDb.options = new LobbyDb();
                LobbyDb.options.Save();
            }
            return LobbyDb.options;
        }
    }

    private static string GetDBFileName()
    {
        if (LobbyDb.optionsFileName == string.Empty)
            LobbyDb.optionsFileName = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\.db.1.5.dat";
        return LobbyDb.optionsFileName;
    }

    public void Save()
    {
        using (FileStream serializationStream = new FileStream(LobbyDb.GetDBFileName(), FileMode.Create, FileAccess.Write))
            new BinaryFormatter().Serialize((Stream) serializationStream, (object) this);
    }

    public static LobbyDb Load()
    {
        if (!File.Exists(LobbyDb.GetDBFileName()))
            return new LobbyDb();
        try
        {
            using (FileStream serializationStream = new FileStream(LobbyDb.GetDBFileName(), FileMode.Open, FileAccess.Read))
                return (LobbyDb) new BinaryFormatter().Deserialize((Stream) serializationStream);
        }
        catch
        {
        }
        return new LobbyDb();
    }
}