// ToBase64 and ToTexture2D are based on work by Cristian Alexandru Geambasu (MIT)
// https://gist.github.com/daemon3000/609e44a907a8b37def0d

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pretzel.Extensions
{
    public static class Texture2DExtensions
    {
        /// <summary>
        /// Encode a texture as a base 64 PNG string. The texture must be readable.
        /// </summary>
        /// <returns>The encoded texture, or an empty string when the texture is null or destroyed</returns>
        public static string ToBase64(this Texture2D texture)
        {
            return texture ? Convert.ToBase64String(texture.EncodeToPNG()) : string.Empty;
        }

        /// <summary>
        /// Create a texture from a base 64 encoded image. The caller owns the texture and must destroy it.
        /// </summary>
        /// <returns>The texture, or null when the string is empty or is not a supported image</returns>
        public static Texture2D ToTexture2D(this string base64)
        {
            if (string.IsNullOrEmpty(base64))
            {
                return null;
            }

            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(Convert.FromBase64String(base64)))
            {
                return texture;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(texture);
            }
            else
            {
                Object.DestroyImmediate(texture);
            }

            return null;
        }
    }
}
