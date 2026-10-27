using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
#if PRETZEL_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Pretzel.DevConsole
{
    /// <summary>
    /// An in-game log overlay and command line, built in code on a <see cref="UIDocument"/>. Give the document a PanelSettings
    /// with a high sort order. Toggle it with the toggle key or <see cref="Toggle"/>. Register commands on <see cref="Commands"/>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Pretzel/Dev Console")]
    public sealed class DeveloperConsole : MonoBehaviour
    {
        private const string Block = "pretzel-dev-console";

        [SerializeField] private UIDocument _document;
        [SerializeField, Min(10)] private int _capacity = 500;

        [Tooltip("Destroys the console in release builds. Development builds and the Editor keep it.")]
        [SerializeField] private bool _developmentBuildsOnly = true;

        [SerializeField] private bool _startOpen;
        [SerializeField, Range(0.2f, 1f)] private float _height = 0.45f;

#if PRETZEL_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
        [SerializeField] private Key _toggleKey = Key.Backquote;
#elif ENABLE_LEGACY_INPUT_MANAGER
        [SerializeField] private KeyCode _toggleKey = KeyCode.BackQuote;
#endif

        private readonly CommandRegistry _commands = new();
        private readonly List<LogEntry> _visible = new();
        private readonly List<string> _history = new();
        private LogBuffer _logs;
        private int _shownVersion = -1;
        private int _historyIndex;
        private VisualElement _panel;
        private ListView _list;
        private TextField _input;
        private Toggle _showLogs;
        private Toggle _showWarnings;
        private Toggle _showErrors;
        private Label _counts;
        private Predicate<LogEntry> _filter;

        public CommandRegistry Commands => _commands;

        public LogBuffer Logs => _logs;

        public bool IsOpen { get; private set; }

        private void Reset()
        {
            _document = GetComponent<UIDocument>();
        }

        private void Awake()
        {
            if (_developmentBuildsOnly && !Debug.isDebugBuild)
            {
                Destroy(gameObject);
                return;
            }

            if (!_document)
            {
                _document = GetComponent<UIDocument>();
            }

            _filter = Matches;
            _logs = new LogBuffer(_capacity).Attach();
            _commands.Register("clear", "Clears the console.", _ => _logs.Clear());
            IsOpen = _startOpen;
        }

        private void OnEnable() => TryBuild();

        private void Start() => TryBuild();

        private void OnDestroy() => _logs?.Dispose();

        private void Update()
        {
            if (_logs == null)
            {
                return;
            }

            if (ToggleKeyPressed())
            {
                Toggle();
            }

            TryBuild();
            if (IsOpen && _logs.Version != _shownVersion)
            {
                Refresh();
            }
        }

        public void Open() => SetOpen(true);

        public void Close() => SetOpen(false);

        public void Toggle() => SetOpen(!IsOpen);

        /// <summary>Runs a command as if it was typed, and shows its output.</summary>
        public void Submit(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            AddLine(LogType.Log, "> " + line);
            if (_history.Count == 0 || _history[_history.Count - 1] != line)
            {
                _history.Add(line);
            }

            _historyIndex = _history.Count;
            string output = _commands.Execute(line);
            if (string.IsNullOrEmpty(output))
            {
                return;
            }

            foreach (string outputLine in output.Split('\n'))
            {
                AddLine(LogType.Log, outputLine.TrimEnd('\r'));
            }
        }

        private void SetOpen(bool open)
        {
            IsOpen = open;
            if (_panel == null)
            {
                return;
            }

            _panel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (open)
            {
                Refresh();
                _input.schedule.Execute(() => _input.Focus());
            }
        }

        private void AddLine(LogType type, string text) => _logs.Add(new LogEntry(type, text, null, DateTime.Now));

        private bool ToggleKeyPressed()
        {
#if PRETZEL_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && _toggleKey != Key.None && keyboard[_toggleKey].wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return _toggleKey != KeyCode.None && Input.GetKeyDown(_toggleKey);
#else
            return false;
#endif
        }

        // UIDocument rebuilds its tree when it is re-enabled, so rebuild the console whenever the root changes.
        private void TryBuild()
        {
            VisualElement root = _document ? _document.rootVisualElement : null;
            if (root == null || _logs == null || (_panel != null && _panel.parent == root))
            {
                return;
            }

            _panel = BuildPanel();
            root.Add(_panel);
            _shownVersion = -1;
            SetOpen(IsOpen);
        }

        private VisualElement BuildPanel()
        {
            VisualElement panel = new() { name = Block };
            panel.AddToClassList(Block);
            panel.style.position = Position.Absolute;
            panel.style.left = 0;
            panel.style.right = 0;
            panel.style.top = 0;
            panel.style.height = Length.Percent(_height * 100f);
            panel.style.backgroundColor = new Color(0.07f, 0.07f, 0.09f, 0.94f);
            panel.style.paddingLeft = panel.style.paddingRight = panel.style.paddingTop = panel.style.paddingBottom = 4;

            VisualElement toolbar = new();
            toolbar.AddToClassList(Block + "__toolbar");
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.alignItems = Align.Center;
            _showLogs = CreateFilter("Logs", toolbar);
            _showWarnings = CreateFilter("Warnings", toolbar);
            _showErrors = CreateFilter("Errors", toolbar);
            Button clear = new(() => _logs.Clear()) { text = "Clear" };
            toolbar.Add(clear);
            _counts = new Label();
            _counts.style.color = new Color(0.75f, 0.75f, 0.75f);
            _counts.style.marginLeft = 8;
            toolbar.Add(_counts);
            panel.Add(toolbar);

            _list = new ListView(_visible, 18f, MakeRow, BindRow)
            {
                selectionType = SelectionType.None,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight
            };
            _list.AddToClassList(Block + "__list");
            _list.style.flexGrow = 1;
            panel.Add(_list);

            _input = new TextField();
            _input.AddToClassList(Block + "__input");
            _input.RegisterCallback<KeyDownEvent>(OnInputKey, TrickleDown.TrickleDown);
            panel.Add(_input);
            return panel;
        }

        private Toggle CreateFilter(string label, VisualElement parent)
        {
            Toggle toggle = new(label) { value = true };
            toggle.AddToClassList(Block + "__filter");
            toggle.style.marginRight = 8;
            toggle.RegisterValueChangedCallback(_ => Refresh());
            parent.Add(toggle);
            return toggle;
        }

        private static VisualElement MakeRow()
        {
            Label label = new() { enableRichText = false };
            label.AddToClassList(Block + "__row");
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.overflow = Overflow.Hidden;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            return label;
        }

        private void BindRow(VisualElement element, int index)
        {
            LogEntry entry = _visible[index];
            Label label = (Label)element;
            int newline = entry.Message.IndexOf('\n');
            string message = newline < 0 ? entry.Message : entry.Message.Substring(0, newline);
            label.text = entry.Time.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "  " + message;
            label.style.color = entry.IsError ? new Color(1f, 0.45f, 0.4f)
                : entry.Type == LogType.Warning ? new Color(1f, 0.85f, 0.35f)
                : new Color(0.9f, 0.9f, 0.9f);
        }

        private void Refresh()
        {
            if (_list == null)
            {
                return;
            }

            _shownVersion = _logs.Version;
            _logs.CopyTo(_visible, _filter);
            _list.RefreshItems();
            if (_visible.Count > 0)
            {
                _list.ScrollToItem(_visible.Count - 1);
            }

            _counts.text = $"{_logs.Count} entries, {_logs.WarningCount} warnings, {_logs.ErrorCount} errors";
        }

        private bool Matches(LogEntry entry)
        {
            if (entry.IsError)
            {
                return _showErrors == null || _showErrors.value;
            }

            if (entry.Type == LogType.Warning)
            {
                return _showWarnings == null || _showWarnings.value;
            }

            return _showLogs == null || _showLogs.value;
        }

        private void OnInputKey(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    string line = _input.value;
                    _input.value = string.Empty;
                    Submit(line);
                    _input.schedule.Execute(() => _input.Focus());
                    evt.StopPropagation();
                    break;
                case KeyCode.UpArrow when _history.Count > 0:
                    _historyIndex = Mathf.Max(0, _historyIndex - 1);
                    _input.value = _history[_historyIndex];
                    evt.StopPropagation();
                    break;
                case KeyCode.DownArrow when _history.Count > 0:
                    _historyIndex = Mathf.Min(_history.Count, _historyIndex + 1);
                    _input.value = _historyIndex < _history.Count ? _history[_historyIndex] : string.Empty;
                    evt.StopPropagation();
                    break;
            }
        }
    }
}
