using System;
using System.Threading.Tasks;

namespace AlicizaX.Console
{
    public interface IAlicizaXConsoleState
    {
        bool IsActive { get; }
    }

    public interface IAlicizaXConsoleSettings
    {
        bool VerboseErrors { get; set; }
        LoggingThreshold VerboseLogging { get; set; }
        LoggingThreshold LoggingLevel { get; set; }
        int MaxStoredLogs { get; set; }
    }

    public interface IAlicizaXConsoleResponse
    {
        void BeginResponse(Action<string> onSubmitResponseCallback, ResponseConfig config);
    }

    public interface IAlicizaXConsoleSerialization
    {
        string Serialize(object value);
    }

    public interface IAlicizaXConsoleOutput
    {
        void ClearConsole();
        void LogToConsole(string logText, bool newLine = true);
        void RemoveLogTrace();
    }

    public interface IAlicizaXConsoleCommandSource
    {
        Task InvokeExternalCommandsAsync(string filePath);
    }

    public interface IAlicizaXConsole :
        IAlicizaXConsoleState,
        IAlicizaXConsoleSettings,
        IAlicizaXConsoleResponse,
        IAlicizaXConsoleSerialization,
        IAlicizaXConsoleOutput,
        IAlicizaXConsoleCommandSource
    {
    }
}
