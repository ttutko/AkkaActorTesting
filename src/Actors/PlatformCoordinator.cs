using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using AkkaActorTesting.Messages;

namespace AkkaActorTesting.Actors
{
    public class PlatformCoordinator : ReceiveActor
    {
        public class ConsoleMessage
        {
            public string Message { get; private set; }

            public ConsoleMessage(string message)
            {
                Message = message;
            }
        }

        public PlatformCoordinator()
        {
            Receive<StartMessage>(m =>
            {
                for(int i = 0; i < 5; i++)
                {
                    var pluginActor = Context.ActorOf(Props.Create(() => new PythonPlugin(Self, m.EnvironmentVariables)), $"plugin_{i}");
                    pluginActor.Tell("Start");
                }
            });

            Receive<ConsoleMessage>(m => 
            {
                Console.WriteLine($"{Sender.Path}: {m.Message}");
            });
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
