using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eng_FolderMetrics
{
    internal static class Helpers
    {
        static long CalculateDirectorySize(string directoryPath)
        {
            long totalSize = 0;

            // Calculate sizes of all files in the directory using Directory.EnumerateFiles
            totalSize += Directory.EnumerateFiles(directoryPath).Sum(file => new FileInfo(file).Length);

            // Calculate sizes of all subdirectories in the directory using Directory.EnumerateDirectories
            totalSize += Directory.EnumerateDirectories(directoryPath).Sum(subdirectory => CalculateDirectorySize(subdirectory));

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
