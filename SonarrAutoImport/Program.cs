using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
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
            public string path;
            public string name = "DownloadedEpisodesScan";
            public string importMode = "Move";
            public string downloadClientId = "SonarrAutoImporter";
        }

        [DataContract(Name="transform")]
        public class SonarrTransform
        {
            public string search;
            public string replace;
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

            [Value(4, MetaName = "Transforms File", HelpText = "Transforms list in text file.", Required = false)]
            public string TransformsPath { get; set; }
        };

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(
                        o => {
                                ProcessVideos( o.BaseURL, o.APIKey, o.SourceDirectory, o.LocalPath, o.TransformsPath, o.DryRun, o.Verbose);
                             } );
        }

        private static SonarrTransform[] LoadTransforms(string transformsPath)
        {
            List<SonarrTransform> transforms = new List<SonarrTransform>();

            if (File.Exists(transformsPath))
            {
                Console.WriteLine("Loading transforms from {0}...", transformsPath);

                string[] lines = File.ReadAllLines(transformsPath);

                foreach (string line in lines)
                {
                    string[] parts = line.Split('|', StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length == 2)
                    {
                        transforms.Add(new SonarrTransform { search = parts[0], replace = parts[1] });
                    }
                }
            }
            else
                Console.WriteLine("No transforms file found: {0}...", transformsPath);

            if ( transforms.Any() )
            {
                Console.WriteLine("Transforms Loaded:");
                foreach (var x in transforms)
                    Console.WriteLine($"  [{x.search}] => [{x.replace}]");
            }
            return transforms.ToArray();
        }

        private static string TransformFileName( SonarrTransform[] transforms, string path, bool verbose )
        {
            string fName = Path.GetFileName(path);
            string newfName = fName;
            foreach (var transform in transforms)
            {
                newfName = Regex.Replace(newfName, transform.search, transform.replace );
                if (verbose )
                    Console.WriteLine(" - Transform {0} => {1}", fName, newfName);
            }

            if( string.Compare( fName, newfName, StringComparison.OrdinalIgnoreCase ) != 0 )
                Console.WriteLine("Filename transformed: {0} => {1}", fName, newfName);
            return newfName;
        }

        private static string MoveFile( string fullPathName, string newFileName )
        {
            string folder = Path.GetDirectoryName(fullPathName);
            string oldFileName = Path.GetFileName(fullPathName);

            if (string.Compare(oldFileName, newFileName, StringComparison.OrdinalIgnoreCase) != 0)
            {
                string newPath = Path.Combine(folder, newFileName);
                Console.WriteLine("Transforming file '{0}' to '{1}'", oldFileName, newFileName);
                try
                {
                    File.Move(fullPathName, newPath, false);
                    return newPath;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to rename file {0}: {1}", fullPathName, ex.Message);
                }
            }

            return fullPathName;
        }

        private static void ProcessVideos( string baseUrl, string APIKey, string folderToScan, string mapfolder, string transformsPath, bool dryRun, bool verbose )
        {
            DirectoryInfo baseDir = new DirectoryInfo(folderToScan);

            Console.WriteLine("Starting video processing:");
            Console.WriteLine(" Base Url:   {0}", baseUrl);
            Console.WriteLine(" API Key:    {0}", APIKey);
            Console.WriteLine(" Folder:     {0}", folderToScan);
            Console.WriteLine(" Transforms: {0}", transformsPath);
            Console.WriteLine(" Mapping:    {0}", mapfolder);
            Console.WriteLine(" Dry Run:    {0}", dryRun);

            var transforms = LoadTransforms(transformsPath);

            var allFiles = baseDir.GetFiles("*.*", SearchOption.AllDirectories).ToList();
            var movieFiles = allFiles.Where(x => movieExtensions.Contains(x.Extension, StringComparer.OrdinalIgnoreCase)).ToList();

            if (movieFiles.Any())
            {
                Console.WriteLine("Processing {0} video files...", movieFiles.Count());

                foreach (var file in movieFiles)
                {
                    string videoFullPath = file.FullName;

                    string newFileName = TransformFileName( transforms, videoFullPath, verbose );

                    if( ! dryRun )
                    {
                        videoFullPath = MoveFile(file.FullName, newFileName);
                    }

                    string path = TranslatePath(folderToScan, videoFullPath, mapfolder);

                    if (!dryRun)
                    {
                        QuickImport(path, baseUrl, APIKey, verbose);
                    }
                    else
                        Console.WriteLine(" => {0}", path);
                }

                Console.WriteLine("All processing complete.");
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
                Console.WriteLine(" - Executed Sonarr command for {0}", remotePath);
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
