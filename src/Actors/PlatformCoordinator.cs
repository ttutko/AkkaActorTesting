using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using Akka.Actor;
using AkkaActorTesting.Configuration;
using AkkaActorTesting.Messages;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using RabbitMQ.Client;

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

        private IConnection _connection;
        private IModel _channel;

        public class ConsoleMessage
        {
            public string Message { get; private set; }

            public ConsoleMessage(string message)
            {
                Message = message;
            }
        }

        public PlatformCoordinator(IMongoClient mongo, 
            PluginHostConfiguration hostConfig, 
            string baseDir, 
            string platformName, 
            string platform, 
            string pluginType, 
            string fileid, 
            string apiVersion, 
            int pluginCount)
        {
            InitRabbitMQ();

            ReceiveAsync<StartMessage>(async m =>
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
                    ZipFile.ExtractToDirectory(zipPath, pluginDir, true);
                }

                // Create virtual environment
                var pythonEnvironmentDir = Path.Combine(pluginDir, "_environment");
                var process = new Process();
                process.StartInfo.FileName = hostConfig.PythonPath;
                process.StartInfo.Arguments = $"-m venv {pythonEnvironmentDir}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.WorkingDirectory = pluginDir;

                process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine(e.Data);
                    }
                });

                //process.Exited += (sender, args) =>
                //{
                //    tcs.SetResult(new ProcessEndedMessage(process.ExitCode));
                //    process.Dispose();
                //};

                process.EnableRaisingEvents = true;

                process.Start();
                process.BeginOutputReadLine();

                
                var result = process.WaitForExit(180000);
                if(result == false)
                {
                    Console.WriteLine("Timeout reached building the plugin python virtual environment!");
                    throw new PluginException("Error starting the plugin: Timeout reached while building virtual environment.");
                }

                // Install packages
                process = new Process();

                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    process.StartInfo.FileName = Path.Combine(pluginDir, "_environment", "Scripts", "python.exe");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    process.StartInfo.FileName = Path.Combine(pluginDir, "_environment", "bin", "python3");
                }

                process.StartInfo.Arguments = $"-m pip install -r requirements.txt";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.WorkingDirectory = pluginDir;

                process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine(e.Data);
                    }
                });

                process.EnableRaisingEvents = true;

                process.Start();
                process.BeginOutputReadLine();


                result = process.WaitForExit(180000);
                if (result == false)
                {
                    Console.WriteLine("Timeout reached installing packages into the plugin python virtual environment!");
                    throw new PluginException("Error starting the plugin: Timeout reached while installing packages into the virtual environment.");
                }

                for (int i = 0; i < pluginCount; i++)
                {
                    var pluginActor = Context.ActorOf(Props.Create(() => new PythonPlugin(Self, pluginDir, m.EnvironmentVariables)), $"plugin_{i}");
                    pluginActor.Tell("Start");
                }
            });

            Receive<MessageQueueMessage>(m =>
            {
                Console.WriteLine($"{Sender.Path}: {m.Content}");
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

        private void HandleMessage(string content)
        {
            //pc.Tell(new MessageQueueMessage(content));
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

        private void InitRabbitMQ()
        {
            var factory = new ConnectionFactory { HostName = "localhost" };

            _connection = factory.CreateConnection();

            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare("plugin_host.exchange", ExchangeType.Topic);
            _channel.QueueDeclare($"plugin_host.queue.{platformName}", false, false, false, null);
            _channel.QueueBind($"plugin_host.queue.{platformName}", "plugin_host.exchange", $"plugin_host.queue.{platfromName}", null);
            _channel.BasicQos(0, 1, false);

            //_connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
        }
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
