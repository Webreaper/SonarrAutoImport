using System;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace SonarrAuto.Logging
{
    public static class LogHandler
    {
        public static string logLocation = string.Empty;

        public static void LogSetup(string fileName)
        {
            var wrapperLogger = LogManager.GetLogger(typeof(LogHandler));
            var logger = (Logger)wrapperLogger.Logger;
            logger.Hierarchy.Root.Level = Level.All;

            PatternLayout logLayout = new PatternLayout();
            logLayout.ConversionPattern = "%date [%thread] %-5level %logger - %message%newline";
            logLayout.ActivateOptions();

            PatternLayout consoleLayout = new PatternLayout();
            consoleLayout.ConversionPattern = ">> %message%newline";
            consoleLayout.ActivateOptions();

            RollingFileAppender roller = new RollingFileAppender();
            roller.AppendToFile = true;
            roller.File = fileName;
            roller.Layout = logLayout;
            roller.MaxSizeRollBackups = 3;
            roller.MaxFileSize = 10000000;
            roller.RollingStyle = RollingFileAppender.RollingMode.Size;
            roller.StaticLogFileName = true;
            roller.ActivateOptions();

            ConsoleAppender console = new ConsoleAppender();
            console.Layout = consoleLayout;
            console.ActivateOptions();

            logger.Hierarchy.Root.AddAppender(console);
            logger.Hierarchy.Root.AddAppender(roller);
            logger.Hierarchy.Root.Level = Level.Info;
            logger.Hierarchy.Configured = true;

            m_logInstance = LogManager.GetLogger(typeof( Importer ));
        }

        private static ILog m_logInstance;

        public static ILog LogInstance() 
        {
            if(m_logInstance == null )
            {
                throw new SystemException("Logging was not initialised.");
            }

            return m_logInstance;
        } 
    }
}
