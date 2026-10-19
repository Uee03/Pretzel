using System;
using Pretzel.Data;
using UnityEngine;

namespace Pretzel.Samples.Data
{
    /// <summary>Schema 2 renamed "name" to "displayName". Old files are migrated on load.</summary>
    [Serializable, SchemaVersion(2)]
    public class PlayerProfile
    {
        public string displayName = "Player";
        public int level = 1;
        public Vector3 position;
        public SerializableGuid id = SerializableGuid.NewGuid();
    }
}
