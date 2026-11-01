using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pretzel.Editor.Prototyping
{
    /// <summary>
    /// Draws white placeholder sprites (square, rounded square, circle, triangle, diamond, hexagon) so a prototype has shapes to tint with
    /// a <c>SpriteRenderer</c> colour, without importing art. Existing files are never overwritten.
    /// </summary>
    public static class PrototypeSprites
    {
        public const string DefaultFolder = "Assets/_Project/Art/Sprites/Prototype";

        /// <summary>Pixels per side, and the pixels per unit, so each sprite is one world unit wide.</summary>
        public const int Size = 64;

        private const int RoundedBorder = 16;
        private const int Samples = 4;

        /// <summary>True when the point lies inside the shape. The shape fills the square from -1 to 1 on both axes.</summary>
        public static bool Contains(PrototypeShape shape, float x, float y)
        {
            switch (shape)
            {
                case PrototypeShape.Square:
                    return Math.Abs(x) <= 1f && Math.Abs(y) <= 1f;
                case PrototypeShape.RoundedSquare:
                    float dx = Math.Max(Math.Abs(x) - 0.5f, 0f);
                    float dy = Math.Max(Math.Abs(y) - 0.5f, 0f);
                    return Math.Abs(x) <= 1f && Math.Abs(y) <= 1f && dx * dx + dy * dy <= 0.25f;
                case PrototypeShape.Circle:
                    return x * x + y * y <= 1f;
                case PrototypeShape.Triangle:
                    return y >= -1f && y <= 1f && Math.Abs(x) <= (1f - y) * 0.5f;
                case PrototypeShape.Diamond:
                    return Math.Abs(x) + Math.Abs(y) <= 1f;
                case PrototypeShape.Hexagon:
                    const float Height = 0.8660254f;
                    return Math.Abs(y) <= Height && Height * Math.Abs(x) + 0.5f * Math.Abs(y) <= Height;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }
        }

        /// <summary>The share of the pixel (0 to 1) that is inside the shape, from a grid of samples. Pixel (0, 0) is the bottom left.</summary>
        public static float Coverage(PrototypeShape shape, int pixelX, int pixelY)
        {
            int inside = 0;
            for (int sy = 0; sy < Samples; sy++)
            {
                for (int sx = 0; sx < Samples; sx++)
                {
                    float x = (pixelX + (sx + 0.5f) / Samples) / Size * 2f - 1f;
                    float y = (pixelY + (sy + 0.5f) / Samples) / Size * 2f - 1f;
                    if (Contains(shape, x, y))
                    {
                        inside++;
                    }
                }
            }

            return inside / (float)(Samples * Samples);
        }

        /// <summary>Creates every missing sprite in <paramref name="folder"/> (a path under Assets). Returns the paths that were created.</summary>
        public static IReadOnlyList<string> Create(string folder = DefaultFolder)
        {
            EnsureFolder(folder);
            List<string> created = new();
            foreach (PrototypeShape shape in Enum.GetValues(typeof(PrototypeShape)))
            {
                string path = $"{folder}/{shape}.png";
                if (File.Exists(path))
                {
                    continue;
                }

                File.WriteAllBytes(path, Render(shape));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                Configure(path, shape);
                created.Add(path);
            }

            AssetDatabase.SaveAssets();
            return created;
        }

        [MenuItem("Pretzel/Create/Prototype Sprites", priority = 121)]
        private static void CreateFromMenu()
        {
            IReadOnlyList<string> created = Create();
            Debug.Log($"[Pretzel] Prototype sprites: created {created.Count} in {DefaultFolder}.");
        }

        private static byte[] Render(PrototypeShape shape)
        {
            Texture2D texture = new(Size, Size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    byte alpha = (byte)Mathf.RoundToInt(Coverage(shape, x, y) * 255f);
                    pixels[y * Size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            byte[] png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            return png;
        }

        private static void Configure(string path, PrototypeShape shape)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Size;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            if (shape == PrototypeShape.RoundedSquare)
            {
                // Lets a SpriteRenderer or UI Image stretch the sprite to any size with the corners intact.
                importer.spriteBorder = new Vector4(RoundedBorder, RoundedBorder, RoundedBorder, RoundedBorder);
            }

            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string folder)
        {
            string[] segments = folder.Split('/');
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
    }
}
