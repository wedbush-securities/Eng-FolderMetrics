﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Eng_FolderMetrics
{
    
    // create console application with logging and exception handing
    public class Program
    {
        public static void Main(string[] args)
        {
            // create logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs\\log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("Please enter a numeric Value");
            Log.Information("Usage: 1. Analyze Folders  2. Copy Folders 3. Find Files and aggregate to One Folder");
            // Get User Input
            var input = Console.ReadLine();

            try
            {
                var argTest = int.TryParse(input, out var num);
                if (argTest && num == 1)
                {
                    Log.Information("Starting up the Folder Analysis");
                    CreateFolderAnalyzeHostBuilder(args).Build().Run();
                }
                else if (num == 2)
                {
                    Log.Information("Starting up the Deep Copy");
                    CreateDeepCopyHostBuilder(args).Build().Run();
                }
                else
                {
                    Log.Information("Starting up the Aggregate Files");
                    CreateAggregateFilesHostBuilder(args).Build().Run();
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
        public static IHostBuilder CreateFolderAnalyzeHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddHostedService<FolderAnalyzeHostedService>().AddSerilog(Log.Logger);
                });

        public static IHostBuilder CreateDeepCopyHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddHostedService<DeepCopyHostedService>().AddSerilog(Log.Logger);
                });

        public static IHostBuilder CreateAggregateFilesHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddHostedService<AggregateFilesHostedService>().AddSerilog(Log.Logger);
                });
    }

}
