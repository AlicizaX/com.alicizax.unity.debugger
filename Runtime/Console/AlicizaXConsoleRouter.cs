using System;
using System.Threading.Tasks;

namespace AlicizaX.Console
{
    public static class AlicizaXConsoleRouter
    {
        public static IAlicizaXConsole ActiveConsole { get; private set; }

        public static void Register(IAlicizaXConsole console)
        {
            if (ActiveConsole == null || console.IsActive)
            {
                ActiveConsole = console;
            }
        }

        public static void SetActive(IAlicizaXConsole console)
        {
            ActiveConsole = console;
        }

        public static void Deregister(IAlicizaXConsole console)
        {
            if (ReferenceEquals(ActiveConsole, console))
            {
                ActiveConsole = null;
            }
        }

        private static IAlicizaXConsole RequireConsole()
        {
            if (ActiveConsole == null)
            {
                throw new InvalidOperationException("No AlicizaX Console instance is available.");
            }

            return ActiveConsole;
        }

        [Command("verbose-errors", "If errors caused by the AlicizaX Console Processor or commands should be logged in verbose mode.")]
        public static bool VerboseErrors
        {
            get => RequireConsole().VerboseErrors;
            set => RequireConsole().VerboseErrors = value;
        }

        [Command("verbose-logging", "The minimum log severity required to use verbose logging.")]
        public static LoggingThreshold VerboseLogging
        {
            get => RequireConsole().VerboseLogging;
            set => RequireConsole().VerboseLogging = value;
        }

        [Command("logging-level", "The minimum log severity required to intercept and display the log.")]
        public static LoggingThreshold LoggingLevel
        {
            get => RequireConsole().LoggingLevel;
            set => RequireConsole().LoggingLevel = value;
        }

        [Command("max-logs")]
        [CommandDescription("The maximum number of logs that may be stored in the log storage before old logs are removed.")]
        public static int MaxStoredLogs
        {
            get => RequireConsole().MaxStoredLogs;
            set => RequireConsole().MaxStoredLogs = value;
        }

        [Command("clear", "Clears the AlicizaX Console")]
        public static void ClearConsole()
        {
            RequireConsole().ClearConsole();
        }

        [Command("qc-script-extern", "Executes an external source of AlicizaXConsole script file, where each line is a separate AlicizaXConsole command.", Platform.AllPlatforms ^ Platform.WebGLPlayer)]
        public static Task InvokeExternalCommandsAsync(string filePath)
        {
            return RequireConsole().InvokeExternalCommandsAsync(filePath);
        }
    }
}
