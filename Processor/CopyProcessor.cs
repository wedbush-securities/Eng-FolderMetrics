using Microsoft.Extensions.DependencyInjection;

namespace Eng_FolderMetrics.Processor;

internal sealed class CopyProcessor
{
    public void Copy(string? sourceDirectory, string? targetDirectory)
    {
        var diSource = new DirectoryInfo(sourceDirectory);
        var diTarget = new DirectoryInfo(targetDirectory);

        CopyAll(diSource, diTarget);
    }

    public void CopyAll(DirectoryInfo source, DirectoryInfo target)
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

    public void CopyFilesToOneFolder(string file, string? aggregateFolder)
    {
        File.Copy(file, aggregateFolder + "\\" + Path.GetFileName(file), true);
    }
}
