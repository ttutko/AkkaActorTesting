using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using System.Linq;

namespace AkkaActorTesting.Actors
{
    public class PythonPlugin : ReceiveActor
    {
        public IActorRef Parent { get; private set; }
        public Process process { get; internal set; } = null;
        public IDictionary<string, string> EnvironmentVariables { get; private set; }

        public PythonPlugin(IActorRef parent, IDictionary<string, string> environmentVariables)
        {
            this.Parent = parent;
            this.EnvironmentVariables = environmentVariables;

            Receive<string>(m =>
            {
                return m.StartsWith("Start");
                
            }, m =>
            {
                DoSomething(Self, EnvironmentVariables).ContinueWith(result =>
                {
                    Console.WriteLine($"Exited with: {result.Result.Result}");
                    return result.Result;
                }, TaskContinuationOptions.ExecuteSynchronously).PipeTo(Self);
            });

            Receive<string>(m =>
            {
                return m.StartsWith("Stop");
            },
            m =>
            {
                if (process != null)
                {
                    process.Kill();
                }
            });

            Receive<ProcessEndedMessage>(m =>
            {                
                if (m.Result != 0)
                {
                    throw new PluginException("Plugin didn't exit cleanly.");
                }

                Self.GracefulStop(TimeSpan.FromSeconds(5), "Process exited cleanly");
            });
        }

        
        

        public class ProcessEndedMessage
        {
            public int Result { get; internal set; }

            public ProcessEndedMessage(int result)
            {
                Result = result;
            }
        }

        

        //protected override void OnReceive(object message)
        //{
        //    if (message.Equals("Start"))
        //    {
        //        DoSomething(Self).ContinueWith(result =>
        //        {
        //            Console.WriteLine($"Exited with: {result.Result.Result}");
        //            return result.Result;
        //        }, TaskContinuationOptions.ExecuteSynchronously).PipeTo(Self);
        //    }
        //    else if (message.Equals("Stop"))
        //    {
        //        if (process != null)
        //        {
        //            process.Kill();
        //        }
        //    }
        //    else if (message is ProcessEndedMessage)
        //    {
        //        var pem = message as ProcessEndedMessage;
        //        if (pem.Result != 0)
        //        {
        //            throw new PluginException("Plugin didn't exit cleanly.");
        //        }

        //        Self.GracefulStop(TimeSpan.FromSeconds(5), "Process exited cleanly");
        //    }
        //}

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

        private Task<ProcessEndedMessage> DoSomething(IActorRef myself, IDictionary<string, string> env)
        {
            var tcs = new TaskCompletionSource<ProcessEndedMessage>();

            process = new Process();
            process.StartInfo.FileName = @"C:\Users\ttutk\AppData\Local\Programs\Python\Python37\python.exe";
            process.StartInfo.Arguments = "Program.py";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.Environment.Add("SPT_EnvVar", $"Myfile_{myself.Path.Name}.txt");

            foreach (var ev in env)
            {
                process.StartInfo.Environment.Add(ev.Key, ev.Value);
            }

            if (myself.Path.Name == "child_0")
            {
                process.StartInfo.Environment.Add("SPT_MaxIters", "5");
            }
            if (myself.Path.Name == "child_3")
            {
                process.StartInfo.Environment.Add("SPT_MaxIters", "1");
                process.StartInfo.Environment.Add("SPT_ExitCode", "1");
            }
            if (myself.Path.Name == "child_4")
            {
                process.StartInfo.Environment.Add("SPT_MaxIters", "10");
                process.StartInfo.Environment.Add("SPT_ExitCode", "3");
            }
            process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();

            process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    Parent.Tell(new MySupervisor.ConsoleMessage(e.Data), myself);
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
