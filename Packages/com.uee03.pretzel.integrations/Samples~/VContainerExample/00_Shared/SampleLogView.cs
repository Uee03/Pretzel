using System;
using UnityEngine.UIElements;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Shows the log in the scope's UIDocument. Without a PanelSettings asset the log goes to the Console only.</summary>
    public sealed class SampleLogView : IStartable, IDisposable
    {
        private readonly SampleLog _log;
        private readonly UIDocument _document;
        private ScrollView _lines;

        public SampleLogView(SampleLog log, UIDocument document)
        {
            _log = log;
            _document = document;
        }

        public void Start()
        {
            if (_document.panelSettings == null)
            {
                return;
            }

            _lines = new ScrollView();
            _lines.style.flexGrow = 1f;
            _document.rootVisualElement.Add(_lines);
            foreach (string line in _log.Lines)
            {
                Append(line);
            }

            _log.Written += Append;
        }

        public void Dispose() => _log.Written -= Append;

        private void Append(string line)
        {
            _lines.Add(new Label(line));
            _lines.scrollOffset = new UnityEngine.Vector2(0f, float.MaxValue);
        }
    }
}
