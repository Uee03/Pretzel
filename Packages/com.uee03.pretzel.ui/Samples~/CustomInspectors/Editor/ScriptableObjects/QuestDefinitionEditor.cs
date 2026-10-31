using System.Collections.Generic;
using System.Text;
using Pretzel.UI.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.CustomInspectors.Editors
{
    /// <summary>
    /// A ScriptableObject inspector on <see cref="InspectorBase"/>: default fields, live validation in help boxes and buttons
    /// that edit through the SerializedObject, so Undo and dirty tracking come for free.
    /// </summary>
    [CustomEditor(typeof(QuestDefinition))]
    public sealed class QuestDefinitionEditor : InspectorBase
    {
        private readonly StringBuilder _errors = new();
        private readonly StringBuilder _warnings = new();
        private readonly HashSet<string> _seen = new(System.StringComparer.OrdinalIgnoreCase);
        private HelpBox _errorBox;
        private HelpBox _warningBox;

        protected override void BuildInspector(VisualElement root)
        {
            AddDefaultProperties(root);

            _errorBox = EditorUI.Help(string.Empty, HelpBoxMessageType.Error, root);
            _warningBox = EditorUI.Help(string.Empty, HelpBoxMessageType.Warning, root);

            root.Add(EditorUI.ButtonRow(
                ("Generate Id From Title", GenerateId),
                ("Add Objective", AddObjective),
                ("Ping", Ping)));

            root.TrackSerializedObjectValue(serializedObject, OnSerializedObjectChanged);
            Validate();
        }

        private void OnSerializedObjectChanged(SerializedObject changed)
        {
            Validate();
        }

        private void Validate()
        {
            QuestDefinition quest = (QuestDefinition)target;
            _errors.Clear();
            _warnings.Clear();
            _seen.Clear();

            if (string.IsNullOrWhiteSpace(quest.id))
            {
                _errors.AppendLine("Id is required.");
            }

            if (string.IsNullOrWhiteSpace(quest.title))
            {
                _errors.AppendLine("Title is required.");
            }

            if (quest.minLevel > quest.maxLevel)
            {
                _errors.AppendLine("Min Level is above Max Level.");
            }

            if (quest.objectives.Count == 0)
            {
                _warnings.AppendLine("The quest has no objectives.");
            }

            foreach (string objective in quest.objectives)
            {
                if (string.IsNullOrWhiteSpace(objective))
                {
                    _warnings.AppendLine("An objective is empty.");
                }
                else if (!_seen.Add(objective))
                {
                    _warnings.AppendLine("Duplicate objective: " + objective);
                }
            }

            Show(_errorBox, _errors);
            Show(_warningBox, _warnings);
        }

        private static void Show(HelpBox box, StringBuilder messages)
        {
            bool any = messages.Length > 0;
            box.style.display = any ? DisplayStyle.Flex : DisplayStyle.None;
            if (any)
            {
                box.text = messages.ToString().TrimEnd();
            }
        }

        private void GenerateId()
        {
            SerializedProperty title = serializedObject.FindProperty(nameof(QuestDefinition.title));
            SerializedProperty id = serializedObject.FindProperty(nameof(QuestDefinition.id));
            id.stringValue = Slugify(title.stringValue);
            serializedObject.ApplyModifiedProperties();
        }

        private void AddObjective()
        {
            SerializedProperty objectives = serializedObject.FindProperty(nameof(QuestDefinition.objectives));
            objectives.arraySize++;
            objectives.GetArrayElementAtIndex(objectives.arraySize - 1).stringValue = "New objective";
            serializedObject.ApplyModifiedProperties();
        }

        private void Ping()
        {
            EditorGUIUtility.PingObject(target);
        }

        private static string Slugify(string text)
        {
            StringBuilder slug = new();
            bool lastWasSeparator = true;
            foreach (char c in text ?? string.Empty)
            {
                if (char.IsLetterOrDigit(c))
                {
                    slug.Append(char.ToLowerInvariant(c));
                    lastWasSeparator = false;
                }
                else if (!lastWasSeparator)
                {
                    slug.Append('-');
                    lastWasSeparator = true;
                }
            }

            return slug.ToString().TrimEnd('-');
        }
    }
}
