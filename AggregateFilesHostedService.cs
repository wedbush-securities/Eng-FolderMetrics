using Microsoft.Extensions.Hosting;

namespace Eng_FolderMetrics
{
    internal sealed class AggregateFilesHostedService : IHostedService
    {
        private readonly Serilog.ILogger _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly CopyProcessor _copyProcessor = new CopyProcessor();

        public AggregateFilesHostedService(
            Serilog.ILogger logger,
            IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _appLifetime = appLifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Information($"Starting with arguments: {string.Join(" ", Environment.GetCommandLineArgs())}");
            _appLifetime.ApplicationStarted.Register(() =>
            {
                Task.Run(() =>
                {
                    try
                    {
                        _logger.Information("Start Check:AggregateFiles");

                        string? sourceFolder = System.Configuration.ConfigurationManager.AppSettings["FolderWithMultipleFiles"];
                        _logger.Information($"App Setting Value: {sourceFolder}");

                        string? aggregateFolder = System.Configuration.ConfigurationManager.AppSettings["FolderToAggregate"];
                        _logger.Information($"App Setting Value: {aggregateFolder}");

                        if (sourceFolder != null)
                        {
                            string[] files = Directory.GetFiles(path: sourceFolder, searchPattern: "*.*", searchOption: SearchOption.AllDirectories);

                            Parallel.ForEach(files,
                                file =>
                                {
                                    _copyProcessor.CopyFilesToOneFolder(file, aggregateFolder);
                                    _logger.Information($"Completed copying {file} to Folder {aggregateFolder}");
                                });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Fatal(ex, "Unhandled exception!");
                    }
                    finally
                    {
                        // Stop the application once the work is done
                        _appLifetime.StopApplication();
                    }

                    return Task.CompletedTask;
                }, cancellationToken); // Add the missing closing curly brace here
            });

            return Task.CompletedTask;
        }

        

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
