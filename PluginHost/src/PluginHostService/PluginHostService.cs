using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PluginHostService
{
    public class PluginHostService : BackgroundService
    {
        private readonly ILogger<PluginHostService> _logger;
        private readonly PluginRepository pluginRepository;
        private readonly ILoggerFactory loggerFactory;
        private readonly ActorSystem _actorSystem;
        private IConnection _connection;
        private IModel _channel;
        private CancellationTokenSource _pluginHostTokenSource;
        private IActorRef _pluginHostActor;
        private Dictionary<string, IActorRef> PlatformActors { get; set; }

        public PluginHostService(ILogger<PluginHostService> logger, PluginRepository pluginRepository, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            this.pluginRepository = pluginRepository;
            this.loggerFactory = loggerFactory;
            _actorSystem = ActorSystem.Create("PluginSystem");
            _pluginHostTokenSource = new CancellationTokenSource();
            PlatformActors = new Dictionary<string, IActorRef>();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            InitRabbitMQ();
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var pluginHostToken = _pluginHostTokenSource.Token;

            // Create PluginHostActor
            var props = Props.Create(() => new PluginHostActor(new PluginHostContext { PluginRepo = pluginRepository, LoggerFactory = loggerFactory }));
            _pluginHostActor = _actorSystem.ActorOf(props, "PluginHost");
            _logger.LogInformation("Created 'PluginHost' actor.");
            _pluginHostActor.Tell(new Messages.CreatePlatformCoordinatorsMessage());
            
            while (!stoppingToken.IsCancellationRequested && !pluginHostToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                _pluginHostActor.Tell(new Messages.QueryHierarchyMessage());
                await Task.Delay(1000, stoppingToken);
            }            
        }

        private void InitRabbitMQ()
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare("plugin_host.exchange", ExchangeType.Topic);
            _channel.QueueDeclare(queue: "plugin_host.queue",
                                  durable: false,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                if(ea.BasicProperties.Headers.ContainsKey("plugin_host_control_message") && Encoding.UTF8.GetString(ea.BasicProperties.Headers["plugin_host_control_message"] as byte[]) == "stop")
                {
                    _logger.LogInformation("Plugin host asked to shutdown.");
                    _pluginHostTokenSource.Cancel();
                }
                var message = Encoding.UTF8.GetString(body);
                _logger.LogDebug($"Received message: {message}");
            };
            _channel.BasicConsume(queue: "plugin_host.queue",
                autoAck: true,
                consumer: consumer);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            if(_channel != null)
            {
                _channel.Dispose();
                _channel = null;
            }

            if(_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }

            return base.StopAsync(cancellationToken);
        }
    }
}
