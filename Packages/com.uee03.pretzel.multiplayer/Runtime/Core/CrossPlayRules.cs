using System;

namespace Pretzel.Multiplayer
{
    /// <summary>
    /// Cross-play rules. Online mode is platform-agnostic (Relay plus Authentication), so enabling cross-play never restricts a join.
    /// With cross-play off, browsing and quick-join can be limited to the player's own platform tag. Joining by code or id is an explicit choice and is never limited.
    /// </summary>
    public static class CrossPlayRules
    {
        public static string NormalizeTag(string tag)
        {
            return string.IsNullOrWhiteSpace(tag) ? PlatformTags.Unknown : tag.Trim().ToLowerInvariant();
        }

        /// <summary>The platform tag a query or quick-join must match, or null when nothing is restricted.</summary>
        public static string GetQueryPlatformTag(bool crossPlayEnabled, string localPlatformTag)
        {
            return crossPlayEnabled ? null : NormalizeTag(localPlatformTag);
        }

        /// <summary>Whether a listed session is offered to the player. A session without a tag (created by another client or an older version) is offered.</summary>
        public static bool IsVisible(bool crossPlayEnabled, string localPlatformTag, string sessionPlatformTag)
        {
            if (crossPlayEnabled || string.IsNullOrWhiteSpace(sessionPlatformTag))
            {
                return true;
            }

            return string.Equals(NormalizeTag(localPlatformTag), NormalizeTag(sessionPlatformTag), StringComparison.Ordinal);
        }
    }
}
