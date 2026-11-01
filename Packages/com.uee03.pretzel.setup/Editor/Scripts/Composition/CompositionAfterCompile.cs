using System;
using System.Collections.Generic;
using Pretzel.Setup.Settings;
using UnityEditor;
using UnityEngine;

namespace Pretzel.Setup.Composition
{
    /// <summary>Finishes "Generate and set up" after the generated code has compiled.</summary>
    [InitializeOnLoad]
    internal static class CompositionAfterCompile
    {
        private const string PendingKey = "Pretzel.Composition.ApplyAfterCompile";
        private const string ExitKey = "Pretzel.Composition.ExitAfterApply";

        static CompositionAfterCompile()
        {
            if (SessionState.GetBool(PendingKey, false))
            {
                EditorApplication.delayCall += ApplyPending;
            }
        }

        public static event Action<IReadOnlyList<string>> Applied;

        /// <param name="exitWhenDone">For a batch run: quit the Editor once the assets exist (exit code 0) or could not be created (1).</param>
        public static void Request(bool exitWhenDone = false)
        {
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(ExitKey, exitWhenDone);
        }

        private static void ApplyPending()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += ApplyPending;
                return;
            }

            SessionState.EraseBool(PendingKey);
            bool exitWhenDone = SessionState.GetBool(ExitKey, false);
            SessionState.EraseBool(ExitKey);
            int exitCode = 0;
            try
            {
                IReadOnlyList<string> log = CompositionAssets.Apply(PretzelSettings.Project.composition);
                Debug.Log("[Pretzel] VContainer setup:\n" + string.Join("\n", log));
                Applied?.Invoke(log);
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogWarning("[Pretzel] VContainer setup did not finish: " + ex.Message);
                exitCode = 1;
            }

            if (exitWhenDone && Application.isBatchMode)
            {
                EditorApplication.Exit(exitCode);
            }
        }
    }
}
