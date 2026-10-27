using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pretzel.DevConsole
{
    /// <summary>
    /// A fixed-size ring of log entries. Thread-safe, because Unity can log from any thread.
    /// <see cref="Version"/> changes on every add or clear, so a view can poll it cheaply.
    /// </summary>
    public sealed class LogBuffer : IDisposable
    {
        private readonly object _sync = new();
        private readonly LogEntry[] _entries;
        private int _start;
        private int _count;
        private int _version;
        private bool _attached;

        public int Capacity => _entries.Length;

        public int Count
        {
            get
            {
                lock (_sync)
                {
                    return _count;
                }
            }
        }

        public int Version
        {
            get
            {
                lock (_sync)
                {
                    return _version;
                }
            }
        }

        public int ErrorCount { get; private set; }

        public int WarningCount { get; private set; }

        public LogBuffer(int capacity = 500)
        {
            _entries = new LogEntry[Mathf.Max(1, capacity)];
        }

        /// <summary>Starts capturing Unity's log. Dispose to stop.</summary>
        public LogBuffer Attach()
        {
            if (!_attached)
            {
                Application.logMessageReceivedThreaded += OnLog;
                _attached = true;
            }

            return this;
        }

        public void Dispose()
        {
            if (_attached)
            {
                Application.logMessageReceivedThreaded -= OnLog;
                _attached = false;
            }
        }

        public void Add(LogEntry entry)
        {
            lock (_sync)
            {
                if (_count == _entries.Length)
                {
                    Forget(_entries[_start]);
                    _entries[_start] = entry;
                    _start = (_start + 1) % _entries.Length;
                }
                else
                {
                    _entries[(_start + _count) % _entries.Length] = entry;
                    _count++;
                }

                if (entry.IsError)
                {
                    ErrorCount++;
                }
                else if (entry.Type == LogType.Warning)
                {
                    WarningCount++;
                }

                _version++;
            }
        }

        public void Clear()
        {
            lock (_sync)
            {
                Array.Clear(_entries, 0, _entries.Length);
                _start = 0;
                _count = 0;
                ErrorCount = 0;
                WarningCount = 0;
                _version++;
            }
        }

        /// <summary>Copies entries oldest first into <paramref name="target"/>, after clearing it.</summary>
        public void CopyTo(List<LogEntry> target, Predicate<LogEntry> filter = null)
        {
            target.Clear();
            lock (_sync)
            {
                for (int i = 0; i < _count; i++)
                {
                    LogEntry entry = _entries[(_start + i) % _entries.Length];
                    if (filter == null || filter(entry))
                    {
                        target.Add(entry);
                    }
                }
            }
        }

        private void Forget(LogEntry dropped)
        {
            if (dropped.IsError)
            {
                ErrorCount--;
            }
            else if (dropped.Type == LogType.Warning)
            {
                WarningCount--;
            }
        }

        private void OnLog(string message, string stackTrace, LogType type) => Add(new LogEntry(type, message, stackTrace, DateTime.Now));
    }
}
