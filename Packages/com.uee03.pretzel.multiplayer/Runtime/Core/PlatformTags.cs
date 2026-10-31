using UnityEngine;

namespace Pretzel.Multiplayer
{
    /// <summary>Short platform names used to tag players and sessions. They are labels for display and optional filtering, not capabilities.</summary>
    public static class PlatformTags
    {
        public const string Unknown = "other";

        public static string Current => FromRuntimePlatform(Application.platform);

        public static string FromRuntimePlatform(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return "windows";
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return "macos";
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    return "linux";
                case RuntimePlatform.Android:
                    return "android";
                case RuntimePlatform.IPhonePlayer:
                    return "ios";
                case RuntimePlatform.WebGLPlayer:
                    return "webgl";
                case RuntimePlatform.Switch:
                    return "switch";
                case RuntimePlatform.PS4:
                case RuntimePlatform.PS5:
                    return "playstation";
                case RuntimePlatform.GameCoreXboxOne:
                case RuntimePlatform.GameCoreXboxSeries:
                    return "xbox";
                default:
                    return Unknown;
            }
        }
    }
}
