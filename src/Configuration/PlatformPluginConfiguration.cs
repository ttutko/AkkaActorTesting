namespace AkkaActorTesting.Configuration
{
    public class PlatformPluginConfiguration
    {
        public string PluginType { get; set; }
        public string PluginApiVersion { get; set; }
        public int PluginsPerHost { get; set; }
        public string Platform { get; set; }
        public string PlatformName { get; set; }
    }
}