using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Eng_FolderMetrics
{
    internal sealed class ConsoleHostedService : IHostedService
    {
        private readonly Serilog.ILogger _logger;
        private readonly IHostApplicationLifetime _appLifetime;

        public ConsoleHostedService(
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
                        _logger.Information("Start Check");

                        string appSettingValue = System.Configuration.ConfigurationManager.AppSettings["Folders"];
                        _logger.Information($"App Setting Value: {appSettingValue}");

                        string[] folders = appSettingValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(f => f.Trim())
                            .ToArray();

                        foreach (string folder in folders)
                        {
                            try
                            {
                                string sanitizedFolder = Path.GetFullPath(folder);

                                // Rest of the code...

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
                });
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        static long GetFileLength(string filename)
        {
            long retval;
            try
            {
                System.IO.FileInfo fi = new System.IO.FileInfo(filename);
                retval = fi.Length;
            }
            catch (System.IO.FileNotFoundException)
            {
                // If a file is no longer present,  just add zero bytes to the total.  
                retval = 0;
            }
            return retval;
        }
    }

    
}