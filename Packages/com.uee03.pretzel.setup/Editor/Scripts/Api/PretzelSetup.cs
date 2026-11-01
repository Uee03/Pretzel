using System;
using System.Collections.Generic;
using Pretzel.Setup.Composition;
using Pretzel.Setup.Folders;
using Pretzel.Setup.Settings;
using UnityEditor;

namespace Pretzel.Setup
{
    /// <summary>
    /// What the Project Setup window does, without the window: create the preset folders and generate the VContainer composition root.
    /// Reference <c>Pretzel.Setup.Editor</c> from your Editor assembly to call it, or run it from the command line with
    /// <see cref="PretzelSetupBatch"/>. Both use the project settings the window edits (<see cref="PretzelSettings.Project"/>);
    /// change <c>PretzelSettings.Project.composition</c> and call <see cref="PretzelSettings.SaveProject"/> to configure a run.
    /// </summary>
    public static class PretzelSetup
    {
        /// <summary>Creates the folders of the active preset. Returns how many were created.</summary>
        public static int CreateFolders()
        {
            int created = ProjectFolders.Create(PretzelSettings.Project.ActivePreset.folders);
            AssetDatabase.SaveAssets();
            return created;
        }

        /// <summary>
        /// Writes the composition root code from the project's composition settings. When new code was written the assets are
        /// created after Unity compiles it; otherwise they are created or repaired now.
        /// </summary>
        /// <exception cref="ArgumentException">The settings are invalid.</exception>
        /// <exception cref="InvalidOperationException">VContainer or Pretzel Integrations is not installed, or the installer is not compiled.</exception>
        public static CompositionResult GenerateComposition()
        {
            return GenerateComposition(exitWhenAssetsAreApplied: false);
        }

        /// <summary>Creates or repairs the installer asset, scope prefab and VContainer settings. The generated code must be compiled.</summary>
        /// <exception cref="InvalidOperationException">The installer is not compiled yet.</exception>
        public static IReadOnlyList<string> ApplyComposition()
        {
            return CompositionAssets.Apply(Settings());
        }

        internal static CompositionResult GenerateComposition(bool exitWhenAssetsAreApplied)
        {
            if (!CompositionAssets.IsIntegrationActive)
            {
                throw new InvalidOperationException("Install VContainer and the Pretzel Integrations module first.");
            }

            CompositionSettings settings = Settings();
            IReadOnlyList<string> written = CompositionGenerator.Generate(settings);
            if (written.Count > 0)
            {
                CompositionAfterCompile.Request(exitWhenAssetsAreApplied);
                AssetDatabase.Refresh();
                return new CompositionResult(written, Array.Empty<string>(), true);
            }

            return new CompositionResult(written, CompositionAssets.Apply(settings), false);
        }

        private static CompositionSettings Settings()
        {
            CompositionSettings settings = PretzelSettings.Project.composition;
            settings.Normalize();
            return settings;
        }
    }
}
