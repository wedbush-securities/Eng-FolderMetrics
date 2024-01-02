using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Security.AccessControl;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Text;
using System.Data;

namespace Eng_FolderMetrics.HostedServices
{
    internal sealed class FileAnalyzeHostedService : IHostedService
    {
        private readonly Serilog.ILogger _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private string? fileDate;

        public FileAnalyzeHostedService(
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

                        string? folderValue = System.Configuration.ConfigurationManager.AppSettings["AnalyzeRootFolder"];
                        _logger.Information($"App Setting Value: {folderValue}");

                        fileDate = System.Configuration.ConfigurationManager.AppSettings["AnalyzeRootFolderDate"];
                        _logger.Information($"App Setting Value: {fileDate}");

                        if (!string.IsNullOrEmpty(folderValue) && !string.IsNullOrEmpty(fileDate))
                        {
                            string[] folders = Directory.GetDirectories(folderValue);

                            //Parallel.ForEach(folders, FileProcessor);
                            Parallel.ForEach(folders, FileProcessor);
                            //foreach (var folder in folders)
                            //{
                            //    FileProcessor(folder, fileDate);
                            //}
                        }
                        else
                        {
                            _logger.Information($"Incorrect App Setting Values: {folderValue}{fileDate}");
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

        private void FileProcessor(string folder)
        {
            try
            {

                if (!DateTime.TryParse(fileDate, out DateTime targetDate))
                {
                    _logger.Error($"Invalid date: {fileDate}");
                    return;
                }

                string sanitizedFolder = Path.GetFullPath(folder);
                DirectoryInfo directoryInfo = new DirectoryInfo(sanitizedFolder);
                if (!directoryInfo.Exists)
                {
                    _logger.Error($"Invalid folder path: {sanitizedFolder}");
                    return;
                }

                long totalBytes = 0;
                int fileCount = 0;

                foreach (FileInfo fileInfo in directoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories))
                {
                    if (fileInfo.LastWriteTime >= targetDate)
                    {
                        totalBytes += fileInfo.Length;
                        fileCount++;
                    }
                }

                _logger.Information($"Folder: {sanitizedFolder}  \t {totalBytes} bytes \t {fileCount} files \t {fileDate}");

                //Security Information
                // Create a new DirectoryInfo object.
                GetSecurityInfoforDir(folder);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Invalid folder path: {folder}, {fileDate}");
            }
        }

        private void GetSecurityInfoforDir(string folder)
        {
            DirectoryInfo dInfo = new DirectoryInfo(folder);
            StringBuilder strSecurityData = new StringBuilder();

            DirectorySecurity directorySecurity = dInfo.GetAccessControl();
            foreach (FileSystemAccessRule rule in directorySecurity.GetAccessRules(true, true,
                         typeof(System.Security.Principal.NTAccount)))
            {

                strSecurityData.Append(($"Group:{rule.IdentityReference.Value}"));

                using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
                {
                    GroupPrincipal group = GroupPrincipal.FindByIdentity(context, rule.IdentityReference.Value);
                    
                    if (group != null)
                    {
                        foreach (Principal user in group.GetMembers())
                        {
                            strSecurityData.Append(($"User: {user.SamAccountName}"));
                        }
                    }
                }
            }
            _logger.Information($"FolderSecurity:{folder} \t Security: {strSecurityData}");
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
