using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace AkkaActorTesting
{
    public class FakeRepository
    {
        private readonly IMongoClient mongo;

        public FakeRepository(IMongoClient mongo)
        {
            this.mongo = mongo;
        }

        public List<(string name, string fileid, string pluginPath, List<KeyValuePair<string, string>> envVars)> GetPlatforms()
        {
            List<(string name, string fileid, string pluginPath, List<KeyValuePair<string, string>> envVars)> platforms = new List<(string name, string fileid, string pluginPath, List<KeyValuePair<string, string>> envVars)>();
            platforms.Add((name: "myplatform1", fileid: "5cab797d0b1a3c260c2bff65", pluginPath: "Program.py", envVars: new List<KeyValuePair<string, string>> { { new KeyValuePair<string, string>("test1", "test1value") } }));
            platforms.Add((name: "myplatform2", fileid: "5cab797d0b1a3c260c2bff67", pluginPath: "Program.py", envVars: new List<KeyValuePair<string, string>> { { new KeyValuePair<string, string>("test2", "test2value") }, { new KeyValuePair<string, string>("test3", "test3value") } }));

            return platforms;
        }
    }
}
