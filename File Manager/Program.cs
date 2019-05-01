using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Drive.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Google.Apis.Services;
using System.Collections.Generic;

namespace DriveFileManager
{
    class Program
    {
        static readonly string[] Scopes = { DriveService.Scope.Drive, DriveService.Scope.DriveFile};
        static string applicationName = "GDrive File Manager";
       
        static void Main(string[] args)
        {
            try
            {
                new Program().Run().Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("ERROR: " + e.Message);
                }
            }
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private async Task Run()
        {
            
            UserCredential credential;

            using (var stream = new FileStream(Directory.GetCurrentDirectory() + @"\client_id.json", FileMode.Open, FileAccess.Read))
            {
                string credentialPath = Directory.GetCurrentDirectory() + @"\token.json";

                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets, Scopes, "user", CancellationToken.None, new FileDataStore(credentialPath, true));
            }

            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });

            DirectoryInfo directory = new DirectoryInfo(GetWorkspaceLocation());

            Console.WriteLine("Checking Files..." + Environment.NewLine);
            
            IList<Google.Apis.Drive.v3.Data.File> driveFolders = GetDriveFolders("root", service); //gdrive root

            foreach (var currentFolder in directory.GetDirectories())
            {
                if (currentFolder.Name == "GDrive File Manager") continue;
                SearchFolders(currentFolder, service, "root"); //recursion
            }
            {
                //foreach (var tempFile in directory.GetFiles())
                //{
                //    if (tempFile.Name.Contains(".lnk")) continue; //shortcut

                //    var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                //    {
                //        Name = tempFile.ToString()
                //    };

                //    bool fileExists = false;

                //    DateTime fileModifiedTime = tempFile.LastWriteTime;

                //    FilesResource.CreateMediaUpload uploadRequest;

                //    FilesResource.ListRequest listRequest = service.Files.List();
                //    listRequest.PageSize = 50;
                //    listRequest.Q = "'root' in parents  and trashed=false"; //samo main drive folder
                //    listRequest.Spaces = "drive";
                //    listRequest.Fields = "files(id, name, modifiedTime, createdTime)";

                //    IList<Google.Apis.Drive.v3.Data.File> driveFiles = listRequest.Execute().Files;
                //    if (driveFiles != null && driveFiles.Count > 0)
                //    {
                //        foreach (var driveFile in driveFiles)
                //        {
                //            //Console.WriteLine(driveFile.Name + " " + driveFile.Id);

                //            if (driveFile.Name == tempFile.Name)
                //            {
                //                fileExists = true;

                //                if (driveFile.ModifiedTime < fileModifiedTime)
                //                {
                //                    FilesResource.DeleteRequest deleteRequest;
                //                    deleteRequest = service.Files.Delete(driveFile.Id);
                //                    var deleteFile = deleteRequest.Execute();

                //                    fileExists = false; //deleted
                //                }
                //            }
                //        }
                //    }
                //    if (!fileExists)
                //    {
                //        using (var stream = new FileStream(getWorkspaceLocation() + "\\" + tempFile.ToString(),
                //                                            FileMode.Open))
                //        {
                //            uploadRequest = service.Files.Create(fileMetadata, stream, mimeTypeLookup.GetMimeType(tempFile.ToString()));
                //            uploadRequest.Upload();
                //        }
                //        var uploadFile = uploadRequest.ResponseBody;
                //        Console.WriteLine(tempFile.ToString() + "\t Uploaded successfully.");
                //    }

                //}
            }
            Console.WriteLine(Environment.NewLine + "All files are up-to-date." + Environment.NewLine);
        }

        private string GetWorkspaceLocation()
        {
            const int EXE_FOLDER_LENGTH = 19; //GDrive File Manager - 19 znakova
            string temp = Directory.GetCurrentDirectory();
            return temp.Remove(temp.Length-EXE_FOLDER_LENGTH);
        }

        private IList<Google.Apis.Drive.v3.Data.File> GetDriveFolders(string folderId, DriveService service)
        {
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 50;
            listRequest.Q = "'" + folderId + "'" + " in parents and trashed=false and mimeType='application/vnd.google-apps.folder'";
            listRequest.Spaces = "drive";
            listRequest.Fields = "files(id, name)";
            IList<Google.Apis.Drive.v3.Data.File> driveFolders = listRequest.Execute().Files;

            return driveFolders;
        }

        private IList<Google.Apis.Drive.v3.Data.File> GetDriveFiles(string folderId, DriveService service)
        {
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 100;
            listRequest.Q = "'" + folderId + "'" + "in parents and trashed=false and mimeType!='application/vnd.google-apps.folder'";
            listRequest.Spaces = "drive";
            listRequest.Fields = "files(id, name, modifiedTime, createdTime)";
            IList<Google.Apis.Drive.v3.Data.File> driveFiles = listRequest.Execute().Files;

            return driveFiles;
        }

        private void SearchFolders(DirectoryInfo currentFolder, DriveService service, string driveFolderId)
        {
            bool folderExists = false;

            IList<Google.Apis.Drive.v3.Data.File> driveFolders = GetDriveFolders(driveFolderId, service);

            foreach (var driveFolder in driveFolders)
            {
                if (driveFolder.Name == currentFolder.Name)
                {
                    folderExists = true;
                    foreach (var folder in currentFolder.GetDirectories())
                    {
                        SearchFolders(folder, service, driveFolder.Id);
                    }
                    break;
                }
            }

            if (!folderExists)
            {
                var newDriveFolder = CreateDriveFolder(driveFolderId, service, currentFolder.Name);
                foreach(var folder in currentFolder.GetDirectories())
                {
                    SearchFolders(folder, service, newDriveFolder.Id);
                }
                
            }

            //bool folderExists = false;
            //string driveFolderId = "root"; //default

            //IList<Google.Apis.Drive.v3.Data.File> deeperDirectoryDriveFolders;
            //IList<Google.Apis.Drive.v3.Data.File> driveFiles;

            //foreach(var driveFolder in driveFolders) //drive
            //{
            //    driveFolderId = driveFolder.Id;

            //    if (driveFolder.Name == currentFolder.Name) {
            //        folderExists = true;
            //        Console.WriteLine(driveFolder.Name); //debug purpose
            //        deeperDirectoryDriveFolders = GetFolders(driveFolder.Id, service);
            //        foreach(var directory in currentFolder.GetDirectories()) //local
            //        {
            //            SearchFolders(directory, service, deeperDirectoryDriveFolders);
            //        }
            //    }
            //    //else //filecheck in current folder
            //    //{
            //    //    driveFiles = GetFiles(folder.Id, service);

            //    //    foreach (var tempFile in currentFolder.GetFiles())
            //    //    {
            //    //        if (tempFile.Name.Contains(".lnk")) continue; //shortcut skip, maybe remove... idk

            //    //        var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            //    //        {
            //    //            Name = tempFile.Name,
            //    //            Parents = new List<string>
            //    //            {
            //    //                folder.Id //parent folder in drive
            //    //            },

            //    //        };
            //    //    }
            //    //}
            //}
            //if (!folderExists)
            //{
            //    //FilesResource.CreateMediaUpload uploadRequest;
            //    var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            //    {
            //        Name = currentFolder.Name,
            //        MimeType = "application/vnd.google-apps.folder",
            //        Parents = new List<string>
            //        {

            //        }
            //    };
            //    var request = service.Files.Create(fileMetadata);
            //    var file = request.Execute();
            //    Console.WriteLine("Created folder: " + file.Name);
            //}
        }

        private Google.Apis.Drive.v3.Data.File CreateDriveFolder(string driveParentFolderId, DriveService service, string currentFolderName)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = currentFolderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new List<string>
                {
                    driveParentFolderId
                }
            };
            var request = service.Files.Create(fileMetadata);
            var file = request.Execute();
            Console.WriteLine("Created folder: " + file.Name);

            return file;
        }
    }
}