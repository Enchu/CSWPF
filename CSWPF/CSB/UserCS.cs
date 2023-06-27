using System;
using System.Runtime.Serialization;

namespace CSWPF.CSB;

[DataContract]
[Serializable]
public class UserCS
{
    [DataMember(Name = "login")]
    public string Login { get; set; }

    [DataMember(Name = "passw")]
    public string Password { get; set; }

    [DataMember(Name = "rank")]
    public int Rank { get; set; }
}