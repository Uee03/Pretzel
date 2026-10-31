using System;
using Pretzel.UI.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.CustomInspectors.Editors
{
    /// <summary>
    /// Keeps the default inspector layout with <see cref="InspectorElement.FillDefaultInspector"/> and adds extras below it:
    /// a live summary, a help box and buttons that edit every selected target with Undo.
    /// </summary>
    [CustomEditor(typeof(HealthStats))]
    [CanEditMultipleObjects]
    public sealed class HealthStatsEditor : UnityEditor.Editor
    {
        private const int DamageAmount = 10;

        private Label _summary;
        private HelpBox _warning;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            root.AddToClassList("pretzel-health-stats");

            // Default layout first, so new fields added to HealthStats show up without touching this editor.
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            Foldout extras = EditorUI.Section("Extras", "pretzel-health-stats-extras", parent: root);
            _summary = new Label();
            _summary.AddToClassList("pretzel-health-stats__summary");
            extras.Add(_summary);

            _warning = EditorUI.Help("Health is above Max Health on at least one object.", HelpBoxMessageType.Warning, extras);

            extras.Add(EditorUI.ButtonRow(
                ("Heal To Full", HealToFull),
                ("Damage " + DamageAmount, Damage),
                ("Kill", Kill)));

            // Fires for edits made through bound fields, Undo and the buttons below.
            root.TrackSerializedObjectValue(serializedObject, OnSerializedObjectChanged);
            Refresh();
            return root;
        }

        private void OnSerializedObjectChanged(SerializedObject changed)
        {
            Refresh();
        }

        private void HealToFull()
        {
            Edit("Heal To Full", ApplyHeal);
        }

        private void Damage()
        {
            Edit("Damage", ApplyDamage);
        }

        private void Kill()
        {
            Edit("Kill", ApplyKill);
        }

        private static void ApplyHeal(HealthStats stats)
        {
            stats.health = stats.maxHealth;
        }

        private static void ApplyDamage(HealthStats stats)
        {
            stats.health = Mathf.Max(0, stats.health - DamageAmount);
        }

        private static void ApplyKill(HealthStats stats)
        {
            stats.health = 0;
        }

        // Undo.RecordObjects before the change, and prefab instances need their override recorded.
        private void Edit(string undoName, Action<HealthStats> change)
        {
            Undo.RecordObjects(targets, undoName);
            foreach (UnityEngine.Object target in targets)
            {
                HealthStats stats = (HealthStats)target;
                change(stats);
                PrefabUtility.RecordPrefabInstancePropertyModifications(stats);
            }

            serializedObject.Update();
            Refresh();
        }

        private void Refresh()
        {
            int totalHealth = 0;
            int totalMax = 0;
            bool overMax = false;
            foreach (UnityEngine.Object target in targets)
            {
                HealthStats stats = (HealthStats)target;
                totalHealth += stats.health;
                totalMax += stats.maxHealth;
                overMax |= stats.health > stats.maxHealth;
            }

            float percent = totalMax > 0 ? 100f * totalHealth / totalMax : 0f;
            _summary.text = targets.Length == 1
                ? $"Health {totalHealth} / {totalMax} ({percent:0}%)"
                : $"{targets.Length} objects: health {totalHealth} / {totalMax} ({percent:0}%)";

            _warning.style.display = overMax ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
