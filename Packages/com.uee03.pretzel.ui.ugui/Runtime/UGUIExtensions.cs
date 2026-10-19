using Pretzel.Core;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Pretzel.UI.UGUI
{
    public static class UGUIExtensions
    {
        public static void AddListener(this Button button, UnityAction action)
        {
            Guard.NotDestroyed(button, nameof(button));
            if (action != null)
            {
                button.onClick.AddListener(action);
            }
        }

        public static void RemoveListener(this Button button, UnityAction action)
        {
            Guard.NotDestroyed(button, nameof(button));
            if (action != null)
            {
                button.onClick.RemoveListener(action);
            }
        }

        public static void RemoveAllListeners(this Button button)
        {
            Guard.NotDestroyed(button, nameof(button));
            button.onClick.RemoveAllListeners();
        }

        /// <summary>Sets the text of a placeholder graphic. Does nothing unless the graphic is a <see cref="TMP_Text"/>.</summary>
        public static void SetPlaceHolderText(this Graphic graphic, string text)
        {
            if (graphic is TMP_Text placeholder)
            {
                placeholder.SetText(text);
            }
        }
    }
}
