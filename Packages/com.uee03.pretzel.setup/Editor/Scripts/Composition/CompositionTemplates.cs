using System;
using System.IO;
using UnityEditor.PackageManager;

namespace Pretzel.Setup.Composition
{
    /// <summary>Reads the ready-to-use source templates in <c>Editor/Templates/Composition</c> and fills in the namespace.</summary>
    internal static class CompositionTemplates
    {
        private const string Folder = "Editor/Templates/Composition";
        private const string NamespaceToken = "#NAMESPACE#";

        public static string Render(string name, CompositionSettings settings)
        {
            PackageInfo package = PackageInfo.FindForAssembly(typeof(CompositionTemplates).Assembly);
            string path = package == null ? null : Path.Combine(package.resolvedPath, Folder, name + ".cs.txt");
            if (path == null || !File.Exists(path))
            {
                throw new InvalidOperationException($"The Pretzel Setup template {name}.cs.txt is missing. Reinstall the package.");
            }

            return File.ReadAllText(path)
                .Replace("\r\n", "\n")
                .Replace(NamespaceToken, settings.namespaceName);
        }
    }
}
