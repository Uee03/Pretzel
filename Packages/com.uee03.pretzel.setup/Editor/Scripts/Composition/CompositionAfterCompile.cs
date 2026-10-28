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

        static CompositionAfterCompile()
        {
            if (SessionState.GetBool(PendingKey, false))
            {
                EditorApplication.delayCall += ApplyPending;
            }
        }

        public static event Action<IReadOnlyList<string>> Applied;

        public static void Request() => SessionState.SetBool(PendingKey, true);

        private static void ApplyPending()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += ApplyPending;
                return;
            }

            SessionState.EraseBool(PendingKey);
            try
            {
                IReadOnlyList<string> log = CompositionAssets.Apply(PretzelSettings.Project.composition);
                Debug.Log("[Pretzel] VContainer setup:\n" + string.Join("\n", log));
                Applied?.Invoke(log);
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogWarning("[Pretzel] VContainer setup did not finish: " + ex.Message);
            }
        }
    }
}
