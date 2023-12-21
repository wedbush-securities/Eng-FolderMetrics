using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eng_FolderMetrics
{
    internal sealed class AggregateFilesHostedService : IHostedService
    {
        private readonly Serilog.ILogger _logger;
        private readonly IHostApplicationLifetime _appLifetime;

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
                Task.Run(async () =>
                {
                    try
                    {
                        _logger.Information("Start Check:AgggregateFiles");

                        string? sourceFolder = System.Configuration.ConfigurationManager.AppSettings["FolderwithMultipleFiles"];
                        _logger.Information($"App Setting Value: {sourceFolder}");

                        string? aggregateFolder = System.Configuration.ConfigurationManager.AppSettings["FoldertoAggregate"];
                        _logger.Information($"App Setting Value: {aggregateFolder}");

                        string[] files = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories);

                        foreach (var file in files)
                        {
                            File.Copy(file, aggregateFolder+ "\\" + Path.GetFileName(file), true);
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
