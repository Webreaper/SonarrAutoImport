using System;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using RestSharp;
using CommandLine;

namespace SonarrAutoImport
{
    class Program
    {
        private static string[] movieExtensions = { 
                ".mkv", ".avi", ".wmv", ".mov", ".amv",
                ".mp4", ".m4a", ".m4v", ".f4v", ".f4a", ".m4b", ".m4r", ".f4b",
                ".mpg", ".mp2", ".mpeg", ".mpe", ".mpv"
            };

        [DataContract(Name = "payload")]
        public class SonarrPayLoad
        {
            public string name = "DownloadedEpisodesScan";
            public string path;
            public string importMode = "Move";
            public string downloadClientId = "SonarrAutoImporter";
        }

        [DataContract(Name = "response")]
        public class SonarrResponse
        {
            [DataMember(Name = "path")]
            public int path { get; set; }
        }

        public class Options
        {
            [Option('v', "verbose", HelpText = "Run logging in Verbose Mode")]
            public bool Verbose { get; set; }

            [Option('d', "dry-run", Required = false, Default = false, HelpText = "Dry run - change nothing.")]
            public bool DryRun { get; set; }

            [Value(0, MetaName = "Source Directory", HelpText = "Input folder to be processed.", Required = true)]
            public string SourceDirectory { get; set; }

            [Value(1, MetaName = "API Key", HelpText = "Sonarr API Key", Required = true)]
            public string APIKey { get; set; }

            [Value(2, MetaName = "Base URL", HelpText = "Sonarr Instance URL", Default = "http://localhost:8989", Required = false)]
            public string BaseURL { get; set; }

            [Value(3, MetaName = "Local Path", HelpText = "Local Path for mapping", Required = false)]
            public string LocalPath { get; set; }
        };

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(
                        o => { 
                                ProcessVideos( o.BaseURL, o.APIKey, o.SourceDirectory, o.LocalPath, o.DryRun, o.Verbose);
                             } );
        }

        private static void ProcessVideos( string baseUrl, string APIKey, string folderToScan, string mapfolder, bool dryRun, bool verbose )
        {
            DirectoryInfo baseDir = new DirectoryInfo(folderToScan);

            Console.WriteLine("Starting video processing:");
            Console.WriteLine(" Base Url: {0}", baseUrl);
            Console.WriteLine(" API Key:  {0}", APIKey);
            Console.WriteLine(" Folder:   {0}", folderToScan);
            Console.WriteLine(" Mapping:  {0}", mapfolder);
            Console.WriteLine(" Dry Run:  {0}", dryRun);

            var allFiles = baseDir.GetFiles("*.*", SearchOption.AllDirectories).ToList();
            var movieFiles = allFiles.Where(x => movieExtensions.Contains(x.Extension, StringComparer.OrdinalIgnoreCase)).ToList();

            if (movieFiles.Any())
            {
                foreach (var file in movieFiles)
                {
                    string path = TranslatePath(folderToScan, file.FullName, mapfolder);

                    if (!dryRun)
                        QuickImport(path, baseUrl, APIKey, verbose);
                    else
                        Console.WriteLine(" => {0}", path);
                }
            }
            else
                Console.WriteLine("No videos found. Nothing to do!");
        }

        private static string TranslatePath(string baseFolder, string fullName, string mapFolder)
        {
            string path = Path.GetFullPath(fullName);
            string localPath = path.Remove(0, baseFolder.Length);
            if (localPath.StartsWith(Path.DirectorySeparatorChar))
                localPath = localPath.Remove(0, 1);
            return Path.Combine(mapFolder, localPath);
        }

        private static void QuickImport( string remotePath, string baseUrl, string APIKey, bool verbose)
        {
            try
            {
                RestClient client = new RestClient(baseUrl);
                var payload = new SonarrPayLoad { path = remotePath };

                var request = new RestRequest(Method.POST);

                request.Resource = "api/command";
                request.RequestFormat = DataFormat.Json;
                request.AddJsonBody(payload);
                request.AddHeader("User-Agent", "Sonarr Auto-Import");
                request.AddHeader("X-Api-Key", APIKey);

                var response = client.Execute(request);
                Console.WriteLine("Executed Sonarr command for {0}", remotePath);
                if( verbose )
                    Console.WriteLine(response.Content);
            }
            catch( Exception e )
            {
                Console.WriteLine("Exception: {0}", e);
            }
        }
    }
}
