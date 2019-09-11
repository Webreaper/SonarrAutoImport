
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
            // Todo: When Log4Net supports .Net core 3.0
            // LogHandler.LogSetup("SonarrAutoImport.log");

            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(
                        o => {
                                var importer = new Importer();
                                importer.ProcessVideos( o.BaseURL, o.APIKey, o.SourceDirectory, o.LocalPath, o.TransformsPath, o.DryRun, o.Verbose);
                             } );
        }


    }
}
