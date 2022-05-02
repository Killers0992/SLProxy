using Newtonsoft.Json;
using SLProxy;
using SLProxy.Cryptography;
using SLProxy.Models;
using SLProxy.ServerList;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

ISerializer Serializer = new SerializerBuilder()
	.WithNamingConvention(UnderscoredNamingConvention.Instance)
	.IgnoreFields()
	.Build();

IDeserializer Deserializer = new DeserializerBuilder()
	.WithNamingConvention(UnderscoredNamingConvention.Instance)
	.IgnoreFields()
	.IgnoreUnmatchedProperties()
	.Build();


if (!File.Exists("./config.yml"))
{
	File.WriteAllText("./config.yml", Serializer.Serialize(new ProxyConfig()));
}

ProxyConfig.Instance = Deserializer.Deserialize<ProxyConfig>(File.ReadAllText("./config.yml"));
File.WriteAllText("./config.yml", Serializer.Serialize(ProxyConfig.Instance));

var serverList = new ServerConsole(7777);

List<Task> Tasks = new List<Task>();

var watcher = new FileSystemWatcher("./");
watcher.EnableRaisingEvents = true;
watcher.star

for (int x = 0; x < ProxyConfig.Instance.Redirects.Count; x++)
{
	var proxy = new UdpProxy();
	Tasks.Add(proxy.Start(x));
}

ProxyConfig.Instance.Redirects.Clear();

Task.WhenAll(Tasks).Wait();