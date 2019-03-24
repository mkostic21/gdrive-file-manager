using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Upload;

namespace DriveFileManager
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(System.Environment.CurrentDirectory);
            Console.ReadKey(); //any key terminates the program
        }
    }
}
