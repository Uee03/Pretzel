using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace Pretzel.Samples.UI.EditorWindows
{
    /// <summary>
    /// The Dashboard's logic, without any UI: counts assets by category in small steps so a window can keep drawing
    /// while a scheduled item calls <see cref="Step"/>.
    /// </summary>
    public sealed class ProjectScanner
    {
        private readonly int[] _counts = new int[Enum.GetValues(typeof(AssetCategory)).Length];
        private string[] _paths = Array.Empty<string>();
        private int _next;

        public bool IsRunning { get; private set; }

        public int Total => _paths.Length;

        public int Processed => _next;

        public IReadOnlyList<int> Counts => _counts;

        public string LargestPath { get; private set; }

        public long LargestBytes { get; private set; }

        public void Begin()
        {
            _paths = AssetDatabase.GetAllAssetPaths();
            _next = 0;
            Array.Clear(_counts, 0, _counts.Length);
            LargestPath = null;
            LargestBytes = 0;
            IsRunning = true;
        }

        public void Cancel()
        {
            IsRunning = false;
        }

        /// <summary>Processes up to <paramref name="budget"/> paths. Returns true when the scan is finished.</summary>
        public bool Step(int budget)
        {
            if (!IsRunning)
            {
                return true;
            }

            int end = Math.Min(_next + budget, _paths.Length);
            for (; _next < end; _next++)
            {
                Process(_paths[_next]);
            }

            if (_next >= _paths.Length)
            {
                IsRunning = false;
            }

            return !IsRunning;
        }

        private void Process(string path)
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return;
            }

            FileInfo info = new(path);
            if (!info.Exists)
            {
                return;
            }

            _counts[(int)Categorize(path)]++;
            if (info.Length > LargestBytes)
            {
                LargestBytes = info.Length;
                LargestPath = path;
            }
        }

        private static AssetCategory Categorize(string path)
        {
            string extension = Path.GetExtension(path);
            if (string.Equals(extension, ".unity", StringComparison.OrdinalIgnoreCase))
            {
                return AssetCategory.Scenes;
            }

            if (string.Equals(extension, ".prefab", StringComparison.OrdinalIgnoreCase))
            {
                return AssetCategory.Prefabs;
            }

            if (string.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase))
            {
                return AssetCategory.Scripts;
            }

            if (string.Equals(extension, ".mat", StringComparison.OrdinalIgnoreCase))
            {
                return AssetCategory.Materials;
            }

            if (string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".tga", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".psd", StringComparison.OrdinalIgnoreCase))
            {
                return AssetCategory.Textures;
            }

            return AssetCategory.Other;
        }
    }
}
