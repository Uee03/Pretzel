using System;
using Pretzel.Core;
using UnityEngine.UIElements;

namespace Pretzel.UI
{
    public static class VisualElementExtensions
    {
        /// <summary>Sets <c>display</c>. A hidden element takes no layout space and receives no events.</summary>
        public static void SetDisplayed(this VisualElement element, bool displayed)
        {
            Guard.NotNull(element, nameof(element)).style.display = displayed ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static bool IsDisplayed(this VisualElement element)
        {
            Guard.NotNull(element, nameof(element));
            return element.style.display != DisplayStyle.None && element.resolvedStyle.display != DisplayStyle.None;
        }

        /// <summary>Sets <c>visibility</c>. A hidden element keeps its layout space.</summary>
        public static void SetVisible(this VisualElement element, bool visible)
        {
            Guard.NotNull(element, nameof(element)).style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }

        /// <summary>Queries a descendant and throws with a clear message when the UXML does not contain it.</summary>
        public static T QRequired<T>(this VisualElement root, string name = null, string className = null) where T : VisualElement
        {
            Guard.NotNull(root, nameof(root));
            T element = root.Q<T>(name, className);
            if (element == null)
            {
                string what = name != null ? $"'{name}'" : className != null ? $"with class '{className}'" : "of that type";
                throw new InvalidOperationException($"No {typeof(T).Name} {what} under '{root.name}'. Check the UXML.");
            }

            return element;
        }

        /// <summary>Registers a click handler. Dispose the result to unregister, for example in <c>OnUnbind</c>.</summary>
        public static IDisposable OnClick(this VisualElement element, Action onClick)
        {
            Guard.NotNull(element, nameof(element));
            Guard.NotNull(onClick, nameof(onClick));
            EventCallback<ClickEvent> callback = _ => onClick();
            element.RegisterCallback(callback);
            return new DisposableAction(() => element.UnregisterCallback(callback));
        }

        /// <summary>Registers any event callback. Dispose the result to unregister.</summary>
        public static IDisposable RegisterDisposable<TEvent>(this VisualElement element, EventCallback<TEvent> callback, TrickleDown trickleDown = TrickleDown.NoTrickleDown)
            where TEvent : EventBase<TEvent>, new()
        {
            Guard.NotNull(element, nameof(element));
            Guard.NotNull(callback, nameof(callback));
            element.RegisterCallback(callback, trickleDown);
            return new DisposableAction(() => element.UnregisterCallback(callback, trickleDown));
        }

        /// <summary>Registers a value-changed callback on a field. Dispose the result to unregister.</summary>
        public static IDisposable OnValueChanged<T>(this INotifyValueChanged<T> field, EventCallback<ChangeEvent<T>> callback)
        {
            Guard.NotNull(field, nameof(field));
            Guard.NotNull(callback, nameof(callback));
            field.RegisterValueChangedCallback(callback);
            return new DisposableAction(() => field.UnregisterValueChangedCallback(callback));
        }

        /// <summary>Focuses the first focusable, displayed descendant, which gamepad and keyboard navigation need to start.</summary>
        /// <returns>False when nothing could take focus.</returns>
        public static bool FocusFirst(this VisualElement root)
        {
            Guard.NotNull(root, nameof(root));
            VisualElement target = root.Query<VisualElement>().Where(e => e.focusable && e.canGrabFocus && e.enabledInHierarchy && IsDisplayedInHierarchy(e, root)).First();
            if (target == null)
            {
                return false;
            }

            target.Focus();
            return true;
        }

        /// <summary>Sets the picking mode of the element and all of its descendants.</summary>
        public static void SetPickingModeRecursive(this VisualElement element, PickingMode mode)
        {
            Guard.NotNull(element, nameof(element));
            element.pickingMode = mode;
            foreach (VisualElement child in element.Children())
            {
                child.SetPickingModeRecursive(mode);
            }
        }

        private static bool IsDisplayedInHierarchy(VisualElement element, VisualElement root)
        {
            for (VisualElement current = element; current != null; current = current.parent)
            {
                if (current.style.display == DisplayStyle.None || current.resolvedStyle.display == DisplayStyle.None)
                {
                    return false;
                }

                if (current == root)
                {
                    break;
                }
            }

            return true;
        }
    }
}
