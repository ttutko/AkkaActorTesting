using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Akka.Actor;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using AkkaActorTesting.Configuration;
using MongoDB.Bson;
using System.Linq;

namespace AkkaActorTesting
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {

                    services.AddSingleton<IMongoClient>((_) =>
                    {
                        var connectionString = context.Configuration.GetValue<string>("spt:database:connectionString");
                        var databaseName = context.Configuration.GetValue<string>("spt:database:databaseName");

                        var client = new MongoClient(connectionString);

                        return client;
                    });

                    services.AddScoped<FakeRepository, FakeRepository>();

                    services.AddSingleton<PluginHostConfiguration>((_) =>
                    {
                        var mongo = services.BuildServiceProvider().GetRequiredService<IMongoClient>();
                        var database = mongo.GetDatabase(context.Configuration.GetValue<string>("spt:database:databasename"));
                        var configCollection = database.GetCollection<BsonDocument>("configuration");

                        var filter = Builders<BsonDocument>.Filter.Eq("applicationId", context.Configuration.GetValue<string>("spt:applicationId")) & Builders<BsonDocument>.Filter.Eq("instanceId", context.Configuration.GetValue<string>("spt:instanceId"));
                        var configDoc = configCollection.Find(filter).FirstOrDefault();

                        if(configDoc == null)
                        {
                            throw new Exception("Configuration is not valid.");
                        }

                        var config = new PluginHostConfiguration();
                        config.ApplicationId = configDoc["applicationId"].AsString;
                        config.InstanceId = configDoc["instanceId"].AsString;
                        config.DatabaseName = configDoc["database"]["databaseName"].AsString;
                        config.ConnectionString = configDoc["database"]["connectionString"].AsString;
                        foreach (var item in configDoc["platformSettings"].AsBsonDocument.Elements)
                        {
                            string platformName = item.Name;
                            var platformSettings = item.Value.AsBsonDocument;
                            string platform = platformSettings["platform"].AsString;
                            string pluginType = platformSettings["pluginType"].AsString;
                            string pluginApiVersion = platformSettings["pluginApiVersion"].AsString;
                            int pluginsPerHost = platformSettings["pluginsPerHost"].AsInt32;

                            config.Platforms.Add(platformName, new PlatformPluginConfiguration()
                            {
                                Platform = platform,
                                PlatformName = platformName,
                                PluginApiVersion = pluginApiVersion,
                                PluginsPerHost = pluginsPerHost,
                                PluginType = pluginType
                            });
                        }

                        return config;
                    });

                    services.AddHostedService<PluginHostService>();
                })
                .Build().Run();            
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var builder = new HostBuilder();

            builder.UseContentRoot(Directory.GetCurrentDirectory());
            builder.ConfigureHostConfiguration(config =>
            {
                config.AddEnvironmentVariables(prefix: "DOTNET_");
                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            });

            builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;

                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

                if (env.IsDevelopment() && !string.IsNullOrEmpty(env.ApplicationName))
                {
                    var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                    if (appAssembly != null)
                    {
                        config.AddUserSecrets(appAssembly, optional: true);
                    }
                }

                config.AddEnvironmentVariables();

                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
                logging.AddEventSourceLogger();
            }).UseServiceProviderFactory(new DefaultServiceProviderFactory());
            
            return builder;
        }
    }

    

    //public class MySupervisor : UntypedActor
    //{
    //    //List<IActorRef> children = new List<IActorRef>();

    //    public class ConsoleMessage
    //    {
    //        public string Message { get; private set; }

    //        public ConsoleMessage(string message)
    //        {
    //            Message = message;
    //        }
    //    }

    //    protected override void OnReceive(object message)
    //    {
    //        if(message.Equals("Start"))
    //        {
    //            for (int i = 0; i < 5; i++)
    //            {
    //                var myChild = Context.ActorOf(Props.Create(() => new MyChild(Self)), $"child_{i}");
    //                //if(i == 0)
    //                //{
    //                //    Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(15), myChild, "Stop", Self);
    //                //}
    //                myChild.Tell("Start");
    //            }
    //        }

    //        if(message is ConsoleMessage)
    //        {
    //            var cm = message as ConsoleMessage;
    //            Console.WriteLine($"{Sender.Path}: {cm.Message}");
    //        }
    //    }

    //    protected override SupervisorStrategy SupervisorStrategy()
    //    {
    //        return new OneForOneStrategy(3, TimeSpan.FromSeconds(15), ex =>
    //       {
    //           if(ex is PluginException)
    //           { 
    //               if(Sender is MyChild)
    //               {
    //                   var sender = Sender as MyChild;
    //                   if(sender.process != null && sender.process.HasExited == false)
    //                   {
    //                       sender.process.Dispose();
    //                   }
    //               }

    //               return Directive.Restart;
    //           }

    //           return Directive.Escalate;
    //       });
    //    }
    //}

    //public class MyChild : UntypedActor
    //{
    //    public Process process { get; internal set; } = null;
    //    public IActorRef parent { get; internal set; } = null;

    //    public class ProcessEndedMessage
    //    {
    //        public int Result { get; internal set; }

    //        public ProcessEndedMessage(int result)
    //        {
    //            Result = result;
    //        }
    //    }

    //    public MyChild(IActorRef parent)
    //    {
    //        this.parent = parent;
    //    }

    //    protected override void OnReceive(object message)
    //    {
    //        if(message.Equals("Start"))
    //        {
    //            DoSomething(Self).ContinueWith(result =>
    //            {
    //                Console.WriteLine($"Exited with: {result.Result.Result}");
    //                return result.Result;
    //            }, TaskContinuationOptions.ExecuteSynchronously).PipeTo(Self);
    //        }
    //        else if (message.Equals("Stop"))
    //        {
    //            if(process != null)
    //            {
    //                process.Kill();
    //            }
    //        }
    //        else if (message is ProcessEndedMessage)
    //        {
    //            var pem = message as ProcessEndedMessage;
    //            if(pem.Result != 0)
    //            {
    //                throw new PluginException("Plugin didn't exit cleanly.");
    //            }

    //            Self.GracefulStop(TimeSpan.FromSeconds(5), "Process exited cleanly");
    //        }
    //    }

    //    protected override void PreStart()
    //    {
    //        Console.WriteLine("About to start...");
    //    }

    //    protected override void PreRestart(Exception reason, object message)
    //    {
    //        Console.WriteLine("About to restart...");
    //    }

    //    protected override void PostRestart(Exception reason)
    //    {
    //        Console.WriteLine("Restarted... telling again.");
    //        Self.Tell("Start");
    //    }

    //    protected override void PostStop()
    //    {
    //        Console.WriteLine("Post stop");
    //    }

    //    private Task<ProcessEndedMessage> DoSomething(IActorRef myself)
    //    {
    //        var tcs = new TaskCompletionSource<ProcessEndedMessage>();

    //        process = new Process();
    //        process.StartInfo.FileName = @"C:\Program Files (x86)\Python37-32\python.exe";
    //        process.StartInfo.Arguments = "Program.py";
    //        process.StartInfo.UseShellExecute = false;
    //        process.StartInfo.RedirectStandardError = true;
    //        process.StartInfo.RedirectStandardOutput = true;
    //        process.StartInfo.Environment.Add("SPT_EnvVar", $"Myfile_{myself.Path.Name}.txt");
    //        if(myself.Path.Name == "child_0")
    //        {
    //            process.StartInfo.Environment.Add("SPT_MaxIters", "5");
    //        }
    //        if(myself.Path.Name == "child_3")
    //        {
    //            process.StartInfo.Environment.Add("SPT_MaxIters", "1");
    //            process.StartInfo.Environment.Add("SPT_ExitCode", "1");
    //        }
    //        if (myself.Path.Name == "child_4")
    //        {
    //            process.StartInfo.Environment.Add("SPT_MaxIters", "10");
    //            process.StartInfo.Environment.Add("SPT_ExitCode", "3");
    //        }
    //        process.StartInfo.WorkingDirectory = @"C:\Users\ttutko\Documents\Visual Studio 2017\Projects\AkkaActorTesting";

    //        process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
    //        {
    //            if(!String.IsNullOrEmpty(e.Data))
    //            {
    //                parent.Tell(new MySupervisor.ConsoleMessage(e.Data), myself);
    //            }
    //        });

    //        process.Exited += (sender, args) =>
    //        {
    //            tcs.SetResult(new ProcessEndedMessage(process.ExitCode));
    //            process.Dispose();
    //        };

    //        process.EnableRaisingEvents = true;

    //        process.Start();
    //        process.BeginOutputReadLine();

    //        return tcs.Task;            
    //    }
    //}
}
