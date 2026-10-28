using System;
using System.Collections.Generic;
using System.IO;
using Pretzel.Core;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.UI.Editor
{
    /// <summary>
    /// Building blocks for UI Toolkit inspectors, property drawers and editor windows. Elements carry <c>pretzel-editor__*</c>
    /// classes, so a stylesheet can restyle them, and use only built-in editor styling by default.
    /// </summary>
    public static class EditorUI
    {
        public const string Block = "pretzel-editor";

        public static bool IsDarkSkin => EditorGUIUtility.isProSkin;

        /// <summary>Adds <c>pretzel-editor--dark</c> or <c>pretzel-editor--light</c>, so a stylesheet can follow the editor skin.</summary>
        public static void ApplySkinClass(VisualElement root)
        {
            Guard.NotNull(root, nameof(root));
            root.EnableInClassList(Block + "--dark", IsDarkSkin);
            root.EnableInClassList(Block + "--light", !IsDarkSkin);
        }

        public static Label Header(string text, VisualElement parent = null)
        {
            Label label = new(text);
            label.AddToClassList(Block + "__header");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 13;
            label.style.marginTop = 6;
            label.style.marginBottom = 2;
            parent?.Add(label);
            return label;
        }

        /// <param name="persistenceKey">Remembers the open state per window or inspector. Must be unique within the panel.</param>
        public static Foldout Section(string title, string persistenceKey = null, bool open = true, VisualElement parent = null)
        {
            Foldout foldout = new() { text = title, value = open };
            foldout.AddToClassList(Block + "__section");
            if (!string.IsNullOrEmpty(persistenceKey))
            {
                foldout.viewDataKey = persistenceKey;
            }

            parent?.Add(foldout);
            return foldout;
        }

        public static HelpBox Help(string message, HelpBoxMessageType type = HelpBoxMessageType.Info, VisualElement parent = null)
        {
            HelpBox box = new(message, type);
            box.AddToClassList(Block + "__help");
            parent?.Add(box);
            return box;
        }

        public static VisualElement Separator(VisualElement parent = null)
        {
            VisualElement line = new();
            line.AddToClassList(Block + "__separator");
            line.style.height = 1;
            line.style.marginTop = line.style.marginBottom = 4;
            line.style.backgroundColor = IsDarkSkin ? new Color(1f, 1f, 1f, 0.12f) : new Color(0f, 0f, 0f, 0.15f);
            parent?.Add(line);
            return line;
        }

        public static VisualElement Row(params VisualElement[] children)
        {
            VisualElement row = new();
            row.AddToClassList(Block + "__row");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            foreach (VisualElement child in children)
            {
                if (child != null)
                {
                    row.Add(child);
                }
            }

            return row;
        }

        public static Button Button(string text, Action onClick, string tooltip = null)
        {
            Button button = new(onClick) { text = text, tooltip = tooltip };
            button.AddToClassList(Block + "__button");
            return button;
        }

        /// <summary>A row of equally wide buttons.</summary>
        public static VisualElement ButtonRow(params (string Label, Action OnClick)[] buttons)
        {
            VisualElement row = Row();
            foreach ((string label, Action onClick) in buttons)
            {
                Button button = Button(label, onClick);
                button.style.flexGrow = 1;
                button.style.flexBasis = 0;
                row.Add(button);
            }

            return row;
        }

        /// <summary>
        /// PropertyFields for every visible top-level property, bound to <paramref name="serializedObject"/>.
        /// The script field is shown disabled, as in the default inspector, unless <paramref name="includeScript"/> is false.
        /// </summary>
        public static VisualElement PropertyFields(SerializedObject serializedObject, bool includeScript = true, params string[] exclude)
        {
            Guard.NotNull(serializedObject, nameof(serializedObject));
            HashSet<string> skipped = new(exclude ?? Array.Empty<string>(), StringComparer.Ordinal);
            VisualElement container = new();
            container.AddToClassList(Block + "__properties");

            SerializedProperty iterator = serializedObject.GetIterator();
            for (bool enter = true; iterator.NextVisible(enter); enter = false)
            {
                if (skipped.Contains(iterator.propertyPath))
                {
                    continue;
                }

                bool isScript = string.Equals(iterator.propertyPath, "m_Script", StringComparison.Ordinal);
                if (isScript && !includeScript)
                {
                    continue;
                }

                PropertyField field = new(iterator.Copy()) { name = "property-" + iterator.propertyPath };
                field.SetEnabled(!isScript);
                container.Add(field);
            }

            container.Bind(serializedObject);
            return container;
        }

        /// <summary>A text field with a browse button for a folder. Project folders are returned as "Assets/..." paths.</summary>
        public static VisualElement FolderField(string label, string value, Action<string> onChanged, bool projectRelative = true)
        {
            return PathField(label, value, onChanged, current =>
            {
                string picked = EditorUtility.OpenFolderPanel(label, StartFolder(current), string.Empty);
                return projectRelative ? ToProjectPath(picked) : picked;
            });
        }

        /// <param name="extension">File extension without the dot, or empty for any file.</param>
        public static VisualElement FileField(string label, string value, string extension, Action<string> onChanged, bool projectRelative = true)
        {
            return PathField(label, value, onChanged, current =>
            {
                string picked = EditorUtility.OpenFilePanel(label, StartFolder(current), extension ?? string.Empty);
                return projectRelative ? ToProjectPath(picked) : picked;
            });
        }

        /// <summary>Loads a stylesheet and warns when the path is wrong, instead of failing silently.</summary>
        public static StyleSheet LoadStyleSheet(string assetPath)
        {
            StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(assetPath);
            if (!sheet)
            {
                Debug.LogWarning($"Stylesheet not found at '{assetPath}'.");
            }

            return sheet;
        }

        public static VisualTreeAsset LoadLayout(string assetPath)
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
            if (!tree)
            {
                Debug.LogWarning($"UXML not found at '{assetPath}'.");
            }

            return tree;
        }

        /// <summary>Converts an absolute path inside the project to an "Assets/..." path. Other paths are returned unchanged.</summary>
        public static string ToProjectPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
            {
                return absolutePath;
            }

            string normalized = absolutePath.Replace('\\', '/');
            string project = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/') + "/";
            return normalized.StartsWith(project, StringComparison.OrdinalIgnoreCase) ? normalized.Substring(project.Length) : normalized;
        }

        private static VisualElement PathField(string label, string value, Action<string> onChanged, Func<string, string> browse)
        {
            Guard.NotNull(onChanged, nameof(onChanged));
            TextField field = new(label) { value = value ?? string.Empty, isDelayed = true };
            field.AddToClassList(Block + "__path-field");
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => onChanged(evt.newValue));

            Button browseButton = Button("...", () =>
            {
                string picked = browse(field.value);
                if (!string.IsNullOrEmpty(picked))
                {
                    field.value = picked;
                }
            }, "Browse");
            browseButton.style.width = 28;

            VisualElement row = Row(field, browseButton);
            row.AddToClassList(Block + "__path");
            return row;
        }

        private static string StartFolder(string current)
        {
            if (string.IsNullOrEmpty(current))
            {
                return Application.dataPath;
            }

            string full = Path.IsPathRooted(current) ? current : Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, current);
            return Directory.Exists(full) ? full : Path.GetDirectoryName(full) ?? Application.dataPath;
        }
    }
}
