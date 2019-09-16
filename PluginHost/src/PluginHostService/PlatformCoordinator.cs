using Akka.Actor;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace PluginHostService
{
    public class PlatformCoordinator : ReceiveActor
    {
        private readonly PluginHostContext context;
        private readonly ILogger<PlatformCoordinator> logger;

        public PlatformCoordinator(PluginHostContext context)
        {
            this.context = context;
            logger = context.LoggerFactory.CreateLogger<PlatformCoordinator>();

            Receive<string>(s =>
            {
                logger.LogInformation("Received {message} from {sender} on {receiver}", s, Sender.Path, Self.Path);
            });

            Receive<ActorIdentity>(identity =>
            {
                if (identity.MessageId.Equals("1"))
                {
                    var subject = identity.Subject;

                    logger.LogInformation("Received identity from: {Path}", subject?.Path);
                }
            });
        }
    }
}
