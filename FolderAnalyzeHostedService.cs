using Microsoft.Extensions.Hosting;

namespace Eng_FolderMetrics
{
    internal sealed class FolderAnalyzeHostedService : IHostedService
    {
        private readonly Serilog.ILogger _logger;
        private readonly IHostApplicationLifetime _appLifetime;

        public FolderAnalyzeHostedService(
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
                        _logger.Information("Start Check:FolderAnalysis");

                        string? appSettingValue = System.Configuration.ConfigurationManager.AppSettings["AnalyzeFolders"];
                        _logger.Information($"App Setting Value: {appSettingValue}");

                        string[] folders = appSettingValue?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(f => f.Trim())
                            .ToArray() ?? throw new InvalidOperationException("Invalid AnalyzeFolders App Settings");
                        
                        Parallel.ForEach(folders, FolderProcessor);
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

        internal void FolderProcessor(string folder)
        {
            try
            {
                string sanitizedFolder = Path.GetFullPath(folder);

                long totalBytes = 0;
                int fileCount = 0;

                foreach (string file in Directory.EnumerateFiles(sanitizedFolder, "*.*", SearchOption.AllDirectories))
                {
                    long length = GetFileLength(file);
                    totalBytes += length;
                    fileCount++;
                }

                _logger.Information($"There are {totalBytes} bytes in {fileCount} files under {sanitizedFolder}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Invalid folder path: {folder}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private static long GetFileLength(string filename)
        {
            long retrieval;
            try
            {
                var fi = new FileInfo(filename);
                retrieval = fi.Length;
            }
            catch (FileNotFoundException)
            {
                // If a file is no longer present,  just add zero bytes to the total.  
                retrieval = 0;
            }
            return retrieval;
        }
    }

    
}