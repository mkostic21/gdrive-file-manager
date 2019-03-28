using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Google.Apis.Drive;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Google.Apis.Services;

namespace DriveFileManager
{
    class Program
    {
        static readonly string[] Scopes = { DriveService.Scope.Drive, DriveService.Scope.DriveFile };
       
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
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private async Task Run()
        {

            UserCredential credential;
            
            using (var stream = new FileStream(Environment.CurrentDirectory + @"\client_id.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets, Scopes, "user", CancellationToken.None);
            }

            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "GDrive File Manager",
            });

            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = "test.jpg"
            };

            FilesResource.CreateMediaUpload request;
            using (var stream = new FileStream(getWorkspaceLocation() + @"\test.jpg",
                                    FileMode.Open))
            {
                request = service.Files.Create(fileMetadata, stream, "image/jpeg");
                request.Fields = "id";
                request.Upload();
            }
            var file = request.ResponseBody;
            Console.WriteLine("File ID: " + file.Id);
        }

        private string getWorkspaceLocation()
        {
            string temp = Environment.CurrentDirectory; 
            return temp.Remove(temp.Length-2); //duljina imena foldera - 2
        }
    }
}
