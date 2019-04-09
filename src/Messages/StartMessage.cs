using System;
using System.Collections.Generic;
using System.Text;

namespace AkkaActorTesting.Messages
{
    public class StartMessage
    {
        public string PathToPlugin { get; private set; }
        public IEnumerable<KeyValuePair<string, string>> EnvironmentVariables { get; private set; }

        public StartMessage(string path, IEnumerable<KeyValuePair<string, string>> environmentVariables)
        {
            PathToPlugin = path;
            EnvironmentVariables = environmentVariables;
        }
    }
}
