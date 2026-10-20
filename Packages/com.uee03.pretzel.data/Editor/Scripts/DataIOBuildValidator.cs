using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Pretzel.Data.Editor
{
    internal enum IssueSeverity
    {
        Warning,
        Error
    }

    internal readonly struct BuildIssue
    {
        public BuildIssue(IssueSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }

        public IssueSeverity Severity { get; }

        public string Message { get; }

        public override string ToString() => (Severity == IssueSeverity.Error ? "Error: " : "Warning: ") + Message;
    }

    /// <summary>What the generated or hand-written PretzelDataSetup script does.</summary>
    internal enum DeliveryScript
    {
        None,
        Embedded,
        Token,
        Environment,
        Custom
    }

    /// <summary>
    /// Checks, before a build, that an encrypted setup has a key that really ships. The Editor key file and your own environment
    /// variables are not part of a player, so a setup that only works with them fails here instead of on a player's machine.
    /// </summary>
    internal static class DataIOBuildValidator
    {
        [MenuItem("Tools/Pretzel/DataIO/Check build setup")]
        private static void CheckFromMenu()
        {
            List<BuildIssue> issues = Validate();
            if (issues.Count == 0)
            {
                Debug.Log("[Pretzel.Data] Build setup looks good.");
                return;
            }

            foreach (BuildIssue issue in issues)
            {
                string message = "[Pretzel.Data] " + issue.Message;
                if (issue.Severity == IssueSeverity.Error)
                {
                    Debug.LogError(message);
                }
                else
                {
                    Debug.LogWarning(message);
                }
            }
        }

        public static List<BuildIssue> Validate()
        {
            return Validate(DataIOConfigAsset.Load(), ReadDeliveryScript(), ReadBundledJson());
        }

        /// <param name="config">The settings asset, or null when there is none.</param>
        /// <param name="deliveryScript">The source of PretzelDataSetup.cs, or null when it does not exist.</param>
        /// <param name="keyJson">The bundled game key JSON, or null when it does not exist.</param>
        public static List<BuildIssue> Validate(DataIOConfig config, string deliveryScript, string keyJson)
        {
            List<BuildIssue> issues = new();
            void Error(string message) => issues.Add(new BuildIssue(IssueSeverity.Error, message));
            void Warn(string message) => issues.Add(new BuildIssue(IssueSeverity.Warning, message));

            EncryptionKeys bundled = null;
            string jsonError = null;
            if (keyJson != null)
            {
                try
                {
                    bundled = GameEncryptionKeyConfig.FromJson(keyJson);
                }
                catch (Exception ex) when (ex is FormatException or Newtonsoft.Json.JsonException
                                               or System.Security.Cryptography.CryptographicException or System.Xml.XmlException)
                {
                    jsonError = ex.Message;
                }
            }

            if (config == null || config.format != DataFormat.Encrypted)
            {
                if (keyJson != null)
                {
                    Warn("A bundled key file (Resources/Pretzel/GameEncryptionKeys.json) ships with the build, but the format is not Encrypted. Delete it if you no longer encrypt.");
                }

                return issues;
            }

            EncryptionMode mode = config.encryptionMode;
            KeySource source = config.keySource;
            DeliveryScript delivery = Classify(deliveryScript);
            bool providerReady = delivery is DeliveryScript.Embedded or DeliveryScript.Token or DeliveryScript.Custom;
            bool jsonUsable = bundled != null && bundled.CanWrite(mode);
            bool jsonRead = source is KeySource.Automatic or KeySource.BundledJson;

            if (delivery == DeliveryScript.Embedded)
            {
                bool scriptHoldsAes = deliveryScript.Contains("PartA");
                if (mode == EncryptionMode.Aes && !scriptHoldsAes)
                {
                    Error("The embedded setup holds an RSA public key, but the settings use AES. Generate the setup again for AES.");
                }
                else if (mode == EncryptionMode.Rsa && scriptHoldsAes)
                {
                    Error("The embedded setup holds an AES key, but the settings use RSA. Generate the setup again for RSA.");
                }
            }

            if (jsonError != null && jsonRead)
            {
                Error("The bundled key config is invalid and would throw at startup: " + jsonError);
            }

            switch (source)
            {
                case KeySource.Provider:
                    if (delivery == DeliveryScript.None)
                    {
                        Warn("The key source is Provider, but no PretzelDataSetup script was found. Make sure your own code assigns EncryptionKeys.Provider before ProjectDataIO.Initialize.");
                    }
                    else if (delivery == DeliveryScript.Environment)
                    {
                        Error("The key source is Provider, but PretzelDataSetup only reads environment variables and registers no provider.");
                    }

                    break;
                case KeySource.Environment:
                    Warn("The key source is Environment. A player's computer has none of these variables, so this only works for servers and tools.");
                    break;
                case KeySource.BundledJson:
                    if (keyJson == null)
                    {
                        Error("The key source is BundledJson, but Resources/Pretzel/GameEncryptionKeys.json is missing. Export it on the Keys tab.");
                    }
                    else if (jsonError == null && !jsonUsable)
                    {
                        Error($"The bundled key config has no {mode} key. Export it again for {mode} on the Keys tab.");
                    }

                    break;
                default:
                    if (delivery == DeliveryScript.Environment)
                    {
                        Warn("The key comes from environment variables. A player's computer has none of these variables, so this only works for servers and tools.");
                    }
                    else if (!providerReady && !jsonUsable && jsonError == null)
                    {
                        Error("No key ships with this build. The Editor key file and your environment variables are not part of a player. "
                              + "Export the embedded setup or the bundled JSON on the Keys tab, or choose a key source that matches how your game gets its key.");
                    }

                    if (providerReady && jsonUsable)
                    {
                        Warn("Both a key provider and a bundled key JSON exist. The provider wins. Choose one key source on the Keys tab and delete the other file.");
                    }

                    break;
            }

            bool jsonShipsAes = bundled != null && bundled.HasAesKey;
            if (jsonShipsAes && !jsonRead)
            {
                Warn($"The bundled key JSON is not used (key source: {source}), but it still ships an AES key. Delete Resources/Pretzel/GameEncryptionKeys.json.");
            }

            bool shipsAes = mode == EncryptionMode.Aes
                            && ((delivery == DeliveryScript.Embedded && source is KeySource.Automatic or KeySource.Provider)
                                || (jsonShipsAes && jsonRead));
            if (shipsAes)
            {
                Warn("This build ships the AES key. A determined player can extract it, with or without IL2CPP. That stops casual save editing, not a cheater. Keep real secrets on a server.");
            }

            return issues;
        }

        private static DeliveryScript Classify(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return DeliveryScript.None;
            }

            if (source.Contains("LoadKeysAsync"))
            {
                return DeliveryScript.Token;
            }

            if (source.Contains("PartA") || source.Contains("const string PublicKey"))
            {
                return DeliveryScript.Embedded;
            }

            return source.Contains("EnvironmentPrefix") ? DeliveryScript.Environment : DeliveryScript.Custom;
        }

        private static string ReadDeliveryScript()
        {
            foreach (string guid in AssetDatabase.FindAssets("PretzelDataSetup t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path) == ShippingGenerator.ScriptName && File.Exists(path))
                {
                    return File.ReadAllText(path);
                }
            }

            return null;
        }

        private static string ReadBundledJson()
        {
            TextAsset asset = Resources.Load<TextAsset>(GameEncryptionKeyConfig.ResourcePath);
            return asset ? asset.text : null;
        }
    }

    /// <summary>Fails a build that cannot get its key. Define PRETZEL_SKIP_DATAIO_CHECK to turn it off.</summary>
    internal sealed class DataIOBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
#if !PRETZEL_SKIP_DATAIO_CHECK
            StringBuilder errors = new();
            foreach (BuildIssue issue in DataIOBuildValidator.Validate())
            {
                if (issue.Severity == IssueSeverity.Error)
                {
                    errors.AppendLine("- " + issue.Message);
                }
                else
                {
                    Debug.LogWarning("[Pretzel.Data] " + issue.Message);
                }
            }

            if (errors.Length > 0)
            {
                throw new BuildFailedException("Pretzel DataIO build check failed:\n" + errors
                    + "Fix it on the Setup and Keys tabs (Tools > Pretzel > DataIO > Check build setup), "
                    + "or define PRETZEL_SKIP_DATAIO_CHECK to skip this check.");
            }
#endif
        }
    }
}
