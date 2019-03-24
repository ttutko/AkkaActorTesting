using System;
using System.Collections.Generic;
using System.Text;

namespace AkkaActorTesting.Messages
{
    public class StartMessage
    {
        public string PathToPlugin { get; private set; }
        public IDictionary<string, string> EnvironmentVariables { get; private set; }

        public StartMessage(string path, IDictionary<string, string> environmentVariables)
        {
            PathToPlugin = path;
            EnvironmentVariables = environmentVariables;
        }
    }
}
