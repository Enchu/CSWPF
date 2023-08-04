using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CSWPF.Helpers;
using CSWPF.Utils;
using System.Linq;
using CSWPF.Steam;
using Newtonsoft.Json;
using Confirmation = CSWPF.Steam.Security.Confirmation;

namespace CSWPF.Directory;

public struct User
{
    public User()
    {
    }

    public string Login { get; set; }
    public string Password { get; set; }
    public ulong SteamID { get; set; }
    public ulong SID { get; set; }
    public string SharedSecret { get; set; }
    public bool Prime { get; set; }
    public DateTime DateTime { get; set; } = DateTime.Now;
    public bool IsLeader { get; set; }
}