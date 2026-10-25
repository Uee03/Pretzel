using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;

namespace Pretzel.Data.Editor
{
    internal enum KeyDelivery
    {
        Environment,
        Embedded,
        Token
    }

    /// <summary>
    /// Writes the small PretzelDataSetup script that gets a key to a shipped build. It only delivers the key:
    /// the store is created by the generated ProjectDataIO (Setup tab).
    /// </summary>
    internal static class ShippingGenerator
    {
        public const string ScriptName = "PretzelDataSetup.cs";
        public const string DefaultFolder = "Assets/Pretzel/DataIO";

        /// <summary>Text in the generated token stub until the request is implemented. The build check looks for it.</summary>
        public const string TokenStubMarker = "Implement PretzelDataSetup.RequestKeyAsync";

        public static string ScriptPath(string folder) => folder + "/" + ScriptName;

        /// <summary>
        /// True when <paramref name="folder"/> has no setup script, or one that only holds generated content.
        /// A script with your own code (an implemented token request or a custom script) needs confirmation first.
        /// </summary>
        public static bool IsSafeToReplace(string folder)
        {
            string path = ScriptPath(folder);
            if (!File.Exists(path))
            {
                return true;
            }

            string existing = File.ReadAllText(path);
            return DataIOBuildValidator.Classify(existing) switch
            {
                DeliveryScript.Embedded or DeliveryScript.Environment => true,
                DeliveryScript.Token => existing.Contains(TokenStubMarker),
                _ => false
            };
        }

        public static bool ConfirmReplace(string folder)
        {
            return IsSafeToReplace(folder) || PanelUi.Confirm("Replace PretzelDataSetup.cs?",
                $"{ScriptPath(folder)} contains your own code, for example an implemented key request. Replacing it loses that code. Replace it?",
                "Replace", "Cancel");
        }

        public static string GuideSourcePath() => DocumentPath("EncryptionKeyDelivery.md");

        public static string DocumentPath(string fileName) => PackagePath("Documentation~", fileName);

        public static string TemplatePath(string relativePath) => PackagePath("Templates~", relativePath);

        private static string PackagePath(string folder, string relativePath)
        {
            UnityEditor.PackageManager.PackageInfo info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(DataIOConfig).Assembly);
            return info == null ? null : Path.Combine(info.resolvedPath, folder, relativePath);
        }

        /// <summary>Throws when <paramref name="folder"/> is not a plain folder inside Assets.</summary>
        public static void ValidateFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)
                || (folder != "Assets" && !folder.StartsWith("Assets/", StringComparison.Ordinal))
                || folder.Contains(".."))
            {
                throw new ArgumentException("The output folder must be inside Assets and cannot contain '..'.", nameof(folder));
            }
        }

        /// <summary>Two copies of one script define the same class twice. Refuse to create the second.</summary>
        public static void EnsureNoOtherScriptCopy(string name, string destination)
        {
            foreach (string guid in AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(name) + " t:MonoScript"))
            {
                string existing = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(existing) == name && !string.Equals(existing, destination, StringComparison.OrdinalIgnoreCase))
                {
                    throw new IOException($"'{name}' already exists at '{existing}'. Move that script and its .meta to "
                        + $"'{destination}' first, or use the existing folder, to avoid duplicate classes.");
                }
            }
        }

        /// <summary>Returns null on success, otherwise the reason it could not be written.</summary>
        /// <param name="replaceConfirmed">The caller already asked before replacing a script that holds your own code.</param>
        public static string Generate(string folder, KeyDelivery delivery, DataIOConfig config, EncryptionKeys keys, out string scriptPath, bool replaceConfirmed = false)
        {
            scriptPath = null;
            if (config.encryptionMode != EncryptionMode.Aes)
            {
                return "Only AES encryption is supported.";
            }

            if (folder != "Assets" && !folder.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return "The folder must be inside Assets, for example Assets/Pretzel/DataIO.";
            }

            if (folder.Contains(".."))
            {
                return "The folder cannot contain '..'.";
            }

            string source;
            switch (delivery)
            {
                case KeyDelivery.Environment:
                    if (!DataIOConfigAsset.IsValidPrefix(config.environmentPrefix))
                    {
                        return "The environment variable prefix may only contain letters, digits and underscores, and cannot start with a digit. Fix it on the Keys tab.";
                    }

                    source = EnvironmentScript(config);
                    break;
                case KeyDelivery.Embedded:
                    source = EmbeddedScript(config, keys, out string error);
                    if (error != null)
                    {
                        return error;
                    }

                    break;
                default:
                    source = TokenScript();
                    break;
            }

            string path = ScriptPath(folder);
            EnsureNoOtherScriptCopy(ScriptName, path);
            if (!replaceConfirmed && !ConfirmReplace(folder))
            {
                return $"Kept the existing {path}. Nothing was changed.";
            }

            Directory.CreateDirectory(folder);
            File.WriteAllText(path, source, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
            scriptPath = path;
            return null;
        }

        private static string Header(string what)
        {
            return "// Generated by Pretzel > Data Format. " + what + "\n"
                   + "// This delivers the key only. Create the store with ProjectDataIO.Initialize() (Setup tab) after this has run.\n"
                   + "// Delete this file to remove it. Delivery options: Documentation~/EncryptionKeyDelivery.md in the Pretzel Data package.\n"
                   + "using System;\nusing System.Threading;\nusing Pretzel.Data;\nusing UnityEngine;\n\n";
        }

        private static string EnvironmentScript(DataIOConfig config)
        {
            return Header("The game reads its keys from environment variables.")
                   + "public static class PretzelDataSetup\n{\n"
                   + $"    private const string EnvironmentPrefix = \"{config.environmentPrefix}\";\n\n"
                   + "    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]\n"
                   + "    private static void CheckKeys()\n    {\n"
                   + "        if (EncryptionKeys.Resolve(EnvironmentPrefix, KeySource.Environment, out _) == null)\n        {\n"
                   + "            Debug.LogError($\"No AES key found. Set {EncryptionKeys.AesVariable(EnvironmentPrefix)}.\");\n"
                   + "        }\n    }\n}\n";
        }

        private static string EmbeddedScript(DataIOConfig config, EncryptionKeys keys, out string error)
        {
            error = null;
            StringBuilder code = new(Header("Contains the AES key, obfuscated. Keep this file (and its .meta) out of public repositories."));
            code.Append("public static class PretzelDataSetup\n{\n");

            if (config.encryptionMode == EncryptionMode.Aes)
            {
                if (!keys.HasAesKey)
                {
                    error = "There is no AES key yet. Generate one on the Encryption tab first.";
                    return null;
                }

                code.Append("    // Each key is split in two arrays and combined at runtime. This only defeats a plain search of the build:\n");
                code.Append("    // a determined player can still recover it, with or without IL2CPP.\n");
                Mask(keys.AesKey, out byte[] masked, out byte[] mask);
                code.Append("    private static readonly byte[] PartA = " + ByteArray(masked) + ";\n");
                code.Append("    private static readonly byte[] PartB = " + ByteArray(mask) + ";\n");

                bool history = keys.PreviousAesKeys.Count > 0;
                if (history)
                {
                    code.Append("\n    // Earlier keys, so saves written before a key change still open.\n");
                    StringBuilder partsA = new();
                    StringBuilder partsB = new();
                    foreach (byte[] previous in keys.PreviousAesKeys)
                    {
                        Mask(previous, out byte[] oldMasked, out byte[] oldMask);
                        partsA.Append("        ").Append(ByteArray(oldMasked)).Append(",\n");
                        partsB.Append("        ").Append(ByteArray(oldMask)).Append(",\n");
                    }

                    code.Append("    private static readonly byte[][] OldPartsA =\n    {\n" + partsA + "    };\n");
                    code.Append("    private static readonly byte[][] OldPartsB =\n    {\n" + partsB + "    };\n");
                }

                code.Append("\n    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]\n");
                code.Append("    private static void Register()\n    {\n        EncryptionKeys.Provider = BuildKeys;\n    }\n\n");
                code.Append("    private static EncryptionKeys BuildKeys()\n    {\n");
                code.Append("        EncryptionKeys keys = new() { AesKey = Combine(PartA, PartB) };\n");
                if (history)
                {
                    code.Append("        for (int i = 0; i < OldPartsA.Length; i++)\n        {\n            keys.PreviousAesKeys.Add(Combine(OldPartsA[i], OldPartsB[i]));\n        }\n\n");
                }

                code.Append("        return keys;\n    }\n\n");
                code.Append("    private static byte[] Combine(byte[] a, byte[] b)\n    {\n");
                code.Append("        byte[] key = new byte[a.Length];\n");
                code.Append("        for (int i = 0; i < key.Length; i++)\n        {\n            key[i] = (byte)(a[i] ^ b[i]);\n        }\n\n");
                code.Append("        return key;\n    }\n}\n");
            }
            return code.ToString();
        }

        private static string TokenScript()
        {
            return Header("The game fetches its AES key from your server with an access token.")
                   + "public static class PretzelDataSetup\n{\n"
                   + "    // After sign-in, with the config's key source set to Provider (Keys tab):\n"
                   + "    //   if (!await PretzelDataSetup.LoadKeysAsync(accessToken, destroyCancellationToken))\n"
                   + "    //   { /* Offer a retry. Do not create or overwrite saves without a key. */ return; }\n"
                   + "    //   ProjectDataIO.Initialize();\n"
                   + "    // On sign-out: ProjectDataIO.Shutdown(); PretzelDataSetup.ClearKeys();\n\n"
                   + "    /// <summary>Call once after sign-in, before ProjectDataIO.Initialize. Returns false, and logs why, when no valid key arrived.</summary>\n"
                   + "    public static async Awaitable<bool> LoadKeysAsync(string accessToken, CancellationToken cancellationToken = default)\n    {\n"
                   + "        string encodedKey;\n"
                   + "        try\n        {\n"
                   + "            encodedKey = await RequestKeyAsync(accessToken, cancellationToken);\n"
                   + "        }\n"
                   + "        catch (Exception ex) when (ex is not OperationCanceledException)\n        {\n"
                   + "            Debug.LogException(ex);\n"
                   + "            return false;\n        }\n\n"
                   + "        byte[] key = EncryptionKeys.DecodeAesKey(encodedKey);\n"
                   + "        if (key == null || key.Length != EncryptionKeys.AesKeySize)\n        {\n"
                   + "            Debug.LogError(\"[PretzelDataSetup] The key service did not return a Base64-encoded 32 byte AES key.\");\n"
                   + "            return false;\n        }\n\n"
                   + "        // The key stays in memory only. Do not write it to disk or PlayerPrefs.\n"
                   + "        EncryptionKeys keys = new() { AesKey = key };\n"
                   + "        EncryptionKeys.Provider = () => keys;\n"
                   + "        return true;\n    }\n\n"
                   + "    /// <summary>Forgets the fetched key, for example on sign-out. Call ProjectDataIO.Shutdown() first.</summary>\n"
                   + "    public static void ClearKeys()\n    {\n"
                   + "        EncryptionKeys.Provider = null;\n    }\n\n"
                   + "    // Replace this body with a call to your key service over HTTPS, e.g. Pretzel.Api.ApiClient GET /save-key with \"Authorization: Bearer <token>\".\n"
                   + "    // The server must validate the token and return the key as Base64. Never log the token or the key. The build check fails until this is done.\n"
                   + "    private static async Awaitable<string> RequestKeyAsync(string accessToken, CancellationToken cancellationToken)\n    {\n"
                   + "        await Awaitable.NextFrameAsync(cancellationToken);\n"
                   + $"        throw new NotImplementedException(\"{TokenStubMarker} to fetch the key from your server.\");\n    }}\n}}\n";
        }

        private static void Mask(byte[] key, out byte[] masked, out byte[] mask)
        {
            mask = new byte[key.Length];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create())
            {
                random.GetBytes(mask);
            }

            masked = new byte[key.Length];
            for (int i = 0; i < key.Length; i++)
            {
                masked[i] = (byte)(key[i] ^ mask[i]);
            }
        }

        private static string ByteArray(byte[] bytes)
        {
            StringBuilder text = new("new byte[] { ");
            for (int i = 0; i < bytes.Length; i++)
            {
                text.Append("0x").Append(bytes[i].ToString("X2"));
                if (i < bytes.Length - 1)
                {
                    text.Append(", ");
                }
            }

            return text.Append(" }").ToString();
        }
    }
}
