using System.Collections.Generic;
using Pretzel.UI.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.CustomInspectors.Editors
{
    /// <summary>
    /// Multi-object editing: PropertyFields show mixed values on their own, a summary reads every target, and the
    /// buttons write to each selected object through its own SerializedObject, so Undo and prefab overrides work per object.
    /// </summary>
    [CustomEditor(typeof(TeamMember))]
    [CanEditMultipleObjects]
    public sealed class TeamMemberEditor : UnityEditor.Editor
    {
        private const int TeamCount = 3;

        private readonly int[] _teamSizes = new int[TeamCount];
        private Label _summary;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            root.AddToClassList("pretzel-team-member");

            if (serializedObject.isEditingMultipleObjects)
            {
                EditorUI.Help("Several objects are selected. Fields that differ show a dash; editing one field writes it to every selected object.", HelpBoxMessageType.Info, root);
            }

            root.Add(new PropertyField { bindingPath = nameof(TeamMember.memberName) });
            root.Add(new PropertyField { bindingPath = nameof(TeamMember.team) });
            root.Add(new PropertyField { bindingPath = nameof(TeamMember.level) });

            _summary = new Label();
            _summary.AddToClassList("pretzel-team-member__summary");
            root.Add(_summary);

            root.Add(EditorUI.ButtonRow(
                ("Red", SetRed),
                ("Blue", SetBlue),
                ("Green", SetGreen)));
            root.Add(EditorUI.ButtonRow(
                ("Level +1", LevelUp),
                ("Number Names", NumberNames)));

            root.TrackSerializedObjectValue(serializedObject, OnSerializedObjectChanged);
            RefreshSummary();
            return root;
        }

        private void OnSerializedObjectChanged(SerializedObject changed)
        {
            RefreshSummary();
        }

        private void SetRed()
        {
            SetTeam(TeamColor.Red);
        }

        private void SetBlue()
        {
            SetTeam(TeamColor.Blue);
        }

        private void SetGreen()
        {
            SetTeam(TeamColor.Green);
        }

        private void SetTeam(TeamColor team)
        {
            Undo.SetCurrentGroupName("Set Team");
            foreach (UnityEngine.Object item in targets)
            {
                using SerializedObject so = new(item);
                so.FindProperty(nameof(TeamMember.team)).enumValueIndex = (int)team;
                so.ApplyModifiedProperties();
            }
        }

        private void LevelUp()
        {
            Undo.SetCurrentGroupName("Level Up");
            foreach (UnityEngine.Object item in targets)
            {
                using SerializedObject so = new(item);
                SerializedProperty level = so.FindProperty(nameof(TeamMember.level));
                level.intValue++;
                so.ApplyModifiedProperties();
            }
        }

        // Each object gets its own value, which a single bound field cannot do.
        private void NumberNames()
        {
            Undo.SetCurrentGroupName("Number Names");
            List<UnityEngine.Object> ordered = new(targets);
            ordered.Sort(CompareByName);
            for (int i = 0; i < ordered.Count; i++)
            {
                using SerializedObject so = new(ordered[i]);
                so.FindProperty(nameof(TeamMember.memberName)).stringValue = "Member " + (i + 1);
                so.ApplyModifiedProperties();
            }
        }

        private static int CompareByName(UnityEngine.Object a, UnityEngine.Object b)
        {
            return string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase);
        }

        private void RefreshSummary()
        {
            System.Array.Clear(_teamSizes, 0, _teamSizes.Length);
            foreach (UnityEngine.Object item in targets)
            {
                _teamSizes[(int)((TeamMember)item).team]++;
            }

            _summary.text = $"{targets.Length} selected: Red {_teamSizes[(int)TeamColor.Red]}, Blue {_teamSizes[(int)TeamColor.Blue]}, Green {_teamSizes[(int)TeamColor.Green]}";
        }
    }
}
