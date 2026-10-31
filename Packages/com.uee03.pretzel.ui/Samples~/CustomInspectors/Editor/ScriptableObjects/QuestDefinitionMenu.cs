using UnityEditor;
using UnityEngine;

namespace Pretzel.Samples.UI.CustomInspectors.Editors
{
    internal static class QuestDefinitionMenu
    {
        // ProjectWindowUtil creates the asset in the selected folder and starts renaming it, like the built-in Create menu.
        [MenuItem("Assets/Create/Pretzel Samples/Quest Definition")]
        private static void CreateFromAssetsMenu()
        {
            Create();
        }

        [MenuItem(SampleObjects.MenuRoot + "Create/Quest Definition")]
        private static void CreateFromSampleMenu()
        {
            Create();
        }

        private static void Create()
        {
            QuestDefinition quest = ScriptableObject.CreateInstance<QuestDefinition>();
            quest.title = "New Quest";
            ProjectWindowUtil.CreateAsset(quest, "New Quest.asset");
        }
    }
}
