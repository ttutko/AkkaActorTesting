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
            platforms.Add((name: "myplatform1", fileid: "62cf71f2f302b94b3a3e3aaa", pluginPath: "Program.py", envVars: new List<KeyValuePair<string, string>> { { new KeyValuePair<string, string>("SPT_ExitCode", "1") }, { new KeyValuePair<string, string>("SPT_MaxIters", "3")} }));
            platforms.Add((name: "myplatform2", fileid: "62cf71f2f302b94b3a3e3aac", pluginPath: "Program.py", envVars: new List<KeyValuePair<string, string>> { { new KeyValuePair<string, string>("test2", "test2value") }, { new KeyValuePair<string, string>("test3", "test3value") } }));

            return platforms;
        }
    }
}
