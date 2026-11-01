using UnityEditor;
using UnityEngine;

namespace SpaceInvaders.Editor
{
    /// <summary>
    /// Imports the pixel art drawn in Aseprite (the sources are in Art~) as crisp sprites: point filtering, no mipmaps and no
    /// compression, so the pixels stay pixels. The explosion frames are 1 unit wide; a star tile is 16 units wide.
    /// </summary>
    internal static class ArtImporter
    {
        public const string ExplosionFolder = "Assets/_Project/Art/Effects/Explosion";
        public const string StarsFolder = "Assets/_Project/Art/Stars";
        public const int ExplosionFrameCount = 8;

        private const float ExplosionPixelsPerUnit = 32f;
        private const float StarPixelsPerUnit = 16f;

        public static void Import()
        {
            for (int i = 0; i < ExplosionFrameCount; i++)
            {
                Configure(FramePath(i), ExplosionPixelsPerUnit);
            }

            foreach (string name in new[] { "stars_far", "stars_mid", "stars_near" })
            {
                Configure(StarPath(name), StarPixelsPerUnit);
            }

            AssetDatabase.SaveAssets();
        }

        public static string FramePath(int index) => $"{ExplosionFolder}/explosion_{index}.png";

        public static string StarPath(string name) => $"{StarsFolder}/{name}.png";

        public static Object[] LoadExplosionFrames()
        {
            Object[] frames = new Object[ExplosionFrameCount];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath(i))
                            ?? throw new System.InvalidOperationException($"Missing explosion frame {FramePath(i)}. Run Art~/build.ps1.");
            }

            return frames;
        }

        public static Sprite LoadStars(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(StarPath(name))
                   ?? throw new System.InvalidOperationException($"Missing star tile {StarPath(name)}. Run Art~/build.ps1.");
        }

        private static void Configure(string path, float pixelsPerUnit)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter
                                       ?? throw new System.InvalidOperationException($"Missing art {path}. Run Art~/build.ps1.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            // A tiled sprite needs a full rectangle mesh.
            TextureImporterSettings settings = new();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
