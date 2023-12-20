using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Eng_FolderMetrics
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public string Path { get; set; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

        internal void AnalyzeFolder()
        {
            if (Directory.Exists(Path))
            {
                long totalSize = CalculateDirectorySize(Path);
                Console.WriteLine($"Total size of {Path}: {FormatBytes(totalSize)}");
            }
            else
            {
                Console.WriteLine("Invalid directory path. Please provide a valid path.");
            }
        }

        static long CalculateDirectorySize(string directoryPath)
        {
            long totalSize = 0;

            // Iterate through all files in the directory
            string[] files = Directory.GetFiles(directoryPath);
            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                totalSize += fileInfo.Length;
            }

            // Iterate through all subdirectories in the directory
            string[] subdirectories = Directory.GetDirectories(directoryPath);
            foreach (string subdirectory in subdirectories)
            {
                totalSize += CalculateDirectorySize(subdirectory);
            }

            return totalSize;
        }
        static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;

            while (bytes >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                bytes /= 1024;
                suffixIndex++;
            }

            return $"{bytes} {suffixes[suffixIndex]}";
        }
    }
}