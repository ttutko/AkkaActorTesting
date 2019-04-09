using Akka.Actor;
using AkkaActorTesting.Actors;
using AkkaActorTesting.Configuration;
using AkkaActorTesting.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AkkaActorTesting
{
    public class PluginHostService : BackgroundService
    {
        private readonly ILogger<PluginHostService> _logger;
        private readonly IConfiguration _config;
        private readonly IMongoClient mongo;
        private readonly PluginHostConfiguration hostConfig;
        private readonly FakeRepository repo;

        private ActorSystem ActorSystem { get; set; }
        private Dictionary<string, IActorRef> PlatformActors { get; set; }

        public PluginHostService(ILogger<PluginHostService> logger, IConfiguration config, IMongoClient mongo, PluginHostConfiguration hostConfig, FakeRepository repo)
        {
            _logger = logger;
            _config = config;
            this.mongo = mongo;
            this.hostConfig = hostConfig;
            this.repo = repo;
            PlatformActors = new Dictionary<string, IActorRef>();

            ActorSystem = ActorSystem.Create("PluginSystem");
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            // Get the list of platforms
            var platforms = repo.GetPlatforms();

            foreach(var platform in platforms)
            {
                // Get the platform settings
                var platformConfig = hostConfig.Platforms[platform.name];                

                var props = Props.Create(() => new PlatformCoordinator(mongo, hostConfig, Directory.GetCurrentDirectory(), platform.name, platformConfig.Platform, platformConfig.PluginType, platform.fileid, platformConfig.PluginApiVersion, platformConfig.PluginsPerHost));
                var pc = ActorSystem.ActorOf(props, platform.name);
                pc.Tell(new StartMessage(platform.pluginPath, platform.envVars));
            }

            
            
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Worker running at: {DateTime.Now}");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    //Console.WriteLine("Hello World!");

    //        var system = ActorSystem.Create("TestSystem");
    //var supervisor = system.ActorOf<MySupervisor>("supervisor");

    //supervisor.Tell("Start");

    //        system.WhenTerminated.Wait();
}
