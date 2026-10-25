using System;
using Pretzel.Data;
using UnityEngine;
using UnityEngine.Scripting;

namespace Pretzel.Samples.Data
{
    /// <summary>Schema 2 renamed "name" to "displayName". Old files are migrated on load.</summary>
    // Preserve keeps the type and constructor Newtonsoft needs when IL2CPP strips code.
    [Serializable, Preserve, SchemaVersion(2)]
    public sealed class PlayerProfile
    {
        public string displayName = "Player";
        public int level = 1;
        public Vector3 position;
        public SerializableGuid id = SerializableGuid.NewGuid();
    }
}
