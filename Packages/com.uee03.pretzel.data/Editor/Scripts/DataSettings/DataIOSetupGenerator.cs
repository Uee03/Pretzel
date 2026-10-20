using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace Pretzel.Data.Editor
{
    /// <summary>Renders the ProjectDataIO setup script, and the optional examples, from the package's Templates~ folder.</summary>
    internal static class DataIOSetupGenerator
    {
        public const string ScriptName = "ProjectDataIO.cs";
        public const string GuideName = "DataIOSetup.md";
        public const string DefaultFolder = ShippingGenerator.DefaultFolder;

        public const string EncryptionDemoName = "PretzelEncryptionExample.cs";
        public const string EncryptionDemoEditorName = "PretzelEncryptionExampleEditor.cs";

        private static readonly string[] SaveExampleFiles = { "ProjectSaveData.cs", "ProjectSaveService.cs", "ProjectSaveExample.cs" };

        public static string ScriptPath(string folder) => folder + "/" + ScriptName;

        public static string RenderPlain(DataFormat format)
        {
            if (format == DataFormat.Encrypted)
            {
                throw new ArgumentException("Use RenderEncrypted for the Encrypted format.", nameof(format));
            }

            return ReadTemplate("ProjectDataIO.Plain.cs.txt").Replace("@@FORMAT@@", format.ToString());
        }

        public static string RenderEncrypted(EncryptionMode mode, KeySource keySource)
        {
            bool rsa = mode == EncryptionMode.Rsa;
            string usage = rsa
                ? "DataIO.Save encrypts with the public key, but this game cannot read the files back: only the private key on your server can.\n"
                  + "// Send the files to your server and read them there with CreateServerStore."
                : "DataIO.Save(\"profile\", profile) and DataIO.Load<Profile>(\"profile\") encrypt and decrypt with the same key.";

            string text = KeepRegion(ReadTemplate("ProjectDataIO.Encrypted.cs.txt"), rsa);
            return text
                .Replace("@@MODE_TEXT@@", rsa ? "RSA: the game writes, a server reads" : "AES")
                .Replace("@@USAGE@@", usage)
                .Replace("@@MODE@@", mode.ToString())
                .Replace("@@KEY_SOURCE@@", keySource.ToString());
        }

        /// <summary>True when writing <paramref name="source"/> would replace a different file, for example one you edited.</summary>
        public static bool WouldReplace(string folder, string source)
        {
            string path = ScriptPath(folder);
            return File.Exists(path) && !string.Equals(File.ReadAllText(path), source, StringComparison.Ordinal);
        }

        /// <summary>Writes ProjectDataIO.cs. Refuses when another copy of the class exists elsewhere in the project.</summary>
        public static string Write(string folder, string source)
        {
            ShippingGenerator.ValidateFolder(folder);
            string path = ScriptPath(folder);
            ShippingGenerator.EnsureNoOtherScriptCopy(ScriptName, path);
            Directory.CreateDirectory(folder);
            File.WriteAllText(path, source, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
            return path;
        }

        /// <summary>
        /// Adds the optional examples next to ProjectDataIO.cs and returns what it created. Existing files are never replaced,
        /// so edits survive. The encryption text demo is only added when <paramref name="includeEncryptionDemo"/> is set.
        /// </summary>
        public static List<string> CreateExamples(string folder, bool includeEncryptionDemo)
        {
            ShippingGenerator.ValidateFolder(folder);
            if (!File.Exists(ScriptPath(folder)))
            {
                throw new InvalidOperationException("Generate the setup code first. The examples call ProjectDataIO.");
            }

            List<(string Template, string Destination)> plan = new();
            foreach (string name in SaveExampleFiles)
            {
                plan.Add(("Examples/" + name + ".txt", folder + "/" + name));
            }

            string demo = folder + "/" + EncryptionDemoName;
            bool demoIsNew = includeEncryptionDemo && !File.Exists(demo);
            if (includeEncryptionDemo)
            {
                plan.Add(("Examples/" + EncryptionDemoName + ".txt", demo));
                if (demoIsNew)
                {
                    plan.Add(("Examples/" + EncryptionDemoEditorName + ".txt", folder + "/Editor/" + EncryptionDemoEditorName));
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[Pretzel.Data] The existing encryption example was preserved, so its inspector was not added. "
                        + "Back up the example outside Assets, remove it, and generate the examples again to get the new version.");
                }
            }

            foreach ((string _, string destination) in plan)
            {
                if (!File.Exists(destination))
                {
                    ShippingGenerator.EnsureNoOtherScriptCopy(Path.GetFileName(destination), destination);
                }
            }

            List<string> created = new();
            foreach ((string template, string destination) in plan)
            {
                if (Copy(template, destination))
                {
                    created.Add(destination);
                }
            }

            string guide = ShippingGenerator.DocumentPath(GuideName);
            string guideDestination = folder + "/" + GuideName;
            if (guide != null && File.Exists(guide) && !File.Exists(guideDestination))
            {
                File.Copy(guide, guideDestination);
                created.Add(guideDestination);
            }

            foreach (string path in created)
            {
                AssetDatabase.ImportAsset(path);
            }

            return created;
        }

        private static bool Copy(string template, string destination)
        {
            if (File.Exists(destination))
            {
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.WriteAllText(destination, ReadTemplate(template), new UTF8Encoding(false));
            return true;
        }

        private static string ReadTemplate(string relativePath)
        {
            string path = ShippingGenerator.TemplatePath(relativePath);
            if (path == null || !File.Exists(path))
            {
                throw new FileNotFoundException("A DataIO setup template is missing from the Pretzel Data package.", path);
            }

            return File.ReadAllText(path);
        }

        /// <summary>Drops the //@@IF_RSA ... //@@END_IF lines unless <paramref name="keep"/> is set, and removes the marker lines.</summary>
        private static string KeepRegion(string template, bool keep)
        {
            StringBuilder result = new();
            bool inside = false;
            foreach (string line in template.Split('\n'))
            {
                string marker = line.Trim();
                if (marker == "//@@IF_RSA")
                {
                    inside = true;
                    continue;
                }

                if (marker == "//@@END_IF")
                {
                    inside = false;
                    continue;
                }

                if (!inside || keep)
                {
                    result.Append(line).Append('\n');
                }
            }

            // Split adds one extra empty segment after the final newline.
            return result.ToString(0, result.Length - 1);
        }
    }
}
