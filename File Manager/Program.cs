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

            SearchFiles(directory, service, "root");
            foreach (var currentFolder in directory.GetDirectories())
            {
                if (currentFolder.Name == "GDrive File Manager") continue;
                SearchFolders(currentFolder, service, "root"); //recursion
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
            listRequest.PageSize = 50;
            listRequest.Q = "'" + folderId + "'" + " in parents and trashed=false and mimeType!='application/vnd.google-apps.folder'";
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
                    SearchFiles(currentFolder, service, driveFolder.Id);
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

                SearchFiles(currentFolder, service, newDriveFolder.Id);
                foreach (var folder in currentFolder.GetDirectories())
                {
                    SearchFolders(folder, service, newDriveFolder.Id);
                }
            }
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

        private void CreateDriveFile(string driveParentFolderId, DriveService service, FileInfo currentFile)
        {
            MimeTypeLookup mimeTypeLookup = new MimeTypeLookup();
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = currentFile.Name,
                Parents = new List<string>
                {
                    driveParentFolderId
                }
            };
            FilesResource.CreateMediaUpload request;
            using (var stream = new FileStream(currentFile.FullName, FileMode.Open))
            {
                request = service.Files.Create(fileMetadata, stream, mimeTypeLookup.GetMimeType(currentFile.Name));
                request.Upload();
            }
            var file = request.ResponseBody;
            Console.WriteLine(file.Name + "\t Uploaded successfully.");
        }

        private void SearchFiles(DirectoryInfo currentFolder, DriveService service, string driveFolderId)
        {
            IList<Google.Apis.Drive.v3.Data.File> driveFiles = GetDriveFiles(driveFolderId, service);

            foreach (var currentFile in currentFolder.GetFiles())
            {
                if (currentFile.Name.Contains(".lnk")) continue; //shortcut
                
                bool fileExists = false;

                DateTime fileModifiedTime = currentFile.LastWriteTime;
                
                if (driveFiles != null && driveFiles.Count > 0)
                {
                    foreach (var driveFile in driveFiles)
                    {

                        if (driveFile.Name == currentFile.Name)
                        {
                            fileExists = true;

                            if (driveFile.ModifiedTime < fileModifiedTime)
                            {
                                FilesResource.DeleteRequest deleteRequest;
                                deleteRequest = service.Files.Delete(driveFile.Id);
                                var deleteFile = deleteRequest.Execute();

                                fileExists = false; //deleted
                            }
                        }
                    }
                }
                if (!fileExists)
                {
                    CreateDriveFile(driveFolderId, service, currentFile);
                }

            }
        }
    }
}