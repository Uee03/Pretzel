using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;

namespace Pretzel.Setup.Composition
{
    /// <summary>What the VContainer tab generates. Stored with the other project settings and committed.</summary>
    [Serializable]
    public sealed class CompositionSettings
    {
        public string namespaceName = string.Empty;
        public string scopeName = "Game";
        public string codeFolder = "Assets/_Scripts/Runtime/Composition";
        public string assetFolder = "Assets/_Project/Settings/Composition";
        public bool createFlow = true;
        public string apiBaseUrl = string.Empty;
        public List<string> pretzelServices = new();
        public List<ServiceDefinition> services = new();

        [JsonIgnore]
        public string InstallerName => scopeName + "Installer";

        [JsonIgnore]
        public string FlowName => scopeName + "Flow";

        [JsonIgnore]
        public string ScopePrefabName => scopeName + "LifetimeScope";

        public void Normalize()
        {
            pretzelServices ??= new List<string>();
            services ??= new List<ServiceDefinition>();
            services.RemoveAll(s => s == null);
            apiBaseUrl ??= string.Empty;
            scopeName = string.IsNullOrWhiteSpace(scopeName) ? "Game" : scopeName.Trim();
            codeFolder = string.IsNullOrWhiteSpace(codeFolder) ? "Assets/_Scripts/Runtime/Composition" : codeFolder.Trim();
            assetFolder = string.IsNullOrWhiteSpace(assetFolder) ? "Assets/_Project/Settings/Composition" : assetFolder.Trim();
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                namespaceName = DefaultNamespace();
            }
        }

        private static string DefaultNamespace()
        {
            StringBuilder name = new();
            foreach (char c in PlayerSettings.productName ?? string.Empty)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    name.Append(c);
                }
            }

            if (name.Length == 0)
            {
                return "Game";
            }

            return char.IsDigit(name[0]) ? "_" + name : name.ToString();
        }
    }
}
