using Microsoft.Extensions.Hosting;

namespace Eng_FolderMetrics
{
    internal sealed class DeepCopyHostedService : IHostedService
    {
        private readonly Serilog.ILogger _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly CopyProcessor _copyProcessor = new CopyProcessor();

        public DeepCopyHostedService(
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
                        _logger.Information("Start Check:DeepCopy");

                        string? appSettingValue = System.Configuration.ConfigurationManager.AppSettings["DeepCopy"];
                        _logger.Information($"App Setting Value: {appSettingValue}");

                        string[] folders = appSettingValue?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(f => f.Trim())
                            .ToArray() ?? throw new InvalidOperationException("Invalid DeepCopy App Settings");

                        Parallel.ForEach(folders, DeepCopyProcessing);
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

        private void DeepCopyProcessing(string? folderInfo)
        {
            string[] copyFolder = folderInfo?.Split(new[] { '>' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim())
                .ToArray() ?? throw new InvalidOperationException("Invalid Info in DeepCopy App Settings");

            if (copyFolder == null || copyFolder.Length < 2)
            {
                throw new InvalidOperationException("Invalid Info in DeepCopy App Settings in" + folderInfo);
            }
            string fromFolder = copyFolder[0];
            string toFolder = copyFolder[1];

            _copyProcessor.Copy(fromFolder, toFolder);
            _logger.Information($"Completed copying from {fromFolder} over to {toFolder} ");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
