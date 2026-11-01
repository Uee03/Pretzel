using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpaceInvaders.Editor
{
    /// <summary>Small helpers so builder code can set private [SerializeField] values the same way the Inspector does.</summary>
    internal static class BuilderUtil
    {
        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        public static void EnsureFolder(string assetPath)
        {
            string[] segments = assetPath.Split('/');
            string parent = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string path = parent + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(path))
                {
                    AssetDatabase.CreateFolder(parent, segments[i]);
                }

                parent = path;
            }
        }

        public static void Edit(Object target, Action<SerializedObject> change)
        {
            SerializedObject serialized = new(target);
            change(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        public static SerializedProperty Property(SerializedObject serialized, string path)
        {
            return serialized.FindProperty(path) ?? throw new ArgumentException($"{serialized.targetObject.GetType().Name} has no serialized field '{path}'.");
        }

        public static void SetObject(Object target, string path, Object value)
        {
            Edit(target, serialized => Property(serialized, path).objectReferenceValue = value);
        }

        public static void SetFloat(Object target, string path, float value)
        {
            Edit(target, serialized => Property(serialized, path).floatValue = value);
        }

        public static void SetInt(Object target, string path, int value)
        {
            Edit(target, serialized => Property(serialized, path).intValue = value);
        }

        public static void SetBool(Object target, string path, bool value)
        {
            Edit(target, serialized => Property(serialized, path).boolValue = value);
        }

        public static void SetString(Object target, string path, string value)
        {
            Edit(target, serialized => Property(serialized, path).stringValue = value);
        }

        public static void SetVector2(Object target, string path, Vector2 value)
        {
            Edit(target, serialized => Property(serialized, path).vector2Value = value);
        }

        public static void SetObjects(Object target, string path, IReadOnlyList<Object> values)
        {
            Edit(target, serialized =>
            {
                SerializedProperty array = Property(serialized, path);
                array.arraySize = values.Count;
                for (int i = 0; i < values.Count; i++)
                {
                    array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }
            });
        }

        public static void SetEnums(Object target, string path, IReadOnlyList<int> values)
        {
            Edit(target, serialized =>
            {
                SerializedProperty array = Property(serialized, path);
                array.arraySize = values.Count;
                for (int i = 0; i < values.Count; i++)
                {
                    array.GetArrayElementAtIndex(i).intValue = values[i];
                }
            });
        }
    }
}
