using UnityEditor;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.EditorWindows
{
    /// <summary>A button cell that pings the asset of the row it is currently bound to. Created once per visible row and reused.</summary>
    public sealed class AssetPingButton : Button
    {
        public AssetPingButton()
        {
            text = "Ping";
            AddToClassList("pretzel-tables__ping");
            clicked += OnClicked;
        }

        public AssetRow Row { get; set; }

        private void OnClicked()
        {
            if (Row == null)
            {
                return;
            }

            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(Row.Path);
            if (asset)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }
    }
}
