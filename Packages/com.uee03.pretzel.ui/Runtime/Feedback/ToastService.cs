using System.Collections.Generic;
using Pretzel.Core;
using UnityEngine.UIElements;

namespace Pretzel.UI
{
    /// <summary>
    /// Short notifications stacked in a container element. Style them with the <c>pretzel-toast</c> class and its
    /// <c>--info</c>, <c>--success</c>, <c>--warning</c> and <c>--error</c> modifiers. The oldest toast goes when the limit is reached.
    /// </summary>
    public sealed class ToastService
    {
        public const string UssClassName = "pretzel-toast";

        private readonly VisualElement _container;
        private readonly Queue<Label> _visible = new();

        public int MaxVisible { get; set; }

        public float FadeDuration { get; set; } = 0.2f;

        public ToastService(VisualElement container, int maxVisible = 3)
        {
            _container = Guard.NotNull(container, nameof(container));
            MaxVisible = maxVisible;
        }

        public void Show(string message, float seconds = 2.5f, ToastKind kind = ToastKind.Info)
        {
            while (_visible.Count >= System.Math.Max(1, MaxVisible))
            {
                _visible.Dequeue().RemoveFromHierarchy();
            }

            Label toast = new(message) { enableRichText = false };
            toast.AddToClassList(UssClassName);
            toast.AddToClassList(UssClassName + "--" + kind.ToString().ToLowerInvariant());
            toast.pickingMode = PickingMode.Ignore;
            toast.style.opacity = 0f;
            _container.Add(toast);
            _visible.Enqueue(toast);
            toast.FadeIn(FadeDuration);
            toast.schedule.Execute(() => Dismiss(toast)).StartingIn((long)(System.Math.Max(0.1f, seconds) * 1000f));
        }

        public void Clear()
        {
            while (_visible.Count > 0)
            {
                _visible.Dequeue().RemoveFromHierarchy();
            }
        }

        private void Dismiss(Label toast)
        {
            if (toast.parent == null)
            {
                return;
            }

            toast.FadeOut(FadeDuration, onComplete: () =>
            {
                toast.RemoveFromHierarchy();
                RemoveFromQueue(toast);
            });
        }

        private void RemoveFromQueue(Label toast)
        {
            int count = _visible.Count;
            for (int i = 0; i < count; i++)
            {
                Label item = _visible.Dequeue();
                if (item != toast)
                {
                    _visible.Enqueue(item);
                }
            }
        }
    }
}
