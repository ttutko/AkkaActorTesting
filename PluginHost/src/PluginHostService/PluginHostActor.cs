using Akka.Actor;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PluginHostService
{
    public class PluginHostActor : ReceiveActor
    {
        private readonly PluginHostContext context;
        private readonly ILogger<PluginHostActor> logger;
        private readonly Dictionary<string, IActorRef> platformActors;

        public PluginHostActor(PluginHostContext context)
        {
            this.context = context;
            this.logger = context.LoggerFactory.CreateLogger<PluginHostActor>();
            platformActors = new Dictionary<string, IActorRef>();
            ReceiveAsync<Messages.CreatePlatformCoordinatorsMessage>(async m => await CreatePlatformCoordinators(m));
            Receive<Messages.QueryHierarchyMessage>(m => QueryHierarchy(m));
            Receive<ActorIdentity>(identity =>
            {
                //if (identity.MessageId.Equals("1"))
                //{
                    var subject = identity.Subject;
                    logger.LogInformation("{MessageId}: Received identity from: {Path}", identity.MessageId, subject?.Path);
                //}
            });
        }

        public async Task CreatePlatformCoordinators(Messages.CreatePlatformCoordinatorsMessage m)
        {
            foreach (var pluginInfo in await context.PluginRepo.GetPluginInfos())
            {
                logger.LogDebug("Found plugin info: {Id} - {Name} - {PluginFilename} - {PluginType}", pluginInfo.Id, pluginInfo.Name, pluginInfo.PluginFilename, pluginInfo.PluginType);
                logger.LogDebug("Creating actor...");
                var props = Props.Create(() => new PlatformCoordinator(context));
                var pc = Context.ActorOf(props, pluginInfo.Name);
                platformActors.Add(pluginInfo.Name, pc);
            }
        }

        public void QueryHierarchy(Messages.QueryHierarchyMessage m)
        {
            var selection = Context.ActorSelection("/user/*");
            selection.Tell(new Identify("1"), Self);
            selection.Tell("test message", Self);
        }
    }
}
