using UnityEditor;

namespace Pretzel.Samples.UI.CustomInspectors.Editors
{
    internal static class WeaponConfigMenu
    {
        [MenuItem(SampleObjects.MenuRoot + "Create/Weapon Config")]
        private static void Create()
        {
            SampleObjects.CreateComponent<WeaponConfig>("Weapon Config");
        }
    }
}
