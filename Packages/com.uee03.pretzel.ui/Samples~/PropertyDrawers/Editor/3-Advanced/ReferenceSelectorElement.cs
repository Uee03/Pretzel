using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.PropertyDrawers.Editors
{
    /// <summary>
    /// A type dropdown for a [SerializeReference] property plus the fields of the chosen type. The element owns the
    /// dropdown and the child fields; it re-syncs on Undo (subscribed only while on a panel).
    /// </summary>
    public sealed class ReferenceSelectorElement : VisualElement
    {
        private const string NoneChoice = "None";

        private readonly SerializedProperty _property;
        private readonly List<Type> _types = new();
        private readonly List<string> _typeNames = new();
        private readonly List<string> _choices = new();
        private readonly DropdownField _dropdown;
        private readonly VisualElement _children;
        private string _shownTypeName;

        public ReferenceSelectorElement(SerializedProperty property)
        {
            _property = property.Copy();
            AddToClassList("pretzel-drawer");
            SampleAssets.AddStyleSheet(this, "PropertyDrawerSamples.uss");

            BuildTypeList(ResolveBaseType(_property.managedReferenceFieldTypename));

            _dropdown = new DropdownField(_property.displayName, _choices, 0);
            _dropdown.RegisterValueChangedCallback(OnDropdownChanged);
            // Replacing the instance through several objects at once is ambiguous, so it is limited to a single object.
            _dropdown.SetEnabled(!_property.serializedObject.isEditingMultipleObjects);
            Add(_dropdown);

            _children = new VisualElement();
            _children.AddToClassList("pretzel-drawer__children");
            Add(_children);

            this.TrackPropertyValue(_property, OnPropertyChanged);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
            Sync();
        }

        private static Type ResolveBaseType(string fieldTypeName)
        {
            // The field type name is "AssemblyName Namespace.TypeName".
            int space = string.IsNullOrEmpty(fieldTypeName) ? -1 : fieldTypeName.IndexOf(' ');
            if (space < 0)
            {
                return typeof(object);
            }

            string assemblyName = fieldTypeName.Substring(0, space);
            string typeName = fieldTypeName.Substring(space + 1);
            return Type.GetType(typeName + ", " + assemblyName) ?? typeof(object);
        }

        private void BuildTypeList(Type baseType)
        {
            List<Type> candidates = new();
            if (IsCreatable(baseType))
            {
                candidates.Add(baseType);
            }

            foreach (Type type in TypeCache.GetTypesDerivedFrom(baseType))
            {
                if (IsCreatable(type))
                {
                    candidates.Add(type);
                }
            }

            candidates.Sort(CompareByName);

            _types.Add(null);
            _typeNames.Add(string.Empty);
            _choices.Add(NoneChoice);
            foreach (Type type in candidates)
            {
                _types.Add(type);
                _typeNames.Add(type.Assembly.GetName().Name + " " + type.FullName);
                _choices.Add(ObjectNames.NicifyVariableName(type.Name));
            }
        }

        private static bool IsCreatable(Type type)
        {
            return type != typeof(object)
                && !type.IsAbstract
                && !type.IsGenericType
                && type.IsSerializable
                && type.GetConstructor(Type.EmptyTypes) != null;
        }

        private static int CompareByName(Type a, Type b)
        {
            return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            Undo.undoRedoPerformed += Sync;
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            Undo.undoRedoPerformed -= Sync;
        }

        private void OnPropertyChanged(SerializedProperty changed)
        {
            Sync();
        }

        private void OnDropdownChanged(ChangeEvent<string> evt)
        {
            int index = _dropdown.index;
            if (index < 0)
            {
                return;
            }

            // The new instance is created with its default values; ApplyModifiedProperties records the change for Undo.
            _property.managedReferenceValue = index > 0 ? Activator.CreateInstance(_types[index]) : null;
            _property.serializedObject.ApplyModifiedProperties();
            _shownTypeName = null;
            Sync();
        }

        private void Sync()
        {
            string typeName = _property.managedReferenceFullTypename;
            if (string.Equals(typeName, _shownTypeName, StringComparison.Ordinal))
            {
                return;
            }

            _shownTypeName = typeName;
            int index = 0;
            for (int i = 1; i < _typeNames.Count; i++)
            {
                if (string.Equals(_typeNames[i], typeName, StringComparison.Ordinal))
                {
                    index = i;
                    break;
                }
            }

            _dropdown.SetValueWithoutNotify(_choices[index]);
            RebuildChildren();
        }

        private void RebuildChildren()
        {
            _children.Clear();
            if (string.IsNullOrEmpty(_property.managedReferenceFullTypename))
            {
                return;
            }

            SerializedProperty end = _property.GetEndProperty();
            SerializedProperty child = _property.Copy();
            for (bool enter = true; child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end); enter = false)
            {
                _children.Add(new PropertyField(child.Copy()));
            }

            // Elements added after the inspector was bound need their own Bind.
            _children.Bind(_property.serializedObject);
        }
    }
}
