using CSWPF.Boost;
using CSWPF.Directory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CSWPF.Direct
{
    public class Account
    {
        public string Login { get; set; }
        public string Password { get; set; }
        [DataMember(Name = "isuse")]
        private bool _isUse = true;

        private Lobby _lobby;

        [NonSerialized]
        private Stopwatch _timerAlive;

        [NonSerialized]
        private bool _isStarted;

        [NonSerialized]
        private bool _isStartedSteam;

        [NonSerialized]
        private bool _waitToClick;

        [DataMember(Name = "isleader")]
        public bool IsLeader { get; set; }
        public bool IsUse
        {
            get => this._isUse && !string.IsNullOrEmpty(this.Login);
            set => this._isUse = value;
        }
        [DataMember(Name = "x")]
        public int X { get; set; }

        [DataMember(Name = "y")]
        public int Y { get; set; }

        [JsonProperty("SteamID")]
        public ulong SteamID { get; set; }
        [DataMember(Name = "Sid")]
        public ulong SID { get; set; }
        [JsonProperty("shared_secret")]
        public string SharedSecret { get; set; }

        [JsonProperty("prime")]
        public bool Prime { get; set; }
        public DateTime DateTime { get; set; } = DateTime.Now;
        public Lobby Lobby { get; set; }

        [DataMember(Name = "isstarted")]
        public bool IsStarted
        {
            get => this._isStarted;
            set
            {
                if (this._isStarted == value)
                    return;
                this._isStarted = value;
                if (this._isStarted)
                {
                    if (this._timerAlive == null)
                        this._timerAlive = new Stopwatch();
                    this._timerAlive.Restart();
                }
                else
                    this._timerAlive?.Stop();
            }
        }

        [DataMember(Name = "isstartedstream")]
        public bool IsStartedSteam
        {
            get => this._isStartedSteam;
            set
            {
                if (this._isStartedSteam == value)
                    return;
                this._isStartedSteam = value;
            }
        }

        public int TimeAlive => this._timerAlive != null && this._timerAlive.IsRunning ? Convert.ToInt32(this._timerAlive.ElapsedMilliseconds) : 0;

        public bool WaitToClick1
        {
            get => this._waitToClick;
            set => this._waitToClick = value;
        }

        public Account(Lobby lobby) => this._lobby = lobby;

        public int Index => this._lobby.Accounts.IndexOf(this);

        public User GetData() => new User
        {
            Login = this.Login,
            Password = this.Password,
            SteamID = this.SteamID,
            SID = this.SID,
            Prime = this.Prime,
            DateTime = this.DateTime,
            SharedSecret = this.SharedSecret,
            IsLeader = this.IsLeader,
        };
    }
}
