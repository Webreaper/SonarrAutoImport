
using System;
using System.IO;
using CommandLine;
using SonarrAuto.Logging;

namespace SonarrAuto
{
    class Program
    {

        public class Options
        {
            [Option('v', "verbose", HelpText = "Run logging in Verbose Mode")]
            public bool Verbose { get; set; }

            [Option('d', "dry-run", Required = false, Default = false, HelpText = "Dry run - change nothing.")]
            public bool DryRun { get; set; }

            [Value(0, MetaName = "Settings Path", HelpText = "Path to settings JSON file (default = app dir)", Required = false)]
            public string SettingsPath { get; set; } = "Settings.json";
        };

        static void Main(string[] args)
        {
            LogHandler.InitLogs();

            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed( o => { RunProcess(o); });
        }

        private static void RunProcess(Options o)
        {
            var settings = Settings.Read(o.SettingsPath);

            if (settings != null)
            {
                var importer = new Importer();

                if (settings.sonarr != null)
                {
                    LogHandler.Log("Processing videos for Sonarr...");
                    importer.ProcessService(settings.sonarr, o.DryRun, o.Verbose, "DownloadedEpisodesScan");
                }
                if (settings.radarr != null)
                {
                    LogHandler.Log("Processing videos for Radarr...");
                    importer.ProcessService(settings.radarr, o.DryRun, o.Verbose, "DownloadedMoviesScan");
                }
            }
            else
                LogHandler.LogError($"Settings not found: {o.SettingsPath}");
        }
    }
}
