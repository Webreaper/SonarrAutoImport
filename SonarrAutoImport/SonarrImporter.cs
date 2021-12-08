using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using RestSharp;
using System.Threading;

namespace SonarrAuto
{
    public class Importer
    {
        private static string[] movieExtensions = {
                ".mkv", ".avi", ".wmv", ".mov", ".amv",
                ".mp4", ".m4a", ".m4v", ".f4v", ".f4a", ".m4b", ".m4r", ".f4b",
                ".mpg", ".mp2", ".mpeg", ".mpe", ".mpv"
            };

        private static string[] musicExtensions = {
                ".mp3", ".flac", ".opus", ".m4a", ".wav", ".wma"
            };

        private static string[] discNames =
        {
            "cd1", "cd2", "cd3", "cd4", "cd5",
            "disk1", "disk2", "disk3", "disk4", "disk5",
            "disc1", "disc2", "disc3", "disc4", "disc5"
        };

        [DataContract(Name = "payload")]
        public class PayLoad
        {
            public string path;
            public string name; // API command name
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
            Logging.LogHandler.Log(fmt, args);
        }

        private void LogError(string fmt, params object[] args)
        {
            Logging.LogHandler.LogError(fmt, args);
        }

        private string TransformFileName(List<Transform> transforms, string path, bool verbose)
        {
            string fName = Path.GetFileName(path);
            string newfName = fName;

            if (transforms != null && transforms.Any())
            {
                Log($" Running {transforms.Count()} transforms on {path}...");

                foreach (var transform in transforms)
                {
                    newfName = Regex.Replace(newfName, transform.search, transform.replace, RegexOptions.IgnoreCase);
                    Logging.LogHandler.LogVerbose(" - Transform {0} => {1}", fName, newfName);
                }

                if (string.Compare(fName, newfName, StringComparison.OrdinalIgnoreCase) != 0)
                    Log("Filename transformed: {0} => {1}", fName, newfName);
            }
            else
                Log("No transforms configured.");
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

        /// <summary>
        /// Skip .partial files, and also any file where the last write time is any time
        /// the last 5 mins.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private bool IsPartialDownload( FileInfo file )
        {
            if (file.Name.Contains(".partial.", StringComparison.OrdinalIgnoreCase))
                return true;

            var age = DateTime.UtcNow - file.LastWriteTimeUtc;

            if (Math.Abs(age.TotalMinutes) < 5)
                return true;

            return false;
        }

        public void ProcessService( ServiceSettings settings, bool dryRun, bool verbose, string apiCommand)
        {
            DirectoryInfo baseDir = new DirectoryInfo(settings.downloadsFolder);

            if (settings.importMode != "Copy" && settings.importMode != "Move")
            {
                Log($"Invalid importMode '{settings.importMode}' in settings. Defaulting to 'Move'");
                settings.importMode = "Move";
            }

            Log("Starting video processing for: {0}", baseDir);
            if (verbose)
            {
                Log(" Base Url:   {0}", settings.url);
                Log(" API Key:    {0}", settings.apiKey);
                Log(" Mapping:    {0}", settings.mappingPath);
                Log(" Timeout:    {0}", settings.timeoutSecs);
                Log(" CopyMode:   {0}", settings.importMode);
                Log(" Dry Run:    {0}", dryRun);
            }

            if (baseDir.Exists)
            {
                var allFiles = baseDir.GetFiles("*.*", SearchOption.AllDirectories).ToList();
                var movieFiles = allFiles.Where(x => movieExtensions.Contains(x.Extension, StringComparer.OrdinalIgnoreCase))
                                        .Where(x => !IsPartialDownload( x ) )
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

                        string path = TranslatePath(settings.downloadsFolder, videoFullPath, settings.mappingPath);

                        if (!dryRun)
                        {
                            QuickImport(path, settings, verbose, apiCommand);
                        }
                        else
                            Log(" => {0}", path);

                        if (settings.timeoutSecs != 0)
                        {
                            Log( $"Sleeping for {settings.timeoutSecs} seconds...");
                            Thread.Sleep(settings.timeoutSecs * 1000);
                        }
                    }

                    Log("All processing complete.");
                }
                else
                    Log("No videos found. Nothing to do!");

                if (settings.trimFolders)
                {
                    Log($"Trimming empty folders in {baseDir.FullName}");
                    TrimEmptyFolders(baseDir, movieExtensions);
                }
            }
            else
                Log($"Folder {baseDir} was not found. Check configuration.");
        }

        private string GetAlbumFolder( DirectoryInfo dir )
        {
            string albumFolder = dir.FullName;

            if( discNames.Any( x => dir.Name.StartsWith( x, StringComparison.OrdinalIgnoreCase ) ) )
            {
                albumFolder = dir.Parent.FullName;
            }

            return albumFolder;
        }


        public void ProcessLidarr(ServiceSettings settings, bool dryRun, bool verbose, string apiCommand)
        {
            DirectoryInfo baseDir = new DirectoryInfo(settings.downloadsFolder);

            if (settings.importMode != "Copy" && settings.importMode != "Move")
            {
                Log($"Invalid importMode '{settings.importMode}' in settings. Defaulting to 'Move'");
                settings.importMode = "Move";
            }

            Log("Starting audio processing for: {0}", baseDir);
            if (verbose)
            {
                Log(" Base Url:   {0}", settings.url);
                Log(" API Key:    {0}", settings.apiKey);
                Log(" Mapping:    {0}", settings.mappingPath);
                Log(" Timeout:    {0}", settings.timeoutSecs);
                Log(" CopyMode:   {0}", settings.importMode);
                Log(" Dry Run:    {0}", dryRun);
            }

            if (baseDir.Exists)
            {
                var allFiles = baseDir.GetFiles("*.*", SearchOption.AllDirectories).ToList();

                var albumFolders = allFiles.Where(x => musicExtensions.Contains(x.Extension, StringComparer.OrdinalIgnoreCase))
                                        .Where(x => !IsPartialDownload(x))
                                        .Select(x => GetAlbumFolder(x.Directory))
                                        .Distinct()
                                        .ToList();

                if (albumFolders.Any())
                {
                    Log("Processing {0} album folders...", albumFolders.Count());

                    foreach (var folder in albumFolders)
                    {
                        string path = TranslatePath(settings.downloadsFolder, folder, settings.mappingPath);

                        if (!dryRun)
                        {
                            QuickImport(path, settings, verbose, apiCommand, true);
                        }
                        else
                            Log(" => {0}", path);

                        if (settings.timeoutSecs != 0)
                        {
                            Log($"Sleeping for {settings.timeoutSecs} seconds...");
                            Thread.Sleep(settings.timeoutSecs * 1000);
                        }
                    }

                    Log("All processing complete.");
                }
                else
                    Log("No albums found. Nothing to do!");

                if (settings.trimFolders)
                {
                    Log($"Trimming empty folders in {baseDir.FullName}");
                    TrimEmptyFolders(baseDir, musicExtensions);
                }
            }
            else
                Log($"Folder {baseDir} was not found. Check configuration.");
        }

        private void TrimEmptyFolders(DirectoryInfo baseDir, string[] extensions)
        {
            if ((baseDir.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                return;

            if (baseDir.Name.StartsWith(".") || baseDir.Name.StartsWith("@"))
                return;

            var allFolders = baseDir.GetDirectories("*.*", SearchOption.AllDirectories);

            foreach (var folder in allFolders)
            {
                try
                {
                    TrimEmptyFolders(folder, extensions);

                    var nonMovieFiles = folder.GetFiles()
                                           .Where(x => !extensions.Contains(x.Extension, StringComparer.OrdinalIgnoreCase)
                                                       && !x.Name.StartsWith( ".")
                                                       && (x.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                                           .ToList();

                    nonMovieFiles.ForEach(x =>
                    {
                        Log($"  Deleting non-video file: {x.FullName}");
                        x.Delete();
                    });

                    if (!folder.GetFiles().Any() && !folder.GetDirectories().Any() )
                    {
                        Log($"Removing empty folder: {folder.FullName}");
                        folder.Delete();
                    }
                }
                catch (Exception ex)
                {
                    Log($"Unexpected exception during folder trim: {ex.Message}");
                }
            }
        }

        private string TranslatePath(string baseFolder, string fullName, string mapFolder)
        {
            string path = Path.GetFullPath(fullName);
            string localPath = path.Remove(0, baseFolder.Length);
            if (localPath.StartsWith(Path.DirectorySeparatorChar))
                localPath = localPath.Remove(0, 1);
            return Path.Combine(mapFolder, localPath);
        }

        private void QuickImport(string remotePath, ServiceSettings service, bool verbose, string apiCommand, bool isLidarr = false)
        {
            try
            {
                RestClient client = new RestClient(service.url);
                var payload = new PayLoad { path = remotePath, name = apiCommand, importMode = service.importMode };

                var request = new RestRequest(Method.POST);

                request.Resource = isLidarr ? "api/v1/command" : "api/command";
                request.RequestFormat = DataFormat.Json;
                request.AddJsonBody(payload);
                request.AddHeader("User-Agent", "Sonarr Auto-Import");
                request.AddHeader("X-Api-Key", service.apiKey);

                var response = client.Execute(request);
                Log(" - Executed Service command for {0}", remotePath);
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
