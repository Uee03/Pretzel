using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceInvaders.Tests
{
    /// <summary>
    /// Saves what the player would see as a PNG without WaitForEndOfFrame, which never fires in batch mode. The camera renders to a
    /// RenderTexture, every UI panel renders to its own RenderTexture, and the layers are blended on the CPU.
    /// </summary>
    internal static class ScreenshotUtility
    {
        private const int Width = 1280;
        private const int Height = 720;

        public static IEnumerator Capture(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            yield return null;

            Camera camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            RenderTexture gameTarget = new(Width, Height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = gameTarget;
            camera.Render();
            camera.targetTexture = null;
            Color32[] pixels = Read(gameTarget);
            Object.Destroy(gameTarget);

            List<PanelSettings> panels = FindPanels();
            Dictionary<PanelSettings, RenderTexture> targets = new();
            foreach (PanelSettings panel in panels)
            {
                RenderTexture target = new(Width, Height, 0, RenderTextureFormat.ARGB32);
                panel.clearColor = true;
                panel.colorClearValue = Color.clear;
                panel.targetTexture = target;
                targets[panel] = target;
            }

            yield return null;
            yield return null;

            foreach (PanelSettings panel in panels)
            {
                Blend(pixels, Read(targets[panel]));
                panel.targetTexture = null;
                Object.Destroy(targets[panel]);
            }

            Texture2D image = new(Width, Height, TextureFormat.RGBA32, false);
            image.SetPixels32(pixels);
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.Destroy(image);
        }

        private static List<PanelSettings> FindPanels()
        {
            List<PanelSettings> panels = new();
            foreach (UIDocument document in Object.FindObjectsByType<UIDocument>())
            {
                if (document.panelSettings != null && !panels.Contains(document.panelSettings))
                {
                    panels.Add(document.panelSettings);
                }
            }

            panels.Sort((a, b) => a.sortingOrder.CompareTo(b.sortingOrder));
            return panels;
        }

        private static Color32[] Read(RenderTexture source)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = source;
            Texture2D copy = new(source.width, source.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            RenderTexture.active = previous;
            Color32[] pixels = copy.GetPixels32();
            Object.Destroy(copy);
            return pixels;
        }

        private static void Blend(Color32[] below, Color32[] above)
        {
            for (int i = 0; i < below.Length; i++)
            {
                float alpha = above[i].a / 255f;
                if (alpha <= 0f)
                {
                    continue;
                }

                below[i] = new Color32(
                    (byte)(above[i].r * alpha + below[i].r * (1f - alpha)),
                    (byte)(above[i].g * alpha + below[i].g * (1f - alpha)),
                    (byte)(above[i].b * alpha + below[i].b * (1f - alpha)),
                    255);
            }
        }
    }
}
