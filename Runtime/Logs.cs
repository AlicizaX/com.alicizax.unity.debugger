using Cysharp.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace AlicizaX.Console
{
    public readonly struct AlicizaXConsoleLog
    {
        public string Text { get; }
        public LogType Type { get; }
        public bool NewLine { get; }

        public AlicizaXConsoleLog(string text, LogType type = LogType.Log, bool newLine = true)
        {
            Text = text;
            Type = type;
            NewLine = newLine;
        }
    }

    public class LogQueue
    {
        private readonly ConcurrentQueue<AlicizaXConsoleLog> _queuedLogs = new ConcurrentQueue<AlicizaXConsoleLog>();
        
        public int MaxStoredLogs { get; set; }
        public bool IsEmpty => _queuedLogs.IsEmpty;

        public LogQueue(int maxStoredLogs = -1)
        {
            MaxStoredLogs = maxStoredLogs;
        }

        public void QueueLog(AlicizaXConsoleLog log)
        {
            _queuedLogs.Enqueue(log);
            if (MaxStoredLogs > 0 && _queuedLogs.Count > MaxStoredLogs)
            {
                _queuedLogs.TryDequeue(out _);
            }
        }

        public bool TryDequeue(out AlicizaXConsoleLog log)
        {
            return _queuedLogs.TryDequeue(out log);
        }

        public void Clear()
        {
            while (TryDequeue(out AlicizaXConsoleLog _)) { }
        }
    }

    public class LogStorage
    {
        private readonly List<AlicizaXConsoleLog> _consoleLogs = new List<AlicizaXConsoleLog>(10);
        private Utf16ValueStringBuilder _logTraceBuilder = ZString.CreateStringBuilder();

        public int MaxStoredLogs { get; set; }
        public IReadOnlyList<AlicizaXConsoleLog> Logs => _consoleLogs;

        public LogStorage(int maxStoredLogs = -1)
        {
            MaxStoredLogs = maxStoredLogs;
        }

        public void AddLog(AlicizaXConsoleLog log)
        {
            _consoleLogs.Add(log);
            
            int logLength = _logTraceBuilder.Length + log.Text.Length;
            if (log.NewLine && _logTraceBuilder.Length > 0)
            {
                logLength += Environment.NewLine.Length;
            }
            
            if (MaxStoredLogs > 0)
            {
                while (_consoleLogs.Count > MaxStoredLogs)
                {
                    int junkLength = _consoleLogs[0].Text.Length;
                    if (_consoleLogs.Count > 1 && _consoleLogs[1].NewLine)
                    {
                        junkLength += Environment.NewLine.Length;
                    }
                    junkLength = Mathf.Min(junkLength, _logTraceBuilder.Length);
                    logLength -= junkLength;
                    
                    _logTraceBuilder.Remove(0, junkLength);
                    _consoleLogs.RemoveAt(0);
                }
            }

            if (log.NewLine && _logTraceBuilder.Length > 0)
            {
                _logTraceBuilder.Append(Environment.NewLine);
            }
            _logTraceBuilder.Append(log.Text);
        }

        public void RemoveLog()
        {
            if (_consoleLogs.Count > 0)
            {
                AlicizaXConsoleLog log = _consoleLogs[_consoleLogs.Count - 1];
                _consoleLogs.RemoveAt(_consoleLogs.Count - 1);

                int removeLength = log.Text.Length;
                if (log.NewLine && _consoleLogs.Count > 0)
                {
                    removeLength += Environment.NewLine.Length;
                }

                _logTraceBuilder.Remove(_logTraceBuilder.Length - removeLength, removeLength);
            }
        }

        public void Clear()
        {
            _consoleLogs.Clear();
            _logTraceBuilder.Clear();
        }

        public string GetLogString()
        {
            return _logTraceBuilder.ToString();
        }
    }
}
