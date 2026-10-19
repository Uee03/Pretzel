#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif
using System;
using UnityEngine.UIElements;

namespace Pretzel.UI
{
    /// <summary>Factories that create a UI Toolkit element, optionally add a style sheet and classes, and parent it in one call.</summary>
    public static class UIToolkitUtility
    {
        #region CONTROLS

        public static VisualElement CreateContainer(VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            return CreateElement<VisualElement>(parent, styleSheet, classNames);
        }

        public static Button CreateButton(string label, Action onClick = null,
            VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            Button button = CreateElement<Button>(parent, styleSheet, classNames);
            button.text = label;
            if (onClick != null)
            {
                button.clicked += onClick;
            }

            return button;
        }

        public static Label CreateLabel(string text, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            Label label = CreateElement<Label>(parent, styleSheet, classNames);
            label.text = text;
            return label;
        }

        public static TextField CreateTextField(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            TextField field = CreateElement<TextField>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static IntegerField CreateIntegerField(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            IntegerField field = CreateElement<IntegerField>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static FloatField CreateFloatField(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            FloatField field = CreateElement<FloatField>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static DoubleField CreateDoubleField(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            DoubleField field = CreateElement<DoubleField>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static LongField CreateLongField(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            LongField field = CreateElement<LongField>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static UnsignedIntegerField CreateUnsignedIntegerField(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            UnsignedIntegerField field = CreateElement<UnsignedIntegerField>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static UnsignedLongField CreateUnsignedLongField(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            UnsignedLongField field = CreateElement<UnsignedLongField>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static Hash128Field CreateHash128Field(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            Hash128Field field = CreateElement<Hash128Field>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static Vector2Field CreateVector2Field(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            Vector2Field field = CreateElement<Vector2Field>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static Vector3Field CreateVector3Field(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            Vector3Field field = CreateElement<Vector3Field>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static Vector4Field CreateVector4Field(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            Vector4Field field = CreateElement<Vector4Field>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static Vector2IntField CreateVector2IntField(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            Vector2IntField field = CreateElement<Vector2IntField>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static Vector3IntField CreateVector3IntField(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            Vector3IntField field = CreateElement<Vector3IntField>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static RectField CreateRectField(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            RectField field = CreateElement<RectField>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static RectIntField CreateRectIntField(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            RectIntField field = CreateElement<RectIntField>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static BoundsField CreateBoundsField(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            BoundsField field = CreateElement<BoundsField>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static BoundsIntField CreateBoundsIntField(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            BoundsIntField field = CreateElement<BoundsIntField>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static Slider CreateSlider(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            Slider slider = CreateElement<Slider>(parent, styleSheet, classNames);
            slider.label = label;
            return slider;
        }

        public static SliderInt CreateSliderInt(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            SliderInt slider = CreateElement<SliderInt>(parent, styleSheet, classNames);
            slider.label = label;
            return slider;
        }

        public static MinMaxSlider CreateMinMaxSlider(string label, float minValue, float maxValue,
            VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            MinMaxSlider slider = CreateElement<MinMaxSlider>(parent, styleSheet, classNames);
            slider.lowLimit = minValue;
            slider.highLimit = maxValue;
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.label = label;
            return slider;
        }

        public static Toggle CreateToggle(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            Toggle toggle = CreateElement<Toggle>(parent, styleSheet, classNames);
            toggle.text = label;
            return toggle;
        }

        public static RadioButton CreateRadioButton(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            RadioButton radioButton = CreateElement<RadioButton>(parent, styleSheet, classNames);
            radioButton.text = label;
            return radioButton;
        }

        public static Foldout CreateFoldout(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            Foldout foldout = CreateElement<Foldout>(parent, styleSheet, classNames);
            foldout.text = label;
            return foldout;
        }

        public static ProgressBar CreateProgressBar(string title, float lowValue, float highValue,
            VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            ProgressBar progressBar = CreateElement<ProgressBar>(parent, styleSheet, classNames);
            progressBar.title = title;
            progressBar.lowValue = lowValue;
            progressBar.highValue = highValue;
            return progressBar;
        }

        // EnumField.Init is the older way. The constructor sets the type and the value together.
        public static EnumField CreateEnumField(string label, Enum enumValue, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            return Decorate(new EnumField(label, enumValue), parent, styleSheet, classNames);
        }

        #endregion

        #region VIEWS

        public static ScrollView CreateScrollView(VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            return CreateElement<ScrollView>(parent, styleSheet, classNames);
        }

        public static ListView CreateListView(VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            return CreateElement<ListView>(parent, styleSheet, classNames);
        }

        public static TreeView CreateTreeView(VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            return CreateElement<TreeView>(parent, styleSheet, classNames);
        }

        public static MultiColumnListView CreateMultiColumnListView(VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            return CreateElement<MultiColumnListView>(parent, styleSheet, classNames);
        }

        public static MultiColumnTreeView CreateMultiColumnTreeView(VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            return CreateElement<MultiColumnTreeView>(parent, styleSheet, classNames);
        }

        public static GroupBox CreateGroupBox(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            GroupBox groupBox = CreateElement<GroupBox>(parent, styleSheet, classNames);
            groupBox.text = label;
            return groupBox;
        }

        #endregion

#if UNITY_EDITOR

        #region EDITOR FIELDS

        public static TagField CreateTagField(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            TagField field = CreateElement<TagField>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static MaskField CreateMaskField(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            MaskField field = CreateElement<MaskField>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static LayerField CreateLayerField(string label, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            LayerField field = CreateElement<LayerField>(parent, styleSheet, classNames);
            field.label = label;
            return field;
        }

        public static EnumFlagsField CreateEnumFlagsField(string label, Enum enumValue, VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            return Decorate(new EnumFlagsField(label, enumValue), parent, styleSheet, classNames);
        }

        public static ObjectField CreateObjectField(string label, bool allowSceneObjects, Type objectType,
            VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            ObjectField field = CreateElement<ObjectField>(parent, styleSheet, classNames);
            field.label = label;
            field.objectType = objectType;
            field.allowSceneObjects = allowSceneObjects;
            return field;
        }

        public static PropertyField CreatePropertyField(string label, SerializedProperty property,
            VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            PropertyField field = CreateElement<PropertyField>(parent, styleSheet, classNames);
            field.label = label;
            field.BindProperty(property);
            return field;
        }

        #endregion

        #region EDITOR TOOLBAR

        public static Toolbar CreateToolbar(VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            return CreateElement<Toolbar>(parent, styleSheet, classNames);
        }

        public static ToolbarMenu CreateToolbarMenu(VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            return CreateElement<ToolbarMenu>(parent, styleSheet, classNames);
        }

        public static ToolbarButton CreateToolbarButton(VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            return CreateElement<ToolbarButton>(parent, styleSheet, classNames);
        }

        public static ToolbarSearchField CreateToolbarSearchField(VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            return CreateElement<ToolbarSearchField>(parent, styleSheet, classNames);
        }

        public static ToolbarPopupSearchField CreateToolbarPopupSearchField(VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            return CreateElement<ToolbarPopupSearchField>(parent, styleSheet, classNames);
        }

        public static ToolbarSpacer CreateToolbarSpacer(VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            return CreateElement<ToolbarSpacer>(parent, styleSheet, classNames);
        }

        public static ToolbarToggle CreateToolbarToggle(VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames)
        {
            return CreateElement<ToolbarToggle>(parent, styleSheet, classNames);
        }

        #endregion

#endif

        #region HELPERS

        /// <summary>Creates an element of type <typeparamref name="T"/>, then adds the style sheet and classes and parents it.</summary>
        public static T CreateElement<T>(VisualElement parent = null, StyleSheet styleSheet = null, params string[] classNames) where T : VisualElement, new()
        {
            return Decorate(new T(), parent, styleSheet, classNames);
        }

        private static T Decorate<T>(T element, VisualElement parent, StyleSheet styleSheet, string[] classNames) where T : VisualElement
        {
            if (styleSheet)
            {
                element.styleSheets.Add(styleSheet);
            }

            if (classNames != null)
            {
                foreach (string className in classNames)
                {
                    element.AddToClassList(className);
                }
            }

            parent?.Add(element);
            return element;
        }

        #endregion
    }
}
