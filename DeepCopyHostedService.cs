using Microsoft.Extensions.Hosting;

namespace Eng_FolderMetrics
{
    internal sealed class DeepCopyHostedService : IHostedService
    {
        private readonly Serilog.ILogger _logger;
        private readonly IHostApplicationLifetime _appLifetime;

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
                Task.Run(async () =>
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
                }, cancellationToken); // Add the missing closing curly brace here
            });

            return Task.CompletedTask;
        }

        private void DeepCopyProcessing(string folderInfo)
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

            Copy(fromFolder, toFolder);
            
            // Copy all contents from fromFolder to toFolder
            /*
            string[] files = Directory.GetFiles(fromFolder, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string relativePath = Path.GetRelativePath(fromFolder, file);
                string destinationPath = Path.Combine(toFolder, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                File.Copy(file, destinationPath, true);
            }
            */
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Copy(string sourceDirectory, string targetDirectory)
        {
            var diSource = new DirectoryInfo(sourceDirectory);
            var diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
            _logger.Information($"Completed copying from {diSource} over to {diTarget} ");
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each sub-directory using recursion.
            foreach (var diSourceSubDir in source.GetDirectories())
            {
                var nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
    }
}
