using System.Net;
using Eng_FolderMetrics.Model;
using Newtonsoft.Json;
using Microsoft.SharePoint.Client;
using System.Security;

namespace Eng_FolderMetrics.Processor
{
    public sealed class SharepointProcessor
    {
        public static List<SharePointInfo>? ListSharePointInfo(string filename)
        {
            string json = System.IO.File.ReadAllText(filename);
            var lstSharePointInfo = JsonConvert.DeserializeObject<List<SharePointInfo>>(json);

            return lstSharePointInfo;
        }

        public Microsoft.SharePoint.Client.File UploadFileSlicePerSlice(ClientContext ctx, string libraryName, string fileName, int fileChunkSizeInMB = 3)
        {
            // Each sliced upload requires a unique ID.
            Guid uploadId = Guid.NewGuid();

            // Get the name of the file.
            string uniqueFileName = Path.GetFileName(fileName);

            // Ensure that target library exists, and create it if it is missing.
            if (!LibraryExists(ctx, ctx.Web, libraryName))
            {
                CreateLibrary(ctx, ctx.Web, libraryName);
            }
            // Get the folder to upload into.
            List docs = ctx.Web.Lists.GetByTitle(libraryName);
            ctx.Load(docs, l => l.RootFolder);
            // Get the information about the folder that will hold the file.
            ctx.Load(docs.RootFolder, f => f.ServerRelativeUrl);
            ctx.ExecuteQuery();

            // File object.
            Microsoft.SharePoint.Client.File uploadFile = null;

            // Calculate block size in bytes.
            int blockSize = fileChunkSizeInMB * 1024 * 1024;

            // Get the information about the folder that will hold the file.
            ctx.Load(docs.RootFolder, f => f.ServerRelativeUrl);
            ctx.ExecuteQuery();


            // Get the size of the file.
            long fileSize = new FileInfo(fileName).Length;

            if (fileSize <= blockSize)
            {
                // Use regular approach.
                using (FileStream fs = new FileStream(fileName, FileMode.Open))
                {
                    FileCreationInformation fileInfo = new FileCreationInformation();
                    fileInfo.ContentStream = fs;
                    fileInfo.Url = uniqueFileName;
                    fileInfo.Overwrite = true;
                    uploadFile = docs.RootFolder.Files.Add(fileInfo);
                    ctx.Load(uploadFile);
                    ctx.ExecuteQuery();
                    // Return the file object for the uploaded file.
                    return uploadFile;
                }
            }
            else
            {
                // Use large file upload approach.
                ClientResult<long> bytesUploaded = null;

                FileStream fs = null;
                try
                {
                    fs = System.IO.File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        byte[] buffer = new byte[blockSize];
                        Byte[] lastBuffer = null;
                        long fileoffset = 0;
                        long totalBytesRead = 0;
                        int bytesRead;
                        bool first = true;
                        bool last = false;

                        // Read data from file system in blocks.
                        while ((bytesRead = br.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            totalBytesRead = totalBytesRead + bytesRead;

                            // You've reached the end of the file.
                            if (totalBytesRead == fileSize)
                            {
                                last = true;
                                // Copy to a new buffer that has the correct size.
                                lastBuffer = new byte[bytesRead];
                                Array.Copy(buffer, 0, lastBuffer, 0, bytesRead);
                            }

                            if (first)
                            {
                                using (MemoryStream contentStream = new MemoryStream())
                                {
                                    // Add an empty file.
                                    FileCreationInformation fileInfo = new FileCreationInformation();
                                    fileInfo.ContentStream = contentStream;
                                    fileInfo.Url = uniqueFileName;
                                    fileInfo.Overwrite = true;
                                    uploadFile = docs.RootFolder.Files.Add(fileInfo);

                                    // Start upload by uploading the first slice.
                                    using (MemoryStream s = new MemoryStream(buffer))
                                    {
                                        // Call the start upload method on the first slice.
                                        bytesUploaded = uploadFile.StartUpload(uploadId, s);
                                        ctx.ExecuteQuery();
                                        // fileoffset is the pointer where the next slice will be added.
                                        fileoffset = bytesUploaded.Value;
                                    }

                                    // You can only start the upload once.
                                    first = false;
                                }
                            }
                            else
                            {
                                if (last)
                                {
                                    // Is this the last slice of data?
                                    using (MemoryStream s = new MemoryStream(lastBuffer))
                                    {
                                        // End sliced upload by calling FinishUpload.
                                        uploadFile = uploadFile.FinishUpload(uploadId, fileoffset, s);
                                        ctx.ExecuteQuery();

                                        // Return the file object for the uploaded file.
                                        return uploadFile;
                                    }
                                }
                                else
                                {
                                    using (MemoryStream s = new MemoryStream(buffer))
                                    {
                                        // Continue sliced upload.
                                        bytesUploaded = uploadFile.ContinueUpload(uploadId, fileoffset, s);
                                        ctx.ExecuteQuery();
                                        // Update fileoffset for the next slice.
                                        fileoffset = bytesUploaded.Value;
                                    }
                                }
                            }
                        } // while ((bytesRead = br.Read(buffer, 0, buffer.Length)) > 0)
                    }
                }
                finally
                {
                    if (fs != null)
                    {
                        fs.Dispose();
                    }
                }
            }

            return null;
        }

        public async Task UploadToSharePoint(SharePointInfo sharePointInfo)
        {
            Uri web = new Uri(sharePointInfo.siteurl);
            string userPrincipalName = sharePointInfo.username;
            SecureString userPassword =sharePointInfo.pass;

            using (var authenticationManager = new AuthenticationManager())
            using (var context = authenticationManager.GetContext(web, userPrincipalName, userPassword))
            {
                context.Load(context.Web, p => p.Title);
                await context.ExecuteQueryAsync();
                Console.WriteLine($"Title: {context.Web.Title}");
            }
        }

        private string GetAnAccessToken(Uri uri, string userPrincipalName, string password)
        {
            throw new NotImplementedException();
        }

        private bool LibraryExists(ClientContext ctx, Web web, string libraryName)
        {
            ListCollection lists = web.Lists;
            IEnumerable<List> results = ctx.LoadQuery<List>(lists.Where(list => list.Title == libraryName));
            ctx.ExecuteQuery();
            List existingList = results.FirstOrDefault();

            if (existingList != null)
            {
                return true;
            }

            return false;
        }

        private void CreateLibrary(ClientContext ctx, Web web, string libraryName)
        {
            // Create library to the web
            ListCreationInformation creationInfo = new ListCreationInformation();
            creationInfo.Title = libraryName;
            creationInfo.TemplateType = (int)ListTemplateType.DocumentLibrary;
            List list = web.Lists.Add(creationInfo);
            ctx.ExecuteQuery();
        }

        public async static void Uploadfiles(SharePointInfo sharePointInfo)
        {
            try
            {
                /*
                using (var PnP.Framework.authenticationManager = new AuthenticationManager())
                using (var context = authenticationManager.GetContext(site, user, password))
                {
                    context.Load(context.Web, p => p.Title);
                    context.ExecuteQuery();
                    Microsoft.SharePoint.Client.File file = context.Web.GetFileByUrl("https://tenant.sharepoint.com/sites/michael/Shared%20Documents/aa.txt");
                    context.Load(file);
                    context.ExecuteQuery();
                    string filepath = @"C:\temp\" + file.Name;



                    Microsoft.SharePoint.Client.ClientResult<Stream> mstream = file.OpenBinaryStream();
                    context.ExecuteQuery();

                    using (var fileStream = new System.IO.FileStream(filepath, System.IO.FileMode.Create))
                    {
                        mstream.Value.CopyTo(fileStream);
                    }


                    using (System.IO.StreamReader sr = new System.IO.StreamReader(mstream.Value))
                    {
                        String line = sr.ReadToEnd();
                        Console.WriteLine(line);
                    }

                    

                    using (ClientContext ctx = new ClientContext(sharePointInfo.siteurl))
                    {
                        SecureString securePassword = new SecureString();
                        foreach (char c in sharePointInfo.pass.ToCharArray())
                        {
                            securePassword.AppendChar(c);
                        }

                        ctx.AuthenticationMode = ClientAuthenticationMode.Default;
                        ctx.Credentials = new SharePointOnlineCredentials(userName, securePassword);

                        if (ctx != null)
                        {
                            var lib = ctx.Web.Lists.GetByTitle("mesdocuments");
                            ctx.Load(lib);
                            ctx.Load(lib.RootFolder);
                            ctx.ExecuteQuery();
                            string sourceUrl = "C:\\temp\\picture.jpg";
                            using (FileStream fs = new FileStream(sourceUrl, FileMode.Open, FileAccess.Read))
                            {
                                Microsoft.SharePoint.Client.File.SaveBinaryDirect(ctx, lib.RootFolder.ServerRelativeUrl + "/myPicture.jpg", fs, true);
                            }
                        }

                    }

                    PnP.Framework.AuthenticationManager authMgr = new PnP.Framework.AuthenticationManager();

                    using (var ctx = new ClientContext(sharePointInfo.siteurl))
                    {
                        ctx.Credentials = new NetworkCredential(sharePointInfo.username, sharePointInfo.pass, "wedbush"); ;

                        //UploadFile(ctx, "LibName/FolderName/Sub Folder Name/Sub Sub Folder Name/Sub Sub Sub Folder Name", filePath);

                        var folder = ctx.Web.GetFolderByServerRelativeUrl(sharePointInfo.tofolder);
                        ctx.Load(folder);
                        ctx.ExecuteQuery();

                        var fileName = System.IO.Path.GetFileName(sharePointInfo.tofolder);
                        var fileUrl = String.Format("{0}/{1}", sharePointInfo.tofolder, fileName);

                        using (var fs = new FileStream(sharePointInfo.tofolder, FileMode.Open))
                        {
                            var fi = new FileInfo(sharePointInfo.tofolder);
                            Microsoft.SharePoint.Client.File.SaveBinaryDirect(ctx, fileUrl, fs, true);
                        }

                        var uploadedFile = ctx.Web.GetFileByServerRelativeUrl(fileUrl);
                        ctx.Load(uploadedFile);
                        ctx.ExecuteQuery();
                    }


                    //var authMgr = new AuthenticationManager();
                    using (var ctx = authMgr.GetSharePointOnlineAuthenticatedContextTenant(sharePointInfo.siteurl, sharePointInfo.username, sharePointInfo.pass))
                    {
                        Web web = ctx.Web;
                        ctx.Load(web);
                        ctx.Load(web.Lists);
                        ctx.ExecuteQueryRetry();
                        List list = web.Lists.GetByTitle("D1");
                        ctx.Load(list);
                        ctx.ExecuteQueryRetry();
                        Folder folder = list.RootFolder.EnsureFolder("Folder1");
                        ctx.Load(folder);
                        ctx.ExecuteQueryRetry();

                        Folder folderToUpload = web.GetFolderByServerRelativeUrl(folder.ServerRelativeUrl);
                        folderToUpload.UploadFile("LargeFile.txt", @"c:\Divya\LargeFile.txt", true);
                        folderToUpload.Update();
                        ctx.Load(folder);
                        ctx.ExecuteQueryRetry();
                        folderToUpload.EnsureProperty(f => f.ServerRelativeUrl);
                        var serverRelativeUrl = folderToUpload.ServerRelativeUrl.TrimEnd('/') + '/' + "LargeFile.txt";

                    }
                    */
                }
            catch (Exception ex)
            {
                System.Console.WriteLine("Exception occurred : " + ex.Message);
                System.Console.ReadLine();
            }
        }
    }
}
