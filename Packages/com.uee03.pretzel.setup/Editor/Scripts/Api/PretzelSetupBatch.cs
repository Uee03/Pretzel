using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pretzel.Setup.Composition;
using Pretzel.Setup.Settings;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Pretzel.Setup
{
    /// <summary>
    /// Command-line entry points for <c>Unity -batchmode -projectPath ... -executeMethod</c>, so a new project can be bootstrapped
    /// without opening the Project Setup window. Do not pass <c>-quit</c> to <see cref="Composition"/>: it quits itself, with exit code 1
    /// on failure, once the generated code has compiled and the assets exist.
    /// <list type="bullet">
    /// <item><c>Pretzel.Setup.PretzelSetupBatch.Folders</c>: creates the active preset's folders.</item>
    /// <item><c>Pretzel.Setup.PretzelSetupBatch.Composition</c>: generates the VContainer composition root and its assets.</item>
    /// </list>
    /// <c>Composition</c> uses the saved project settings, and these arguments override and save them:
    /// <c>-pretzelNamespace</c>, <c>-pretzelScope</c>, <c>-pretzelServices a,b,c</c> (ids of the Pretzel services, or <c>none</c>)
    /// and <c>-pretzelStarter true|false</c>.
    /// </summary>
    public static class PretzelSetupBatch
    {
        public const string NamespaceArgument = "-pretzelNamespace";
        public const string ScopeArgument = "-pretzelScope";
        public const string ServicesArgument = "-pretzelServices";
        public const string StarterArgument = "-pretzelStarter";

        public static void Folders()
        {
            Debug.Log($"[Pretzel] Folder preset '{PretzelSettings.Project.ActivePreset.name}': created {PretzelSetup.CreateFolders()} folder(s).");
            Exit(0);
        }

        public static void Composition()
        {
            try
            {
                CompositionSettings settings = PretzelSettings.Project.composition;
                if (ApplyOverrides(Environment.GetCommandLineArgs(), settings))
                {
                    settings.Normalize();
                    PretzelSettings.SaveProject();
                }

                // Compile errors stop the reload that finishes the setup, so without this the run would wait forever.
                CompilationPipeline.assemblyCompilationFinished += ExitOnCompileError;
                CompositionResult result = PretzelSetup.GenerateComposition(exitWhenAssetsAreApplied: true);
                if (result.AssetsPending)
                {
                    Debug.Log($"[Pretzel] Wrote {result.WrittenFiles.Count} file(s). The assets are created after compilation, then the Editor quits.");
                    return;
                }

                Debug.Log("[Pretzel] VContainer setup:\n" + string.Join("\n", result.AssetLog));
                Exit(0);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is IOException)
            {
                Debug.LogError("[Pretzel] VContainer setup failed: " + ex.Message);
                Exit(1);
            }
        }

        /// <summary>Applies the <c>-pretzel*</c> arguments to the settings. Returns true when anything was set.</summary>
        /// <exception cref="ArgumentException">A value is missing or not valid.</exception>
        internal static bool ApplyOverrides(IReadOnlyList<string> args, CompositionSettings settings)
        {
            bool changed = false;
            string value;
            if (TryRead(args, NamespaceArgument, out value))
            {
                settings.namespaceName = value;
                changed = true;
            }

            if (TryRead(args, ScopeArgument, out value))
            {
                settings.scopeName = value;
                changed = true;
            }

            if (TryRead(args, ServicesArgument, out value))
            {
                settings.pretzelServices = ParseServices(value);
                changed = true;
            }

            if (TryRead(args, StarterArgument, out value))
            {
                settings.starterServices = bool.TryParse(value, out bool starter)
                    ? starter
                    : throw new ArgumentException($"{StarterArgument} expects true or false, not '{value}'.");
                changed = true;
            }

            return changed;
        }

        private static List<string> ParseServices(string value)
        {
            if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase))
            {
                return new List<string>();
            }

            List<string> ids = new();
            foreach (string id in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(part => part.Trim()))
            {
                PretzelServiceOption option = PretzelServiceCatalog.Find(id)
                                              ?? throw new ArgumentException($"'{id}' is not a Pretzel service. Known ids: {string.Join(", ", PretzelServiceCatalog.All.Select(o => o.Id))}.");
                ids.Add(option.Id);
            }

            return ids;
        }

        private static bool TryRead(IReadOnlyList<string> args, string name, out string value)
        {
            for (int i = 0; i < args.Count; i++)
            {
                if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                value = i + 1 < args.Count && !args[i + 1].StartsWith("-", StringComparison.Ordinal)
                    ? args[i + 1]
                    : throw new ArgumentException($"{name} needs a value.");
                return true;
            }

            value = null;
            return false;
        }

        private static void ExitOnCompileError(string assemblyPath, CompilerMessage[] messages)
        {
            if (messages.Any(message => message.type == CompilerMessageType.Error))
            {
                Debug.LogError($"[Pretzel] The generated code did not compile ({Path.GetFileName(assemblyPath)}). Setup stopped.");
                Exit(1);
            }
        }

        private static void Exit(int code)
        {
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(code);
            }
        }
    }
}
