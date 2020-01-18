
using System.IO;
using CommandLine;

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
            public string SettingsPath { get; set; }
        };

        static void Main(string[] args)
        {

            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(
                        o => {

                            var settings = Settings.Read(o.SettingsPath);

                            var importer = new Importer();
                                importer.ProcessVideos( settings, o.DryRun, o.Verbose);
                             } );
        }


    }
}
