using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Akka.Actor;
using AkkaActorTesting.Configuration;
using AkkaActorTesting.Messages;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace AkkaActorTesting.Actors
{
    public class PlatformCoordinator : ReceiveActor
    {
        private readonly string baseDir;
        private readonly string platformName;
        private readonly string platform;
        private readonly string pluginType;
        private readonly string fileid;
        private readonly string apiVersion;
        private readonly int pluginCount;

        public class ConsoleMessage
        {
            public string Message { get; private set; }

            public ConsoleMessage(string message)
            {
                Message = message;
            }
        }

        public PlatformCoordinator(IMongoClient mongo, PluginHostConfiguration hostConfig, string baseDir, string platformName, string platform, string pluginType, string fileid, string apiVersion, int pluginCount)
        {
            Receive<StartMessage>(async m =>
            {
                // Download and extract the plugin
                var bucket = new GridFSBucket(mongo.GetDatabase(hostConfig.DatabaseName), new GridFSBucketOptions() { BucketName = "fs" });

                var pluginDir = Directory.CreateDirectory(Path.Combine(baseDir, platformName)).FullName;
                var pluginFilesDir = Directory.CreateDirectory(Path.Combine(baseDir, "_pluginFiles")).FullName;

                using(var downloadStream = await bucket.OpenDownloadStreamAsync(new ObjectId(fileid)))
                {
                    var zipPath = Path.Combine(pluginFilesDir, downloadStream.FileInfo.Filename);
                    using (var fs = new FileStream(zipPath, FileMode.Create))
                    {
                        await downloadStream.CopyToAsync(fs);
                    }
                    ZipFile.ExtractToDirectory(zipPath, pluginDir);
                }

                
                
                for (int i = 0; i < 5; i++)
                {
                    var pluginActor = Context.ActorOf(Props.Create(() => new PythonPlugin(Self, m.EnvironmentVariables)), $"plugin_{i}");
                    pluginActor.Tell("Start");
                }
            });

            Receive<ConsoleMessage>(m => 
            {
                Console.WriteLine($"{Sender.Path}: {m.Message}");
            });
            this.baseDir = baseDir;
            this.platformName = platformName;
            this.platform = platform;
            this.pluginType = pluginType;
            this.fileid = fileid;
            this.apiVersion = apiVersion;
            this.pluginCount = pluginCount;
        }

        //protected override void OnReceive(object message)
        //{
        //    if (message.Equals("Start"))
        //    {
        //        for (int i = 0; i < 5; i++)
        //        {
        //            var myChild = Context.ActorOf(Props.Create(() => new MyChild(Self)), $"child_{i}");
        //            //if(i == 0)
        //            //{
        //            //    Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(15), myChild, "Stop", Self);
        //            //}
        //            myChild.Tell("Start");
        //        }
        //    }

        //    if (message is ConsoleMessage)
        //    {
        //        var cm = message as ConsoleMessage;
        //        Console.WriteLine($"{Sender.Path}: {cm.Message}");
        //    }
        //}

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(3, TimeSpan.FromSeconds(15), ex =>
            {
                if (ex is PluginException)
                {
                    if (Sender is PythonPlugin)
                    {
                        var sender = Sender as PythonPlugin;
                        if (sender.process != null && sender.process.HasExited == false)
                        {
                            sender.process.Dispose();
                        }
                    }

                    return Directive.Restart;
                }

                return Directive.Escalate;
            });
        }
    }
}
