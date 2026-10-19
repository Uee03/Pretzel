using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Pretzel.UI.UGUI.Editor
{
    [CustomEditor(typeof(UIFader))]
    public class UIFaderEditor : UnityEditor.Editor
    {
        private const string StylePath = "Packages/com.uee03.pretzel.ui.ugui/Editor/Styles/UIFaderEditor.uss";
        private const string HiddenClass = "pretzel-ui-fader__field--hidden";

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            root.AddToClassList("pretzel-ui-fader");
            StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (sheet)
            {
                root.styleSheets.Add(sheet);
            }

            PropertyField script = new(serializedObject.FindProperty("m_Script"));
            script.SetEnabled(false);
            root.Add(script);

            SerializedProperty useCanvasGroup = serializedObject.FindProperty("_useCanvasGroup");
            VisualElement canvasGroup = Field("_canvasGroup");
            VisualElement uiElements = Field("_uiElements");
            VisualElement targets = Group("Fader Properties", Field("_useCanvasGroup"), canvasGroup, uiElements);

            SerializedProperty fadeOnStart = serializedObject.FindProperty("_fadeOnStart");
            VisualElement fadeOnStartType = Field("_fadeOnStartType");
            VisualElement onStart = Group("Fade On Start", Field("_fadeOnStart"), fadeOnStartType);

            SerializedProperty useEaseType = serializedObject.FindProperty("_useEaseType");
            VisualElement fadeInEase = Field("_fadeInEaseType");
            VisualElement fadeOutEase = Field("_fadeOutEaseType");
            VisualElement fadeInCurve = Field("_fadeInAnimationCurve");
            VisualElement fadeOutCurve = Field("_fadeOutAnimationCurve");
            VisualElement fade = Group("Fade Properties", Field("_useEaseType"), fadeInEase, fadeOutEase, fadeInCurve, fadeOutCurve,
                Field("_fadeInDuration"), Field("_fadeOutDuration"));

            root.Add(targets);
            root.Add(onStart);
            root.Add(fade);

            void Refresh()
            {
                bool group = useCanvasGroup.boolValue;
                canvasGroup.EnableInClassList(HiddenClass, !group);
                uiElements.EnableInClassList(HiddenClass, group);
                fadeOnStartType.EnableInClassList(HiddenClass, !fadeOnStart.boolValue);

                bool ease = useEaseType.boolValue;
                fadeInEase.EnableInClassList(HiddenClass, !ease);
                fadeOutEase.EnableInClassList(HiddenClass, !ease);
                fadeInCurve.EnableInClassList(HiddenClass, ease);
                fadeOutCurve.EnableInClassList(HiddenClass, ease);
            }

            Refresh();
            root.TrackPropertyValue(useCanvasGroup, _ => Refresh());
            root.TrackPropertyValue(fadeOnStart, _ => Refresh());
            root.TrackPropertyValue(useEaseType, _ => Refresh());
            return root;
        }

        private PropertyField Field(string propertyName)
        {
            return new PropertyField(serializedObject.FindProperty(propertyName));
        }

        private static VisualElement Group(string title, params VisualElement[] children)
        {
            VisualElement group = new();
            group.AddToClassList("pretzel-ui-fader__group");

            Label label = new(title);
            label.AddToClassList("pretzel-ui-fader__title");
            group.Add(label);

            foreach (VisualElement child in children)
            {
                group.Add(child);
            }

            return group;
        }
    }
}
