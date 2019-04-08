using Akka.Actor;
using AkkaActorTesting.Actors;
using AkkaActorTesting.Configuration;
using AkkaActorTesting.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AkkaActorTesting
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;
        private readonly IMongoClient mongo;
        private readonly PluginHostConfiguration hostConfig;

        private ActorSystem ActorSystem { get; set; }
        private Dictionary<string, IActorRef> PlatformActors { get; set; }

        public Worker(ILogger<Worker> logger, IConfiguration config, IMongoClient mongo, PluginHostConfiguration hostConfig)
        {
            _logger = logger;
            _config = config;
            this.mongo = mongo;
            this.hostConfig = hostConfig;
            PlatformActors = new Dictionary<string, IActorRef>();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            // Get information from configuration
            ActorSystem = ActorSystem.Create("PluginSystem");
            var pc = ActorSystem.ActorOf<PlatformCoordinator>("platform1");
            pc.Tell(new StartMessage("Program.py", new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } }));
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
