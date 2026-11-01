using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pretzel.Sfx
{
    /// <summary>The sounds of a game by name. Designers fill it in the Inspector; code plays a sound by its id.</summary>
    [CreateAssetMenu(menuName = "Pretzel/Audio/SFX Library", fileName = "SfxLibrary")]
    public sealed class SfxLibrary : ScriptableObject
    {
        [SerializeField] private List<SfxEntry> _entries = new();

        [NonSerialized] private Dictionary<string, SfxEntry> _byId;

        public IReadOnlyList<SfxEntry> Entries => _entries;

        public bool TryGet(string id, out SfxEntry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            _byId ??= Index();
            return _byId.TryGetValue(id, out entry);
        }

        /// <summary>Replaces the entries from code, for tests and generated content.</summary>
        public void SetEntries(IEnumerable<SfxEntry> entries)
        {
            _entries = new List<SfxEntry>(entries);
            _byId = null;
        }

        private void OnValidate()
        {
            _byId = null;
            HashSet<string> seen = new(StringComparer.Ordinal);
            foreach (SfxEntry entry in _entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Id))
                {
                    continue;
                }

                if (!seen.Add(entry.Id))
                {
                    Debug.LogWarning($"SfxLibrary '{name}' has the id '{entry.Id}' twice. The first entry is used.", this);
                }
            }
        }

        private Dictionary<string, SfxEntry> Index()
        {
            Dictionary<string, SfxEntry> index = new(StringComparer.Ordinal);
            foreach (SfxEntry entry in _entries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.Id))
                {
                    index.TryAdd(entry.Id, entry);
                }
            }

            return index;
        }
    }
}
