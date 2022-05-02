using Newtonsoft.Json;
using SLProxy.Models;
using YamlDotNet.Serialization;

namespace SLProxy
{
    public class ProxyConfig
    {
        public static ProxyConfig Instance;
        public Version GameVersion { get; set; }
        public int ConnectionTimeout { get; set; } = 5000;
        public string Email { get; set; } = "email@gmail.com";
        public List<ServerInfo> Redirects { get; set; } = new List<ServerInfo>();
    }
}
