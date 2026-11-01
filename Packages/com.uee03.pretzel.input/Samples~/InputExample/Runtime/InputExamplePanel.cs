using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Pretzel.Inputs.Samples
{
    /// <summary>Shows the current context and device, and has buttons for a rebind, a reset and a short input block (what a scene load does).</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class InputExamplePanel : MonoBehaviour
    {
        private const string BlockReason = "example-block";
        private const int JumpBindingIndex = 0;

        [SerializeField] private InputExample _input;

        private Label _status;
        private Button _rebind;
        private RebindSession _session;
        private IVisualElementScheduledItem _refresh;

        private void OnEnable()
        {
            if (_input == null)
            {
                Debug.LogError("InputExamplePanel needs the InputExample.", this);
                return;
            }

            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            VisualElement bar = new();
            bar.AddToClassList("ix-bar");
            root.Add(bar);

            _rebind = AddButton(bar, string.Empty, StartRebind);
            AddButton(bar, "Reset bindings", ResetBindings);
            AddButton(bar, "Block input 2 s", BlockForTwoSeconds);

            _status = new Label();
            _status.AddToClassList("ix-status");
            root.Add(_status);

            Label help = new("Move: A D or the left stick.  Jump: Space or A, buffered for 0.15 s.  Pause: Esc or Start.");
            help.AddToClassList("ix-help");
            root.Add(help);

            ShowBinding();
            _refresh = _status.schedule.Execute(ShowStatus).Every(100);
        }

        private void OnDisable()
        {
            _refresh?.Pause();
            _session?.Dispose();
            _session = null;
        }

        private static Button AddButton(VisualElement parent, string text, System.Action onClick)
        {
            Button button = new(onClick) { text = text };
            button.AddToClassList("ix-button");
            parent.Add(button);
            return button;
        }

        private void StartRebind()
        {
            if (_session != null && _session.IsRunning)
            {
                return;
            }

            _rebind.text = "Press a key... (Esc cancels)";
            _session = RebindSession.Start(_input.Actions.FindAction("Gameplay/Jump", true), JumpBindingIndex, OnRebound);
        }

        private void OnRebound(RebindResult result)
        {
            if (result == RebindResult.Completed)
            {
                BindingPersistence.Save(_input.Actions, _input.Store);
            }

            ShowBinding();
        }

        private void ResetBindings()
        {
            BindingPersistence.Reset(_input.Actions, _input.Store);
            ShowBinding();
        }

        // Blocks input the way a scene load does, then releases it.
        private void BlockForTwoSeconds()
        {
            _input.Router.Blockers.Block(BlockReason);
            _status.schedule.Execute(() => _input.Router.Blockers.Unblock(BlockReason)).StartingIn(2000);
        }

        private void ShowBinding()
        {
            string binding = _input.Actions.FindAction("Gameplay/Jump", true).GetBindingDisplayString(JumpBindingIndex);
            _rebind.text = $"Rebind Jump ({binding})";
        }

        private void ShowStatus()
        {
            string context = _input.Router.ActiveContext ?? (_input.Router.Blockers.IsBlocked ? "none (input blocked)" : "none");
            _status.text = $"Context: {context}      Device: {_input.Devices.Kind}";
        }
    }
}
