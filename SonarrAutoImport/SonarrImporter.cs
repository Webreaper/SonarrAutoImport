using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using RestSharp;

namespace SonarrAuto
{
    public class Importer
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

        [DataContract(Name = "payload")]
        public class RadarrPayLoad
        {
            public string path;
            public string name = "DownloadedMoviesScan";
            public string importMode = "Move";
            public string downloadClientId = "SonarrAutoImporter";
        }

        [DataContract(Name = "transform")]
        public class SonarrTransform
        {
            public string search;
            public string replace;
        }

        private void Log(string fmt, params object[] args)
        {
//            LogHandler.LogInstance().InfoFormat(fmt, args);
            Console.WriteLine(fmt, args);
        }

        private void LogError(string fmt, params object[] args)
        {
            //            LogHandler.LogError().InfoFormat(fmt, args);
            Console.WriteLine("Error: " + fmt, args);
        }

        private string TransformFileName(TransformSettings transforms, string path, bool verbose)
        {
            string fName = Path.GetFileName(path);
            string newfName = fName;
            foreach (var transform in transforms.transforms)
            {
                newfName = Regex.Replace(newfName, transform.search, transform.replace);
                if (verbose)
                    Log(" - Transform {0} => {1}", fName, newfName);
            }

            if (string.Compare(fName, newfName, StringComparison.OrdinalIgnoreCase) != 0)
                Log("Filename transformed: {0} => {1}", fName, newfName);
            return newfName;
        }

        private string MoveFile(string fullPathName, string newFileName)
        {
            string folder = Path.GetDirectoryName(fullPathName);
            string oldFileName = Path.GetFileName(fullPathName);

            if (string.Compare(oldFileName, newFileName, StringComparison.OrdinalIgnoreCase) != 0)
            {
                string newPath = Path.Combine(folder, newFileName);
                Log("Transforming file '{0}' to '{1}'", oldFileName, newFileName);
                try
                {
                    File.Move(fullPathName, newPath, false);
                    return newPath;
                }
                catch (Exception ex)
                {
                    LogError("Unable to rename file {0}: {1}", fullPathName, ex.Message);
                }
            }

            return fullPathName;
        }

        public void ProcessVideos( Settings settings, bool dryRun, bool verbose)
        {
            DirectoryInfo baseDir = new DirectoryInfo(settings.sonarr.downloadsFolder);

            Log("Starting video processing for: {0}", baseDir);
            if (verbose)
            {
                Log(" Base Url:   {0}", settings.sonarr.url);
                Log(" API Key:    {0}", settings.sonarr.apiKey);
                Log(" Mapping:    {0}", settings.sonarr.mappingPath);
                Log(" Dry Run:    {0}", dryRun);
            }

            var allFiles = baseDir.GetFiles("*.*", SearchOption.AllDirectories).ToList();
            var movieFiles = allFiles.Where(x => movieExtensions.Contains(x.Extension, StringComparer.OrdinalIgnoreCase))
                                    .Where( x => ! x.Name.Contains( ".partial.", StringComparison.OrdinalIgnoreCase ) )
                                    .ToList();

            if (movieFiles.Any())
            {
                Log("Processing {0} video files...", movieFiles.Count());

                foreach (var file in movieFiles)
                {
                    string videoFullPath = file.FullName;

                    string newFileName = TransformFileName(settings.transforms, videoFullPath, verbose);

                    if (!dryRun)
                    {
                        videoFullPath = MoveFile(file.FullName, newFileName);
                    }

                    string path = TranslatePath(settings.sonarr.downloadsFolder, videoFullPath, settings.sonarr.mappingPath);

                    if (!dryRun)
                    {
                        QuickImport(path, settings.sonarr, verbose);
                    }
                    else
                        Log(" => {0}", path);
                }

                Log("All processing complete.");
            }
            else
                Log("No videos found. Nothing to do!");
        }

        private string TranslatePath(string baseFolder, string fullName, string mapFolder)
        {
            string path = Path.GetFullPath(fullName);
            string localPath = path.Remove(0, baseFolder.Length);
            if (localPath.StartsWith(Path.DirectorySeparatorChar))
                localPath = localPath.Remove(0, 1);
            return Path.Combine(mapFolder, localPath);
        }

        private void QuickImport(string remotePath, SonarrSettings sonarr, bool verbose)
        {
            try
            {
                RestClient client = new RestClient(sonarr.url);
                var payload = new SonarrPayLoad { path = remotePath };

                var request = new RestRequest(Method.POST);

                request.Resource = "api/command";
                request.RequestFormat = DataFormat.Json;
                request.AddJsonBody(payload);
                request.AddHeader("User-Agent", "Sonarr Auto-Import");
                request.AddHeader("X-Api-Key", sonarr.apiKey);

                var response = client.Execute(request);
                Log(" - Executed Sonarr command for {0}", remotePath);
                if (verbose)
                    Log(response.Content);
            }
            catch (Exception e)
            {
                LogError("Exception: {0}", e);
            }
        }
    }
}
