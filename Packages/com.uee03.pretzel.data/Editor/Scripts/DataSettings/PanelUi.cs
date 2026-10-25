using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Data.Editor
{
    internal interface IDataFormatPanel
    {
        string Title { get; }

        VisualElement CreateView(DataFormatState state);

        void OnSelected();
    }

    internal static class PanelUi
    {
        public const string Ok = "ok";
        public const string Warn = "warn";
        public const string Missing = "missing";

        /// <summary>Asks the user to confirm. Replaceable, so automated checks can answer without a window.</summary>
        public static Func<string, string, string, string, bool> Confirm = UnityEditor.EditorUtility.DisplayDialog;

        public static void FillBullets(VisualElement container, IReadOnlyList<string> items, bool numbered = false)
        {
            container.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                Label label = new((numbered ? (i + 1) + ".  " : "-  ") + items[i]);
                label.AddToClassList("pretzel-bullets__item");
                container.Add(label);
            }
        }

        public static void SetChip(Label chip, string text, string kind)
        {
            chip.text = text;
            chip.RemoveFromClassList("pretzel-chip--ok");
            chip.RemoveFromClassList("pretzel-chip--warn");
            chip.RemoveFromClassList("pretzel-chip--missing");
            chip.AddToClassList("pretzel-chip");
            chip.AddToClassList("pretzel-chip--" + kind);
        }

        public static Label NewChip(string text, string kind)
        {
            Label chip = new();
            SetChip(chip, text, kind);
            return chip;
        }

        public static void SetVisible(VisualElement element, bool visible)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>A short fingerprint, so a key can be recognised without showing it.</summary>
        public static string Fingerprint(byte[] data) => EncryptionKeys.Fingerprint(data);

        public static void Copy(string text)
        {
            GUIUtility.systemCopyBuffer = text ?? string.Empty;
        }

        public static void SetText(Label label, string text, string modifier = null)
        {
            label.text = text;
            label.RemoveFromClassList("pretzel-section__hint--warning");
            label.RemoveFromClassList("pretzel-section__hint--error");
            if (modifier != null)
            {
                label.AddToClassList("pretzel-section__hint--" + modifier);
            }
        }

        public static string ErrorText(Exception ex) => ex.InnerException?.Message ?? ex.Message;

        public static bool IsExpectedFailure(Exception ex)
        {
            return ex is System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException
                or ArgumentException or InvalidOperationException or FormatException or NotSupportedException
                or Newtonsoft.Json.JsonException or System.Security.Cryptography.CryptographicException
                or System.Security.SecurityException;
        }
    }
}
