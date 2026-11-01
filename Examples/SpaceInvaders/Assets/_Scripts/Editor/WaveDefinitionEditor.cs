using System.Text;
using Pretzel.UI.Editor;
using SpaceInvaders.Gameplay;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceInvaders.Editor
{
    /// <summary>Wave inspector: the usual fields, plus a live preview of the formation and what the numbers mean in play.</summary>
    [CustomEditor(typeof(WaveDefinition))]
    public sealed class WaveDefinitionEditor : InspectorBase
    {
        private const float CellSize = 18f;

        private static readonly Color[] _kindColors =
        {
            new Color(1f, 0.35f, 0.85f),
            new Color(0.3f, 0.9f, 1f),
            new Color(0.4f, 1f, 0.45f)
        };

        protected override void BuildInspector(VisualElement root)
        {
            AddDefaultProperties(root, "m_Script");
            EditorUI.Separator(root);

            Foldout preview = EditorUI.Section("Formation preview", "SpaceInvaders.WavePreview", true, root);
            VisualElement grid = new();
            grid.style.alignSelf = Align.Center;
            grid.style.marginTop = 4;
            grid.style.marginBottom = 6;
            Label stats = new();
            stats.style.whiteSpace = WhiteSpace.Normal;
            VisualElement warnings = new();
            preview.Add(grid);
            preview.Add(stats);
            preview.Add(warnings);

            root.TrackSerializedObjectValue(serializedObject, _ => Rebuild(grid, stats, warnings));
            Rebuild(grid, stats, warnings);
        }

        private void Rebuild(VisualElement grid, Label stats, VisualElement warnings)
        {
            SerializedProperty rows = serializedObject.FindProperty("_rows");
            int columns = serializedObject.FindProperty("_columns").intValue;
            grid.Clear();
            warnings.Clear();

            int points = 0;
            for (int row = 0; row < rows.arraySize; row++)
            {
                InvaderKind kind = (InvaderKind)rows.GetArrayElementAtIndex(row).intValue;
                points += ScoreTable.PointsFor(kind) * columns;
                VisualElement line = EditorUI.Row();
                for (int column = 0; column < columns; column++)
                {
                    VisualElement cell = new();
                    cell.style.width = CellSize;
                    cell.style.height = CellSize * 0.7f;
                    cell.style.marginRight = 2;
                    cell.style.marginBottom = 2;
                    cell.style.borderTopLeftRadius = cell.style.borderTopRightRadius = 3;
                    cell.style.backgroundColor = _kindColors[(int)kind];
                    cell.tooltip = $"{kind}: {ScoreTable.PointsFor(kind)} points";
                    line.Add(cell);
                }

                grid.Add(line);
            }

            float slowest = serializedObject.FindProperty("_slowestStepSeconds").floatValue;
            float fastest = serializedObject.FindProperty("_fastestStepSeconds").floatValue;
            float bombs = serializedObject.FindProperty("_bombIntervalSeconds").floatValue;
            float spacingX = serializedObject.FindProperty("_spacing").vector2Value.x;
            StringBuilder text = new();
            text.Append($"{rows.arraySize * columns} invaders, {points} points to clear the wave.\n");
            text.Append($"Formation width {Mathf.Max(0, columns - 1) * spacingX + 0.9f:0.0} units. ");
            text.Append($"A bomb about every {bombs:0.0} s, up to {serializedObject.FindProperty("_maxBombs").intValue} at once.");
            stats.text = text.ToString();

            if (rows.arraySize == 0)
            {
                EditorUI.Help("This wave has no rows, so nobody would attack.", HelpBoxMessageType.Error, warnings);
            }

            if (fastest > slowest)
            {
                EditorUI.Help("The fastest step is slower than the slowest step. The wave uses the slowest value for both.", HelpBoxMessageType.Warning, warnings);
            }

            if (Mathf.Max(0, columns - 1) * spacingX + 0.9f > 15.2f)
            {
                EditorUI.Help("The formation is wider than the playfield (about 15 units) and would start outside it.", HelpBoxMessageType.Warning, warnings);
            }
        }
    }
}
