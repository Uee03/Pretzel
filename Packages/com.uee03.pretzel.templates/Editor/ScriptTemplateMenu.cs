using System.IO;
using UnityEditor;

namespace Pretzel.Templates
{
    /// <summary>Adds Assets > Create > Pretzel entries that create scripts from the templates in <c>Templates~</c>.</summary>
    internal static class ScriptTemplateMenu
    {
        private const string MenuRoot = "Assets/Create/Pretzel/";
        private const int MenuPriority = 80;

        [MenuItem(MenuRoot + "C# Script", false, MenuPriority)]
        private static void CreateScript() => Create("PretzelScript.cs.txt", "NewScript.cs");

        [MenuItem(MenuRoot + "C# UI Controller", false, MenuPriority + 1)]
        private static void CreateUIController() => Create("PretzelUIController.cs.txt", "NewUIController.cs");

        [MenuItem(MenuRoot + "C# Singleton", false, MenuPriority + 2)]
        private static void CreateSingleton() => Create("PretzelSingleton.cs.txt", "NewSingleton.cs");

        [MenuItem(MenuRoot + "UI Toolkit Theme", false, MenuPriority + 20)]
        private static void CreateTheme() => Create("PretzelTheme.tss.txt", "NewTheme.tss");

        private static void Create(string template, string defaultName)
        {
            UnityEditor.PackageManager.PackageInfo package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ScriptTemplateMenu).Assembly);
            string path = Path.Combine(package.resolvedPath, "Templates~", template);
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(path, defaultName);
        }
    }
}
