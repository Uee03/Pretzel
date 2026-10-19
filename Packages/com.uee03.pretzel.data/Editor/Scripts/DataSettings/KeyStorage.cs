using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Pretzel.Data.Editor
{
    internal enum KeyScope
    {
        All,
        PublicOnly
    }

    internal enum EnvironmentStatus
    {
        NotSet,
        SessionOnly,
        Saved
    }

    internal enum GitIgnoreState
    {
        Unknown,
        Ignored,
        NotIgnored
    }

    /// <summary>Stores keys as user environment variables or in the per-project key file.</summary>
    internal static class KeyStorage
    {
        /// <summary>Only Windows can write user-level variables from .NET. Elsewhere they last until the Editor closes.</summary>
        public static bool CanPersistEnvironment => Application.platform == RuntimePlatform.WindowsEditor;

        #region ENVIRONMENT

        public static List<(string Name, string Value)> Variables(string prefix, EncryptionKeys keys, KeyScope scope)
        {
            List<(string, string)> variables = new();
            if (scope == KeyScope.All && keys.HasAesKey)
            {
                variables.Add((EncryptionKeys.AesVariable(prefix), EncryptionKeys.EncodeAesKey(keys.AesKey)));
            }

            if (keys.HasPublicKey)
            {
                variables.Add((EncryptionKeys.PublicVariable(prefix), EncryptionKeys.EncodeXml(keys.RsaPublicKey)));
            }

            if (scope == KeyScope.All && keys.HasPrivateKey)
            {
                variables.Add((EncryptionKeys.PrivateVariable(prefix), EncryptionKeys.EncodeXml(keys.RsaPrivateKey)));
            }

            return variables;
        }

        public static void SaveEnvironment(string prefix, EncryptionKeys keys, KeyScope scope)
        {
            foreach ((string name, string value) in Variables(prefix, keys, scope))
            {
                Environment.SetEnvironmentVariable(name, value);
                if (CanPersistEnvironment)
                {
                    Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.User);
                }
            }
        }

        public static void ClearEnvironment(string prefix)
        {
            foreach (string name in AllNames(prefix))
            {
                Environment.SetEnvironmentVariable(name, null);
                if (CanPersistEnvironment)
                {
                    Environment.SetEnvironmentVariable(name, null, EnvironmentVariableTarget.User);
                }
            }
        }

        public static EnvironmentStatus StatusOf(string name)
        {
            if (CanPersistEnvironment && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User)))
            {
                return EnvironmentStatus.Saved;
            }

            return string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)) ? EnvironmentStatus.NotSet : EnvironmentStatus.SessionOnly;
        }

        /// <summary>Commands the user can run themselves, for a shell profile or a build server.</summary>
        public static string ShellCommands(string prefix, EncryptionKeys keys, KeyScope scope)
        {
            StringBuilder text = new();
            bool windows = Application.platform == RuntimePlatform.WindowsEditor;
            foreach ((string name, string value) in Variables(prefix, keys, scope))
            {
                text.AppendLine(windows
                    ? $"[Environment]::SetEnvironmentVariable('{name}', '{value}', 'User')"
                    : $"export {name}='{value}'");
            }

            return text.ToString();
        }

        private static IEnumerable<string> AllNames(string prefix)
        {
            yield return EncryptionKeys.AesVariable(prefix);
            yield return EncryptionKeys.PublicVariable(prefix);
            yield return EncryptionKeys.PrivateVariable(prefix);
        }

        #endregion

        #region KEY FILE

        /// <summary>Writes the keys into the project key file. Keys that are not part of <paramref name="scope"/> are left as they were.</summary>
        public static void SaveFile(EncryptionKeys keys, KeyScope scope)
        {
            EncryptionKeys merged = EncryptionKeyFile.Load() ?? new EncryptionKeys();
            if (scope == KeyScope.All)
            {
                merged.AesKey = keys.HasAesKey ? keys.AesKey : merged.AesKey;
                merged.RsaPrivateKey = keys.HasPrivateKey ? keys.RsaPrivateKey : merged.RsaPrivateKey;
            }

            if (keys.HasPublicKey)
            {
                merged.RsaPublicKey = keys.RsaPublicKey;
            }

            EncryptionKeyFile.Save(merged);
        }

        public static GitIgnoreState UserSettingsIgnoreState()
        {
            string folder = Directory.GetParent(Application.dataPath).FullName;
            bool foundFile = false;
            for (int level = 0; level < 4 && folder != null; level++)
            {
                string path = Path.Combine(folder, ".gitignore");
                if (File.Exists(path))
                {
                    foundFile = true;
                    foreach (string raw in File.ReadAllLines(path))
                    {
                        string line = raw.Trim();
                        if (line.Length > 0 && line[0] != '#' && line[0] != '!' && line.Contains("serSettings"))
                        {
                            return GitIgnoreState.Ignored;
                        }
                    }
                }

                folder = Directory.GetParent(folder)?.FullName;
            }

            return foundFile ? GitIgnoreState.NotIgnored : GitIgnoreState.Unknown;
        }

        #endregion
    }
}
