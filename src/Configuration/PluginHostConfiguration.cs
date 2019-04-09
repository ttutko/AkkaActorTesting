using System;
using System.Collections.Generic;
using System.Text;

namespace AkkaActorTesting.Configuration
{
    public class PluginHostConfiguration
    {
        public string ApplicationId { get; set; }
        public string InstanceId { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string PythonPath { get; set; }
        public Dictionary<string, PlatformPluginConfiguration> Platforms { get; private set; }

        public PluginHostConfiguration()
        {
            Platforms = new Dictionary<string, PlatformPluginConfiguration>();
        }
    }
}
