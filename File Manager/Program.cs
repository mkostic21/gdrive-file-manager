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
            MimeTypeLookup mimeTypeLookup = new MimeTypeLookup();
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


            DirectoryInfo directory = new DirectoryInfo(getWorkspaceLocation());

            Console.WriteLine("Checking Files..." + Environment.NewLine);


            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 100; 
            listRequest.Q = "'root' in parents and trashed=false and mimeType='application/vnd.google-apps.folder'"; //samo main drive folder
            listRequest.Spaces = "drive";
            listRequest.Fields = "files(id, name)";
            IList<Google.Apis.Drive.v3.Data.File> driveFolders = listRequest.Execute().Files;

            //print(driveFolders, service);

            //foreach (var d in driveFolders)
            //{
            //    Console.WriteLine(d.Name + "\t" + d.Id);
            //}

            foreach (var tempFolder in directory.GetDirectories())
            {
                searchFolders(tempFolder, service, driveFolders); //recursion
            }

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

            Console.WriteLine(Environment.NewLine + "All files are up-to-date." + Environment.NewLine);
        }

        private string getWorkspaceLocation()
        {
            const int EXE_FOLDER_LENGTH = 19; //GDrive File Manager - 19 znakova
            string temp = Directory.GetCurrentDirectory();
            return temp.Remove(temp.Length-EXE_FOLDER_LENGTH);
        }

        private IList<Google.Apis.Drive.v3.Data.File> getCurrentFolders(string folderId, DriveService service)
        {
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 10;
            listRequest.Q = "'"+folderId+"'"+" in parents and trashed=false and mimeType='application/vnd.google-apps.folder'";
            listRequest.Spaces = "drive";
            listRequest.Fields = "files(id, name)";
            IList<Google.Apis.Drive.v3.Data.File> driveFolders = listRequest.Execute().Files;

            return driveFolders;
        }

        private void searchFolders(DirectoryInfo currentDirectory, DriveService service, IList<Google.Apis.Drive.v3.Data.File> driveFolders)
        {
            IList<Google.Apis.Drive.v3.Data.File> newDriveFolders;

            foreach(var folder in driveFolders) //drive
            {
                if (folder.Name == currentDirectory.Name) {
                    Console.WriteLine(folder.Name);
                    newDriveFolders = getCurrentFolders(folder.Id, service);
                    foreach(var tempFolder in currentDirectory.GetDirectories()) //local
                    {
                        searchFolders(tempFolder, service, newDriveFolders);
                    }
                }
            }
        }

        //private void print(IList<Google.Apis.Drive.v3.Data.File> driveFolders, DriveService service)
        //{
        //    IList<Google.Apis.Drive.v3.Data.File> newDriveFolders;
        //    foreach (var folder in driveFolders)
        //    {
        //        Console.WriteLine(folder.Name);
        //        newDriveFolders = getCurrentFolders(folder.Id, service);
        //        print(newDriveFolders, service);
        //    }
        //}
    }
}