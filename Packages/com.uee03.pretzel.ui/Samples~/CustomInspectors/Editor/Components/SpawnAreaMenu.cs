using UnityEditor;

namespace Pretzel.Samples.UI.CustomInspectors.Editors
{
    internal static class SpawnAreaMenu
    {
        [MenuItem(SampleObjects.MenuRoot + "Create/Spawn Area")]
        private static void Create()
        {
            SampleObjects.CreateComponent<SpawnArea>("Spawn Area");
        }
    }
}
