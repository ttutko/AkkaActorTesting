using Akka.Actor;
using AkkaActorTesting.Actors;
using AkkaActorTesting.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private ActorSystem ActorSystem { get; set; }
        private List<string> Platforms = new List<string> { "Plat1", "Plat2" };
        private Dictionary<string, IActorRef> PlatformActors { get; set; }

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            PlatformActors = new Dictionary<string, IActorRef>();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            ActorSystem = ActorSystem.Create("PluginSystem");
            foreach(var plat in Platforms)
            {
                var coordinator = ActorSystem.ActorOf<PlatformCoordinator>(plat);
                PlatformActors.Add(plat, coordinator);
                coordinator.Tell(new StartMessage("Program.py", new Dictionary<string, string> { { "test", "testvalue" } }));
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
