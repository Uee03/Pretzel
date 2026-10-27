using System;
using UnityEngine;

namespace Pretzel.DevConsole
{
    public readonly struct LogEntry
    {
        public LogType Type { get; }
        public string Message { get; }
        public string StackTrace { get; }
        public DateTime Time { get; }

        public LogEntry(LogType type, string message, string stackTrace, DateTime time)
        {
            Type = type;
            Message = message ?? string.Empty;
            StackTrace = stackTrace ?? string.Empty;
            Time = time;
        }

        public bool IsError => Type == LogType.Error || Type == LogType.Exception || Type == LogType.Assert;
    }
}
