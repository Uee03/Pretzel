using System;
using UnityEngine;

namespace Pretzel.Data
{
    /// <summary>A <see cref="System.Guid"/> that Unity can serialize, stored as its canonical lowercase string.</summary>
    [Serializable]
    public sealed class SerializableGuid : IEquatable<SerializableGuid>
    {
        /// <summary>Name of the serialized backing field, for editor code that binds to it.</summary>
        public const string GuidFieldName = nameof(_guidString);

        [SerializeField, HideInInspector]
        private string _guidString;

        public string GuidString => _guidString;

        public bool IsEmpty => ToGuid() == Guid.Empty;

        /// <summary>Creates an empty GUID.</summary>
        public SerializableGuid()
        {
            _guidString = Guid.Empty.ToString();
        }

        /// <exception cref="FormatException">The text is not a valid GUID.</exception>
        public SerializableGuid(string guidString)
        {
            _guidString = Guid.Parse(guidString).ToString();
        }

        public SerializableGuid(Guid guid)
        {
            _guidString = guid.ToString();
        }

        public static SerializableGuid NewGuid() => new(Guid.NewGuid());

        public static string NewGuidString() => Guid.NewGuid().ToString();

        public static string EmptyGuidString() => Guid.Empty.ToString();

        public static bool TryParse(string text, out SerializableGuid result)
        {
            if (Guid.TryParse(text, out Guid guid))
            {
                result = new SerializableGuid(guid);
                return true;
            }

            result = null;
            return false;
        }

        public void Regenerate()
        {
            _guidString = Guid.NewGuid().ToString();
        }

        /// <summary>Returns <see cref="Guid.Empty"/> when the stored text is not a valid GUID.</summary>
        public Guid ToGuid() => Guid.TryParse(_guidString, out Guid guid) ? guid : Guid.Empty;

        public override string ToString() => _guidString;

        public bool Equals(SerializableGuid other)
        {
            return other is not null && string.Equals(_guidString, other._guidString, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj) => Equals(obj as SerializableGuid);

        public override int GetHashCode()
        {
            return _guidString == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(_guidString);
        }

        public static bool operator ==(SerializableGuid left, SerializableGuid right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(SerializableGuid left, SerializableGuid right) => !(left == right);

        public static implicit operator string(SerializableGuid guid) => guid?.GuidString;

        public static implicit operator Guid(SerializableGuid guid) => guid?.ToGuid() ?? Guid.Empty;

        public static implicit operator SerializableGuid(Guid guid) => new(guid);

        /// <summary>Explicit because a string can be malformed.</summary>
        public static explicit operator SerializableGuid(string guidString) => new(guidString);
    }
}
