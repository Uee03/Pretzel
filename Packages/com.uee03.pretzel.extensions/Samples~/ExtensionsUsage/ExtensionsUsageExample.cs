using System.Collections.Generic;
using System.IO;
using Pretzel.Extensions;
using UnityEngine;

namespace Pretzel.Samples.Extensions
{
    /// <summary>
    /// Add this to a GameObject in an empty scene and press Play. The Console shows the result of each example.
    /// </summary>
    public class ExtensionsUsageExample : MonoBehaviour
    {
        [SerializeField] private List<string> _words = new() { "Hello", "World", "Simple", "Shuffle" };
        [SerializeField] private string[] _wordArray = { "Alpha", "Beta", "Gamma" };
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] private Transform _parent;
        [SerializeField] private GameObject _prefab;

        private void Start()
        {
            StringExamples();
            CollectionExamples();
            FloatExamples();
            ObjectExamples();
            LayerMaskExamples();
            TransformExamples();
            GameObjectExamples();
            IOExamples();
        }

        private void StringExamples()
        {
            string email = "ada@example.com";
            Debug.Log($"Valid email: {email.IsValidEmail()}");
            Debug.Log($"Valid url: {"https://unity.com".IsValidUrl()}");
            Debug.Log($"Empty after trim: {"   ".IsNullOrEmpty(trim: true)}");

            // Cleaning text
            Debug.Log("Hello,   World! #1".RemoveSpecialChars());          // "Hello   World 1"
            Debug.Log("Hello,   World! #1".ToAlphanumeric());              // "HelloWorld1"
            Debug.Log("  too    many   spaces ".InnerTrim());              // "too many spaces"

            // Encoding and base 64
            string encoded = "Pretzel".ToBase64();
            Debug.Log($"{encoded} -> {encoded.FromBase64()}");
            byte[] bytes = "Pretzel".ToBytes(EncodingType.ASCII);
            Debug.Log($"{bytes.Length} bytes -> {bytes.FromBytes(EncodingType.ASCII)}");
        }

        private void CollectionExamples()
        {
            // Lists and arrays share the same extensions.
            _words.Shuffle();
            _wordArray.Shuffle();
            _words.Swap(0, _words.Count - 1);
            Debug.Log($"Random word: {_words.GetRandomItem()}");

            // Use TryGetRandomItem when the collection may be empty.
            List<int> empty = new();
            Debug.Log(empty.TryGetRandomItem(out int number) ? $"Got {number}" : "Nothing to pick from");
        }

        private void FloatExamples()
        {
            Debug.Log(75f.ToTimeMS());      // 01:15
            Debug.Log(3725f.ToTimeMS());    // 62:05
            Debug.Log(3725f.ToTimeHMS());   // 01:02:05
        }

        private void ObjectExamples()
        {
            string text = null;
            Debug.Log($"Null string: {text.IsNull()}");

            // The Unity overload also reports destroyed objects.
            Debug.Log($"Parent missing: {_parent.IsNull()}");
        }

        private void LayerMaskExamples()
        {
            Debug.Log($"Default layer in mask: {_layerMask.InLayerMask(0)}");
        }

        private void TransformExamples()
        {
            Debug.Log($"Rounded position: {transform.PositionToInt()}");

            if (_parent)
            {
                transform.ReparentAndAlign(_parent);
            }

            // Throws if the layer does not exist in the project's Tags and Layers.
            transform.SetColliderInteractionLayers("Default");
        }

        private void GameObjectExamples()
        {
            if (!_prefab)
            {
                return;
            }

            // Clone places the copy at the origin without rotation unless you pass values.
            GameObject copy = _prefab.Clone(new Vector3(0f, 1f, 0f), parent: _parent);

            // The routines are coroutines, so start them yourself.
            StartCoroutine(copy.DeactivateRoutine(2f));

            // Or use the MonoBehaviour shortcuts.
            this.DeactivateGO(5f);
            copy.Destroy(10f);
        }

        private void IOExamples()
        {
            string path = Path.Combine(Application.persistentDataPath, "extensions-example.txt");

            path.WriteText("first line");
            path.WriteWithStream("\nsecond line", FileMode.Append);
            Debug.Log(path.ReadText());

            foreach (string line in path.ReadLines())
            {
                Debug.Log(line);
            }
        }
    }
}
