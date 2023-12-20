using Microsoft.Extensions.DependencyInjection;
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

            // create host
            var host = CreateHostBuilder(args).Build();

            // add logic to get input from user and analyze folder
            Console.WriteLine("Enter the path to the folder you want to analyze: ");
            string path = Console.ReadLine();

            // create worker and assign path property

            var worker = ActivatorUtilities.CreateInstance<Worker>(host.Services);
            worker.Path = path;
            //call worker method to analyze folder
            worker.AnalyzeFolder();

            
            try
            {
                Log.Information("Starting up");
               host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush(); // close logger
            }
        }

     public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog() // use serilog
                     .ConfigureServices((hostContext, services) =>
                {   
                    // create singleton worker
                    services.AddSingleton<Worker>();
                    
                    //services.AddHostedService<Worker>(); // assign worker property
                   // services.AddSingleton<Worker>(new Worker() { Path = args[0] }); // assign path property

                });
    }

}
