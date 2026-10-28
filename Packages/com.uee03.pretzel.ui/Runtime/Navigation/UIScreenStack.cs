using System;
using System.Collections.Generic;
using Pretzel.Core;
using Pretzel.Tween;
using UnityEngine.UIElements;

namespace Pretzel.UI
{
    /// <summary>
    /// Navigation between screens that are <see cref="VisualElement"/>s, such as main menu, options and credits.
    /// A pushed screen fades in and takes focus. A modal push keeps the screen below it visible.
    /// Back buttons and the cancel action usually call <see cref="Pop"/>.
    /// </summary>
    public sealed class UIScreenStack
    {
        private readonly List<Entry> _stack = new();

        public float FadeDuration { get; set; }

        public EaseType Ease { get; set; }

        public int Count => _stack.Count;

        public VisualElement Current => _stack.Count > 0 ? _stack[_stack.Count - 1].Screen : null;

        /// <summary>Raised with the new top screen, or null when the stack is empty.</summary>
        public event Action<VisualElement> Changed;

        public UIScreenStack(float fadeDuration = 0.2f, EaseType ease = EaseType.OutQuad)
        {
            FadeDuration = fadeDuration;
            Ease = ease;
        }

        /// <param name="modal">Keeps the screen below visible, for dialogs and pause overlays.</param>
        public void Push(VisualElement screen, bool modal = false, bool animate = true)
        {
            Guard.NotNull(screen, nameof(screen));
            if (Contains(screen))
            {
                throw new InvalidOperationException($"Screen '{screen.name}' is already on the stack.");
            }

            VisualElement below = Current;
            if (below != null && !modal)
            {
                Hide(below, animate);
            }

            _stack.Add(new Entry(screen, modal));
            Show(screen, animate);
            Changed?.Invoke(screen);
        }

        /// <summary>Hides the top screen and shows the one below. Does nothing with fewer than <paramref name="keep"/> + 1 screens.</summary>
        /// <param name="keep">Screens that must stay on the stack, so Back on the main menu does nothing.</param>
        public bool Pop(bool animate = true, int keep = 1)
        {
            if (_stack.Count <= keep)
            {
                return false;
            }

            Entry top = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);
            Hide(top.Screen, animate);
            VisualElement below = Current;
            if (below != null)
            {
                if (!top.Modal)
                {
                    Show(below, animate);
                }
                else
                {
                    below.FocusFirst();
                }
            }

            Changed?.Invoke(below);
            return true;
        }

        /// <summary>Replaces the top screen.</summary>
        public void Replace(VisualElement screen, bool animate = true)
        {
            Guard.NotNull(screen, nameof(screen));
            if (_stack.Count > 0)
            {
                Entry top = _stack[_stack.Count - 1];
                _stack.RemoveAt(_stack.Count - 1);
                Hide(top.Screen, animate);
            }

            Push(screen, false, animate);
        }

        /// <summary>Pops back to the first screen.</summary>
        public void PopToRoot(bool animate = true)
        {
            while (_stack.Count > 1)
            {
                Entry top = _stack[_stack.Count - 1];
                _stack.RemoveAt(_stack.Count - 1);
                Hide(top.Screen, animate);
            }

            if (_stack.Count == 1)
            {
                Show(_stack[0].Screen, animate);
            }

            Changed?.Invoke(Current);
        }

        /// <summary>Hides and removes every screen.</summary>
        public void Clear(bool animate = true)
        {
            foreach (Entry entry in _stack)
            {
                Hide(entry.Screen, animate);
            }

            _stack.Clear();
            Changed?.Invoke(null);
        }

        public bool Contains(VisualElement screen) => _stack.Exists(e => e.Screen == screen);

        private void Show(VisualElement screen, bool animate)
        {
            screen.BringToFront();
            if (animate && FadeDuration > 0f)
            {
                screen.FadeIn(FadeDuration, Ease, () => screen.FocusFirst());
            }
            else
            {
                screen.FadeInNow();
                screen.FocusFirst();
            }
        }

        private void Hide(VisualElement screen, bool animate)
        {
            if (animate && FadeDuration > 0f)
            {
                screen.FadeOut(FadeDuration, Ease);
            }
            else
            {
                screen.FadeOutNow();
            }
        }

        private sealed class Entry
        {
            public readonly VisualElement Screen;
            public readonly bool Modal;

            public Entry(VisualElement screen, bool modal)
            {
                Screen = screen;
                Modal = modal;
            }
        }
    }
}
