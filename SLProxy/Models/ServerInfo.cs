using YamlDotNet.Serialization;

namespace SLProxy.Models
{
    public class ServerInfo
    {
        public ushort LocalPort { get; set; } = 7777;
        public string ServerIp { get; set; } = "localhost";
        public ushort ServerPort { get; set; } = 3777;

        public string ServerName { get; set; } = "Default server name";
        public string Pastebin { get; set; } = string.Empty;
        public int MaxPlayers { get; set; } = 20;
        public bool Modded { get; set; }
        public bool Whitelist { get; set; }
        public bool FriendlyFire { get; set; }
        public string GameVersion { get; set; } = "11.1.4";
        public bool PrivateBeta { get; set; }
        [YamlIgnore]
        public int OnlinePlayers => Players.Count;
        public readonly Dictionary<string, PlayerInfo> Players = new Dictionary<string, PlayerInfo>();
    }
}
