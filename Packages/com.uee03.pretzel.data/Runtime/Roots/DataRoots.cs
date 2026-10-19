using System;
using System.IO;
using Pretzel.Core;
using UnityEngine;

namespace Pretzel.Data
{
    /// <summary>Built-in <see cref="IDataRoot"/> factories. Nothing is read from <see cref="Application"/> until the root is first used.</summary>
    public static class DataRoots
    {
        /// <summary><see cref="Application.persistentDataPath"/>, optionally with a sub-folder.</summary>
        public static IDataRoot PersistentData(string subFolder = null)
        {
            return new LazyRoot(() => Combine(Application.persistentDataPath, subFolder));
        }

        /// <summary><see cref="Application.temporaryCachePath"/>, optionally with a sub-folder.</summary>
        public static IDataRoot TemporaryCache(string subFolder = null)
        {
            return new LazyRoot(() => Combine(Application.temporaryCachePath, subFolder));
        }

        /// <summary>An absolute folder path.</summary>
        public static IDataRoot Absolute(string path)
        {
            Guard.NotNullOrEmpty(path, nameof(path));
            if (!Path.IsPathRooted(path))
            {
                throw new ArgumentException("Path must be absolute.", nameof(path));
            }

            return new LazyRoot(() => path);
        }

#if UNITY_EDITOR
        /// <summary>The project's ProjectSettings folder. Editor only. Shared with the team.</summary>
        public static IDataRoot ProjectSettings(string subFolder = null)
        {
            return new LazyRoot(() => Combine(Path.Combine(ProjectFolder(), "ProjectSettings"), subFolder));
        }

        /// <summary>The project's UserSettings folder. Editor only. Per user.</summary>
        public static IDataRoot UserSettings(string subFolder = null)
        {
            return new LazyRoot(() => Combine(Path.Combine(ProjectFolder(), "UserSettings"), subFolder));
        }

        private static string ProjectFolder() => Directory.GetParent(Application.dataPath).FullName;
#endif

        private static string Combine(string basePath, string subFolder)
        {
            if (string.IsNullOrEmpty(subFolder))
            {
                return basePath;
            }

            string error = DataNames.ValidateFolder(subFolder);
            if (error != null)
            {
                throw new ArgumentException(error, nameof(subFolder));
            }

            return Path.Combine(basePath, subFolder);
        }

        private sealed class LazyRoot : IDataRoot
        {
            private readonly Func<string> _resolve;

            public LazyRoot(Func<string> resolve)
            {
                _resolve = resolve;
            }

            public string FullPath => _resolve();
        }
    }
}
