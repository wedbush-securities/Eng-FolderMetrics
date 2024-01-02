using Eng_FolderMetrics.HostedServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Eng_FolderMetrics
{
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
            Log.Information("Usage: 1. Analyze Folders \n 2. Copy Folders \n 3. Find Files and aggregate to One Folder \n 4. Copy files and upload to SharePoint \n 5. FolderAnalyze -Files older than Date");
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
                else if (num == 3)
                {
                    Log.Information("Starting up the Aggregate Files");
                    CreateAggregateFilesHostBuilder(args).Build().Run();
                }
                else if (num == 4)
                {
                    Log.Information("Starting up the Copy Files and Upload to SharePoint");
                    CreateSharePointHostBuilder(args).Build().Run();
                }
                else if (num == 5)
                {
                    Log.Information("Starting up the File Analyzer");
                    createFileAnalyzeHostBuilder(args).Build().Run();
                }
                else
                {
                    Log.Information($"NO logic for this selection {num}");
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

        private static IHostBuilder createFileAnalyzeHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddHostedService<FileAnalyzeHostedService>().AddSerilog(Log.Logger);
                });

        private static IHostBuilder CreateSharePointHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddHostedService<SharePointHostedService>().AddSerilog(Log.Logger);
                });

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
