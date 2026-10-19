using System;
using System.Collections.Generic;
using Pretzel.Core;

namespace Pretzel.Data
{
    /// <summary>Settings for a <see cref="DataStore"/>. The store takes a snapshot when it is constructed.</summary>
    public sealed class DataStoreOptions
    {
        /// <summary>Indent the JSON so it can be read and diffed.</summary>
        public bool PrettyPrint { get; set; }

        /// <summary>Keep the previous file as .bak on every save, and recover from it when the main file is corrupt.</summary>
        public bool KeepBackup { get; set; } = true;

        /// <summary>Prefix of slot folders. A slot of 2 becomes "Slot_2".</summary>
        public string SlotPrefix { get; set; } = "Slot_";

        /// <summary>Byte transforms applied in list order on save and in reverse order on load.</summary>
        public List<IDataTransform> Transforms { get; } = new();

        internal Dictionary<Type, List<IMigration>> Migrations { get; } = new();

        /// <summary>Registers a migration step for <typeparamref name="T"/>. Pair it with <see cref="SchemaVersionAttribute"/>.</summary>
        public DataStoreOptions AddMigration<T>(IMigration migration)
        {
            Guard.NotNull(migration, nameof(migration));
            if (!Migrations.TryGetValue(typeof(T), out List<IMigration> list))
            {
                list = new List<IMigration>();
                Migrations[typeof(T)] = list;
            }

            list.Add(migration);
            return this;
        }
    }
}
