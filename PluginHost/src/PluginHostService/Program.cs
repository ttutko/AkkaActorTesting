using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Serilog;
using Serilog.Configuration;



namespace PluginHostService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(hostContext.Configuration)
                        .CreateLogger();

                    var connectionString = hostContext.Configuration["spt:database:connectionString"];
                    var databaseName = hostContext.Configuration["spt:database:databaseName"];

                    var client = new MongoClient(connectionString);
                    var database = client.GetDatabase(databaseName);

                    services.AddSingleton<IMongoDatabase>(database);
                    services.AddTransient<PluginRepository>();
                    services.AddLogging(loggingBuilder => { loggingBuilder.AddSerilog(); });

                    services.AddHostedService<PluginHostService>();
                });
    }
}
