using System.Collections.Generic;
using SpaceInvaders.Gameplay;
using UnityEditor;
using UnityEngine;

namespace SpaceInvaders.Editor
{
    /// <summary>Creates the five wave assets and the wave set. Existing assets keep their values, so designers' tweaks survive a rebuild.</summary>
    internal static class WaveAssetsBuilder
    {
        public const string Folder = "Assets/_Project/ScriptableObjects/Waves";
        public const string WaveSetPath = "Assets/_Project/ScriptableObjects/WaveSet.asset";

        private static readonly WaveSpec[] _specs =
        {
            new WaveSpec("Wave 1", 11, new[] { InvaderKind.Squid, InvaderKind.Crab, InvaderKind.Crab, InvaderKind.Octopus, InvaderKind.Octopus }, 0.9f, 1.5f, 2),
            new WaveSpec("Wave 2", 11, new[] { InvaderKind.Squid, InvaderKind.Squid, InvaderKind.Crab, InvaderKind.Crab, InvaderKind.Octopus }, 0.8f, 1.2f, 3),
            new WaveSpec("Wave 3", 11, new[] { InvaderKind.Squid, InvaderKind.Crab, InvaderKind.Crab, InvaderKind.Crab, InvaderKind.Octopus }, 0.7f, 1.0f, 3),
            new WaveSpec("Wave 4", 12, new[] { InvaderKind.Squid, InvaderKind.Squid, InvaderKind.Crab, InvaderKind.Octopus, InvaderKind.Octopus }, 0.6f, 0.9f, 4),
            new WaveSpec("Wave 5", 12, new[] { InvaderKind.Squid, InvaderKind.Squid, InvaderKind.Crab, InvaderKind.Crab, InvaderKind.Octopus, InvaderKind.Octopus }, 0.55f, 0.8f, 5)
        };

        public static WaveSet Build()
        {
            BuilderUtil.EnsureFolder(Folder);
            List<Object> waves = new();
            for (int i = 0; i < _specs.Length; i++)
            {
                string path = $"{Folder}/Wave{i + 1:00}.asset";
                bool isNew = AssetDatabase.LoadAssetAtPath<WaveDefinition>(path) == null;
                WaveDefinition wave = BuilderUtil.LoadOrCreate<WaveDefinition>(path);
                if (isNew)
                {
                    Configure(wave, _specs[i]);
                }

                waves.Add(wave);
            }

            WaveSet set = BuilderUtil.LoadOrCreate<WaveSet>(WaveSetPath);
            BuilderUtil.SetObjects(set, "_waves", waves);
            return set;
        }

        private static void Configure(WaveDefinition wave, WaveSpec spec)
        {
            BuilderUtil.Edit(wave, serialized =>
            {
                BuilderUtil.Property(serialized, "_title").stringValue = spec.Title;
                BuilderUtil.Property(serialized, "_columns").intValue = spec.Columns;
                BuilderUtil.Property(serialized, "_slowestStepSeconds").floatValue = spec.SlowestStep;
                BuilderUtil.Property(serialized, "_bombIntervalSeconds").floatValue = spec.BombInterval;
                BuilderUtil.Property(serialized, "_maxBombs").intValue = spec.MaxBombs;
                SerializedProperty rows = BuilderUtil.Property(serialized, "_rows");
                rows.arraySize = spec.Rows.Length;
                for (int i = 0; i < spec.Rows.Length; i++)
                {
                    rows.GetArrayElementAtIndex(i).intValue = (int)spec.Rows[i];
                }
            });
        }

        private sealed class WaveSpec
        {
            public readonly string Title;
            public readonly int Columns;
            public readonly InvaderKind[] Rows;
            public readonly float SlowestStep;
            public readonly float BombInterval;
            public readonly int MaxBombs;

            public WaveSpec(string title, int columns, InvaderKind[] rows, float slowestStep, float bombInterval, int maxBombs)
            {
                Title = title;
                Columns = columns;
                Rows = rows;
                SlowestStep = slowestStep;
                BombInterval = bombInterval;
                MaxBombs = maxBombs;
            }
        }
    }
}
