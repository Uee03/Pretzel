using System;

namespace Pretzel.Data
{
    /// <summary>
    /// Identifies one saved item: a name, an optional slot and an optional sub-folder.
    /// The constructor validates every part, so a key cannot point outside the store root.
    /// </summary>
    // Value type: equality by name, slot and folder; used as a dictionary key.
    public readonly struct DataKey : IEquatable<DataKey>
    {
        public string Name { get; }

        /// <summary>Save slot, or null when the item is not slotted. Must be 0 or greater.</summary>
        public int? Slot { get; }

        /// <summary>Relative sub-folder using '/' separators, or null.</summary>
        public string Folder { get; }

        public bool IsValid => Name != null;

        public DataKey(string name, int? slot = null, string folder = null)
        {
            string error = DataNames.ValidateName(name);
            if (error != null)
            {
                throw new ArgumentException(error, nameof(name));
            }

            if (slot.HasValue && slot.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), slot.Value, "Slot must be 0 or greater.");
            }

            error = DataNames.ValidateFolder(folder);
            if (error != null)
            {
                throw new ArgumentException(error, nameof(folder));
            }

            Name = name;
            Slot = slot;
            Folder = string.IsNullOrEmpty(folder) ? null : folder;
        }

        public DataKey WithSlot(int? slot) => new(Name, slot, Folder);

        public DataKey WithFolder(string folder) => new(Name, Slot, folder);

        public static implicit operator DataKey(string name) => new(name);

        public bool Equals(DataKey other)
        {
            return string.Equals(Name, other.Name, StringComparison.Ordinal)
                   && Slot == other.Slot
                   && string.Equals(Folder, other.Folder, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is DataKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Name, Slot, Folder);

        public static bool operator ==(DataKey left, DataKey right) => left.Equals(right);

        public static bool operator !=(DataKey left, DataKey right) => !left.Equals(right);

        public override string ToString()
        {
            if (!IsValid)
            {
                return "(invalid key)";
            }

            string folder = Folder == null ? string.Empty : Folder + "/";
            string slot = Slot.HasValue ? $"[slot {Slot.Value}]" : string.Empty;
            return folder + Name + slot;
        }
    }
}
