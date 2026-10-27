using System;

namespace Pretzel.Data
{
    /// <summary>Metadata saved next to each slot, so a load menu can list slots without loading the save data.</summary>
    [Serializable]
    public sealed class SaveSlotInfo
    {
        public int slot;
        public string label = string.Empty;
        public long savedAtUtcTicks;
        public double playTimeSeconds;
        public string sceneName = string.Empty;

        /// <summary>Free text for the load menu, such as "Chapter 3 - Level 12".</summary>
        public string summary = string.Empty;

        public DateTime SavedAtUtc => new(savedAtUtcTicks, DateTimeKind.Utc);
    }
}
