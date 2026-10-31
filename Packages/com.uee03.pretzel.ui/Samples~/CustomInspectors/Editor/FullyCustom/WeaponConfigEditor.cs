using Pretzel.UI.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.CustomInspectors.Editors
{
    /// <summary>
    /// A fully custom layout: PropertyFields found through <c>bindingPath</c>, foldout sections and groups that
    /// show or hide depending on the weapon kind. Unity binds the returned tree to the serialized object.
    /// </summary>
    [CustomEditor(typeof(WeaponConfig))]
    [CanEditMultipleObjects]
    public sealed class WeaponConfigEditor : UnityEditor.Editor
    {
        private const string HiddenClass = "pretzel-weapon__group--hidden";

        private VisualElement _meleeGroup;
        private VisualElement _rangedGroup;
        private VisualElement _magicGroup;
        private VisualElement _projectileSpeed;
        private HelpBox _mixedKinds;
        private Label _preview;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            root.AddToClassList("pretzel-weapon");
            SampleAssets.AddStyleSheet(root, "WeaponConfigEditor.uss");

            Label title = new("Weapon");
            title.AddToClassList("pretzel-weapon__title");
            root.Add(title);

            Foldout general = AddSection(root, "General", "pretzel-weapon-general");
            AddField(general, nameof(WeaponConfig.displayName));
            AddField(general, nameof(WeaponConfig.kind));
            AddField(general, nameof(WeaponConfig.damage));
            AddField(general, nameof(WeaponConfig.cooldown));

            Foldout kindSettings = AddSection(root, "Kind settings", "pretzel-weapon-kind");
            _mixedKinds = EditorUI.Help("The selected weapons have different kinds, so every group is shown.", HelpBoxMessageType.Info, kindSettings);

            _meleeGroup = AddGroup(kindSettings, "Melee");
            AddField(_meleeGroup, nameof(WeaponConfig.reach));
            AddField(_meleeGroup, nameof(WeaponConfig.cleave));

            _rangedGroup = AddGroup(kindSettings, "Ranged");
            AddField(_rangedGroup, nameof(WeaponConfig.range));
            AddField(_rangedGroup, nameof(WeaponConfig.magazineSize));
            AddField(_rangedGroup, nameof(WeaponConfig.usesProjectile));
            _projectileSpeed = AddField(_rangedGroup, nameof(WeaponConfig.projectileSpeed));

            _magicGroup = AddGroup(kindSettings, "Magic");
            AddField(_magicGroup, nameof(WeaponConfig.manaCost));
            AddField(_magicGroup, nameof(WeaponConfig.effectColor));

            _preview = new Label();
            _preview.AddToClassList("pretzel-weapon__preview");
            root.Add(_preview);

            // Tracked callbacks fire on change; the initial state is applied right after.
            root.TrackPropertyValue(serializedObject.FindProperty(nameof(WeaponConfig.kind)), OnKindChanged);
            root.TrackPropertyValue(serializedObject.FindProperty(nameof(WeaponConfig.usesProjectile)), OnUsesProjectileChanged);
            root.TrackSerializedObjectValue(serializedObject, OnSerializedObjectChanged);

            OnKindChanged(serializedObject.FindProperty(nameof(WeaponConfig.kind)));
            OnUsesProjectileChanged(serializedObject.FindProperty(nameof(WeaponConfig.usesProjectile)));
            UpdatePreview();
            return root;
        }

        private static Foldout AddSection(VisualElement parent, string title, string persistenceKey)
        {
            Foldout section = EditorUI.Section(title, persistenceKey, parent: parent);
            section.AddToClassList("pretzel-weapon__section");
            return section;
        }

        private static VisualElement AddGroup(VisualElement parent, string title)
        {
            VisualElement group = new();
            group.AddToClassList("pretzel-weapon__group");

            Label label = new(title);
            label.AddToClassList("pretzel-weapon__group-title");
            group.Add(label);

            parent.Add(group);
            return group;
        }

        private static PropertyField AddField(VisualElement parent, string propertyPath)
        {
            PropertyField field = new() { bindingPath = propertyPath };
            parent.Add(field);
            return field;
        }

        private void OnKindChanged(SerializedProperty kind)
        {
            bool mixed = kind.hasMultipleDifferentValues;
            WeaponKind value = (WeaponKind)kind.enumValueIndex;
            _mixedKinds.style.display = mixed ? DisplayStyle.Flex : DisplayStyle.None;
            _meleeGroup.EnableInClassList(HiddenClass, !mixed && value != WeaponKind.Melee);
            _rangedGroup.EnableInClassList(HiddenClass, !mixed && value != WeaponKind.Ranged);
            _magicGroup.EnableInClassList(HiddenClass, !mixed && value != WeaponKind.Magic);
        }

        private void OnUsesProjectileChanged(SerializedProperty usesProjectile)
        {
            bool show = usesProjectile.hasMultipleDifferentValues || usesProjectile.boolValue;
            _projectileSpeed.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnSerializedObjectChanged(SerializedObject changed)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            WeaponConfig weapon = (WeaponConfig)target;
            _preview.text = targets.Length == 1
                ? $"{weapon.displayName}: {weapon.DamagePerSecond:0.0} damage per second"
                : $"{targets.Length} weapons selected";
        }
    }
}
