using System;
using UnityEditor;
using UnityEngine;

namespace SpaceInvaders.Editor
{
    /// <summary>Unity's built-in sprites and the URP unlit sprite material: the only "art" the demo uses.</summary>
    internal static class DefaultArt
    {
        private const string RoundedSpritePath = "UI/Skin/UISprite.psd";
        private const string CircleSpritePath = "UI/Skin/Knob.psd";
        private const string UnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        public static Sprite Rounded { get; private set; }

        public static Sprite Circle { get; private set; }

        public static Material Unlit { get; private set; }

        public static void Load()
        {
            Rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>(RoundedSpritePath);
            Circle = AssetDatabase.GetBuiltinExtraResource<Sprite>(CircleSpritePath);
            Unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
            if (Rounded == null || Circle == null || Unlit == null)
            {
                throw new InvalidOperationException("The default sprites or the URP unlit sprite material were not found.");
            }
        }
    }
}
