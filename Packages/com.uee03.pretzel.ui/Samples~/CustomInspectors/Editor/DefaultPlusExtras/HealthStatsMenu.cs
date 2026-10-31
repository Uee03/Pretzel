using UnityEditor;

namespace Pretzel.Samples.UI.CustomInspectors.Editors
{
    internal static class HealthStatsMenu
    {
        [MenuItem(SampleObjects.MenuRoot + "Create/Health Stats")]
        private static void Create()
        {
            SampleObjects.CreateComponent<HealthStats>("Health Stats");
        }
    }
}
