using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Eng_FolderMetrics.Model;
using Eng_FolderMetrics.Processor;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using Serilog;
using static Eng_FolderMetrics.Processor.SharepointProcessor;

namespace Eng_FolderMetrics.HostedServices
{
    internal sealed class SharePointHostedService : IHostedService
    {
        private readonly Serilog.ILogger _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly CopyProcessor _copyProcessor = new CopyProcessor();

        public SharePointHostedService(
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
                        _logger.Information("Start Check:SharePoint Service");
                        
                        // Get config settings from SharePointConfig.json
                        var lstSharePointInfo = ListSharePointInfo(@"SharePointConfig.json");
                        if (lstSharePointInfo != null)
                        {
                            _logger.Information($"Got config Entries Count:{lstSharePointInfo.Count}");

                            foreach (var info in lstSharePointInfo)
                            {
                                if (info.username != null)
                                {
                                    SecureString? pwd = GetPassword(_logger, info.username);
                                    // set password on all the objects
                                    if (pwd != null && pwd.Length > 1)
                                    {
                                        info.pass = pwd;
                                        pwd= null;
                                    }
                                    SharePointProcessing(info);
                                }
                            }
                            //lstSharePointInfo.Select(c =>
                            //{
                            //    c.pass = pwd;
                            //    return c;
                            //}).ToList();
                            // Parallel.ForEach(lstSharePointInfo, SharePointProcessing);
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

        private SecureString? GetPassword(ILogger logger, string username)
        {
            SecureString? sStrPwd = new SecureString();
            try
            {
                logger.Information($"Type Password for Username: {username} ");

                for (ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                     keyInfo.Key != ConsoleKey.Enter;
                     keyInfo = Console.ReadKey(true))
                {
                    if (keyInfo.Key == ConsoleKey.Backspace)
                    {
                        if (sStrPwd.Length > 0)
                        {
                            sStrPwd.RemoveAt(sStrPwd.Length - 1);
                            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                            logger.Information(" ");
                            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        }
                    }
                    else if (keyInfo.Key != ConsoleKey.Enter)
                    {
                        logger.Information("*");
                        sStrPwd.AppendChar(keyInfo.KeyChar);
                    }

                }

                logger.Information("");
            }
            catch (Exception e)
            {
                sStrPwd = null;
                logger.Fatal($"Error getting Password{e.Message}");
            }
            return sStrPwd;
        }


        private void SharePointProcessing(SharePointInfo? spInfo)
        {
            if (spInfo != null && !string.IsNullOrEmpty(spInfo.fromfolder) && !string.IsNullOrEmpty(spInfo.tofolder))
            {
                //_copyProcessor.Copy(spInfo.fromfolder, spInfo.tofolder);
                SharepointProcessor processor = new SharepointProcessor();
                processor.UploadToSharePoint(spInfo);
            }
            else
            {
                _logger.Fatal($"Invalid SharePoint Info:{spInfo}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
