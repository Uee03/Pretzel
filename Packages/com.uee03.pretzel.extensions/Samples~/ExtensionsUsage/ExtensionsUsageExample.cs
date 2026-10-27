using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pretzel.Extensions;
using UnityEngine;

namespace Pretzel.Samples.Extensions
{
    /// <summary>
    /// Add this to a GameObject in an empty scene and press Play. The Console shows the result of each example.
    /// Every object the sample creates lives under a temporary sandbox, which is destroyed with the sample.
    /// </summary>
    public class ExtensionsUsageExample : MonoBehaviour
    {
        [SerializeField] private List<string> _words = new() { "Hello", "World", "Simple", "Shuffle" };
        [SerializeField] private string[] _wordArray = { "Alpha", "Beta", "Gamma" };
        [SerializeField] private LayerMask _layerMask = 1;
        [Tooltip("Optional. A prefab to clone. A cube is used when empty.")]
        [SerializeField] private GameObject _prefab;

        private GameObject _sandbox;

        private async void Start()
        {
            _sandbox = new GameObject("Extensions Sandbox");

            StringExamples();
            ByteAndEncodingExamples();
            CollectionExamples();
            FloatExamples();
            ObjectExamples();
            LayerMaskExamples();
            TransformExamples();
            GameObjectExamples();
            MonoBehaviourExamples();
            TextureExamples();
            FileExamples();

            try
            {
                await FileExamplesAsync(destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // The sample was destroyed while a file operation was running.
            }
            catch (IOException ex)
            {
                this.PrintException(ex);
            }
        }

        private void OnDestroy()
        {
            // The sandbox may already be gone when the scene unloads.
            if (_sandbox.IsNotNull())
            {
                _sandbox.Destroy();
            }
        }

        private void StringExamples()
        {
            Debug.Log($"Valid email: {"ada@example.com".IsValidEmail()}");
            Debug.Log($"Valid url: {"https://unity.com".IsValidUrl()}");
            Debug.Log($"Valid phone: {"+27-82-1234".IsValidPhoneNumber()}");
            Debug.Log($"Alphanumeric: {"Player42".IsAlphanumeric()}");
            Debug.Log($"Matches a level id: {"lvl-07".MatchesPattern(@"^lvl-\d{2}$")}");
            Debug.Log($"Has special characters: {"Hi!".HasSpecialChars()}");
            Debug.Log($"Empty after trim: {"   ".IsNullOrEmpty(trim: true)}, white space: {"\t".IsNullOrWhiteSpace()}");

            // Cleaning user input, for example a player name or a file name.
            Debug.Log("Hello,   World! #1".RemoveSpecialChars());                     // "Hello   World 1"
            Debug.Log("Hello,   World! #1".ToAlphanumeric());                         // "HelloWorld1"
            Debug.Log("Ada's  save: #1 (backup)".RemoveSpecialCharsKeepPunctuation()); // "Ada's save: 1 (backup)"
            Debug.Log("  too    many   spaces ".InnerTrim());                         // "too many spaces"
        }

        private void ByteAndEncodingExamples()
        {
            string encoded = "Pretzel".ToBase64();
            Debug.Log($"{encoded} -> {encoded.FromBase64()}");

            byte[] bytes = "Pretzel".ToBytes(EncodingType.ASCII);
            Debug.Log($"{bytes.Length} bytes -> {bytes.FromBytes(EncodingType.ASCII)}, base 64 {bytes.ToBase64()}");
            Debug.Log($"Back to {encoded.FromBase64ToBytes().Length} bytes. UTF32 is {EncodingType.UTF32.ToEncoding().EncodingName}.");
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
            Debug.Log($"Null string: {text.IsNull()}, sandbox exists: {_sandbox.IsNotNull()}");

            // The Unity overload also reports destroyed objects, which a plain C# null check misses.
            Debug.Log($"Prefab missing: {_prefab.IsNull()}");
        }

        private void LayerMaskExamples()
        {
            Debug.Log($"This object's layer is in the mask: {_layerMask.InLayerMask(gameObject.layer)}");
        }

        private void TransformExamples()
        {
            Transform root = _sandbox.transform;
            Transform child = new GameObject("Child").transform;
            child.position = new Vector3(1.4f, 2.6f, -0.2f);
            Debug.Log($"Rounded position: {child.PositionToInt()}");

            // Snap onto the parent: local position and rotation are reset.
            child.ReparentAndAlign(root);

            // Every collider under the root moves to the layer. Throws if the layer is not in Tags and Layers.
            GameObject.CreatePrimitive(PrimitiveType.Sphere).transform.SetParent(root);
            root.SetColliderInteractionLayers("Default");

            Transform copy = child.Clone(Vector3.right, Quaternion.identity, root);
            copy.Deactivate();
            copy.Activate();
            copy.Destroy();

            // Inactive children are kept unless destroyInactive is true.
            Transform container = new GameObject("Container").transform;
            container.SetParent(root);
            new GameObject("Active child").transform.SetParent(container);
            GameObject inactive = new("Inactive child");
            inactive.SetParent(container);
            inactive.Deactivate();
            container.DestroyAllChildren(destroyInactive: true);
        }

        private void GameObjectExamples()
        {
            bool usePrefab = _prefab;
            GameObject source = usePrefab ? _prefab : CreateTemplateCube();

            // Clone places the copy at the origin without rotation unless you pass values.
            GameObject copy = source.Clone(new Vector3(0f, 1f, 0f), parent: _sandbox.transform);
            Debug.Log($"Copy's parent transform: {copy.GetParent().name}, parent object: {copy.GetParentGo().name}");

            GameObject loose = new("Loose");
            loose.SetParent(_sandbox);           // GameObject parent
            loose.SetParent((GameObject)null);   // back to the scene root
            loose.SetParent(_sandbox.transform); // Transform parent

            // The routines are coroutines, so start them on a behaviour that stays active.
            loose.Deactivate();
            StartCoroutine(loose.ActivateRoutine(1f));
            StartCoroutine(copy.DeactivateRoutine(2f));
            copy.Destroy(5f);

            if (!usePrefab)
            {
                source.Destroy();
            }
        }

        private void MonoBehaviourExamples()
        {
            // Logs that select this component when clicked in the Console.
            this.PrintWarning("A warning from the extensions sample.");
            this.PrintError("An error from the extensions sample. This one is expected.");

            GameObject holder = new("Ticker");
            ExtensionsTicker ticker = holder.AddComponent<ExtensionsTicker>();
            ticker.SetParent(_sandbox.transform);

            // Activate and Deactivate change the component's enabled flag. The GO variants change the GameObject.
            ticker.Deactivate();
            ticker.Activate();
            ticker.DeactivateGO();
            ticker.ActivateGO();

            // The timed variants run a coroutine on the ticker, so start them while it is active and enabled.
            ticker.Deactivate(3f);
            ticker.DeactivateGO(4f);
            ticker.Destroy(6f);   // only the component
            ticker.DestroyGO(8f); // the whole GameObject
        }

        private void TextureExamples()
        {
            Texture2D texture = new(4, 4);
            texture.SetPixel(0, 0, Color.magenta);
            texture.Apply();

            string base64 = texture.ToBase64();
            Texture2D restored = base64.ToTexture2D();
            Debug.Log(restored
                ? $"PNG as base 64 is {base64.Length} characters, restored {restored.width}x{restored.height}."
                : "The base 64 text was not a supported image.");

            // Both textures belong to this method, so destroy them.
            Destroy(texture);
            if (restored)
            {
                Destroy(restored);
            }
        }

        private static string SampleFolder()
        {
            string folder = Path.Combine(Application.temporaryCachePath, "PretzelSamples");
            Directory.CreateDirectory(folder);
            return folder;
        }

        private void FileExamples()
        {
            string folder = SampleFolder();
            string path = Path.Combine(folder, "extensions-example.txt");
            Debug.Log($"Path valid: {path.IsPathValid()}, folder only: {(folder + "/").IsPathValid()}");

            path.WriteText("first line");
            path.WriteWithStream("\nsecond line", FileMode.Append);
            Debug.Log(path.ReadText());
            Debug.Log(path.ReadWithStream());

            path.WriteLines(new[] { "one", "two" });
            foreach (string line in path.ReadLines())
            {
                Debug.Log(line);
            }

            string bytesPath = Path.Combine(folder, "extensions-example.bin");
            bytesPath.WriteBytes(new byte[] { 1, 2, 3 });
            Debug.Log($"Read {bytesPath.ReadBytes().Length} bytes.");
        }

        private static async Task FileExamplesAsync(CancellationToken cancellationToken)
        {
            string folder = SampleFolder();
            string path = Path.Combine(folder, "extensions-example-async.txt");

            // The code after each await resumes on the main thread, so it may use Unity objects.
            await path.WriteTextAsync("async text", cancellationToken: cancellationToken);
            Debug.Log(await path.ReadTextAsync(cancellationToken: cancellationToken));

            await path.WriteLinesAsync(new[] { "a", "b", "c" }, cancellationToken: cancellationToken);
            Debug.Log($"{(await path.ReadLinesAsync(cancellationToken: cancellationToken)).Length} lines");

            await path.WriteWithStreamAsync("\nappended", FileMode.Append);
            Debug.Log(await path.ReadWithStreamAsync());

            string bytesPath = Path.Combine(folder, "extensions-example-async.bin");
            await bytesPath.WriteBytesAsync(new byte[] { 4, 5, 6 }, cancellationToken);
            Debug.Log($"Read {(await bytesPath.ReadBytesAsync(cancellationToken)).Length} bytes asynchronously.");
        }

        private GameObject CreateTemplateCube()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.SetParent(_sandbox);
            return cube;
        }
    }
}
