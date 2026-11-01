using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SpaceInvaders.Editor
{
    /// <summary>Builds the demo as a Windows player from the scenes in the build settings.</summary>
    public static class PlayerBuilder
    {
        private const string OutputArgument = "-buildOutput";

        [MenuItem("Space Invaders/Build Windows Player", priority = 30)]
        public static void BuildWindows()
        {
            string output = ReadArgument(OutputArgument)
                            ?? Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "SpaceInvaders", "SpaceInvaders.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));

            BuildPlayerOptions options = new()
            {
                scenes = new[] { SceneBuilder.TitleScenePath, SceneBuilder.GameScenePath, SceneBuilder.CoopScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"[SI] Build {summary.result}: {summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors, {summary.totalWarnings} warnings -> {output}");
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("The player build failed. See the log for the errors.");
            }
        }

        private static string ReadArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.Ordinal))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
