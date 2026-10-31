using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Writes to the Console and keeps the lines for <see cref="SampleLogView"/>.</summary>
    public sealed class SampleLog : ISampleLog
    {
        private readonly List<string> _lines = new();

        public event Action<string> Written;

        public IReadOnlyList<string> Lines => _lines;

        public void Write(string topic, string message)
        {
            string line = $"[{topic}] {message}";
            _lines.Add(line);
            Debug.Log(line);
            Written?.Invoke(line);
        }
    }
}
