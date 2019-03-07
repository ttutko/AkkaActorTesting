using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;

namespace AkkaActorTesting
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var system = ActorSystem.Create("TestSystem");
            var supervisor = system.ActorOf<MySupervisor>("supervisor");

            supervisor.Tell("Start");

            system.WhenTerminated.Wait();
        }
    }

    public class MySupervisor : UntypedActor
    {
        //List<IActorRef> children = new List<IActorRef>();

        public class ConsoleMessage
        {
            public string Message { get; private set; }

            public ConsoleMessage(string message)
            {
                Message = message;
            }
        }

        protected override void OnReceive(object message)
        {
            if(message.Equals("Start"))
            {
                for (int i = 0; i < 5; i++)
                {
                    var myChild = Context.ActorOf(Props.Create(() => new MyChild(Self)), $"child_{i}");
                    //if(i == 0)
                    //{
                    //    Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(15), myChild, "Stop", Self);
                    //}
                    myChild.Tell("Start");
                }
            }

            if(message is ConsoleMessage)
            {
                var cm = message as ConsoleMessage;
                Console.WriteLine($"{Sender.Path}: {cm.Message}");
            }
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(3, TimeSpan.FromSeconds(15), ex =>
           {
               if(ex is PluginException)
               { 
                   if(Sender is MyChild)
                   {
                       var sender = Sender as MyChild;
                       if(sender.process != null && sender.process.HasExited == false)
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

    public class MyChild : UntypedActor
    {
        public Process process { get; internal set; } = null;
        public IActorRef parent { get; internal set; } = null;

        public class ProcessEndedMessage
        {
            public int Result { get; internal set; }

            public ProcessEndedMessage(int result)
            {
                Result = result;
            }
        }

        public MyChild(IActorRef parent)
        {
            this.parent = parent;
        }

        protected override void OnReceive(object message)
        {
            if(message.Equals("Start"))
            {
                DoSomething(Self).ContinueWith(result =>
                {
                    Console.WriteLine($"Exited with: {result.Result.Result}");
                    return result.Result;
                }, TaskContinuationOptions.ExecuteSynchronously).PipeTo(Self);
            }
            else if (message.Equals("Stop"))
            {
                if(process != null)
                {
                    process.Kill();
                }
            }
            else if (message is ProcessEndedMessage)
            {
                var pem = message as ProcessEndedMessage;
                if(pem.Result != 0)
                {
                    throw new PluginException("Plugin didn't exit cleanly.");
                }

                Self.GracefulStop(TimeSpan.FromSeconds(5), "Process exited cleanly");
            }
        }

        protected override void PreStart()
        {
            Console.WriteLine("About to start...");
        }

        protected override void PreRestart(Exception reason, object message)
        {
            Console.WriteLine("About to restart...");
        }

        protected override void PostRestart(Exception reason)
        {
            Console.WriteLine("Restarted... telling again.");
            Self.Tell("Start");
        }

        protected override void PostStop()
        {
            Console.WriteLine("Post stop");
        }

        private Task<ProcessEndedMessage> DoSomething(IActorRef myself)
        {
            var tcs = new TaskCompletionSource<ProcessEndedMessage>();

            process = new Process();
            process.StartInfo.FileName = @"C:\Program Files (x86)\Python37-32\python.exe";
            process.StartInfo.Arguments = "Program.py";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.Environment.Add("SPT_EnvVar", $"Myfile_{myself.Path.Name}.txt");
            if(myself.Path.Name == "child_0")
            {
                process.StartInfo.Environment.Add("SPT_MaxIters", "5");
            }
            if(myself.Path.Name == "child_3")
            {
                process.StartInfo.Environment.Add("SPT_MaxIters", "1");
                process.StartInfo.Environment.Add("SPT_ExitCode", "1");
            }
            if (myself.Path.Name == "child_4")
            {
                process.StartInfo.Environment.Add("SPT_MaxIters", "10");
                process.StartInfo.Environment.Add("SPT_ExitCode", "3");
            }
            process.StartInfo.WorkingDirectory = @"C:\Users\ttutko\Documents\Visual Studio 2017\Projects\AkkaActorTesting";

            process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if(!String.IsNullOrEmpty(e.Data))
                {
                    parent.Tell(new MySupervisor.ConsoleMessage(e.Data), myself);
                }
            });

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(new ProcessEndedMessage(process.ExitCode));
                process.Dispose();
            };

            process.EnableRaisingEvents = true;

            process.Start();
            process.BeginOutputReadLine();

            return tcs.Task;            
        }
    }
}
