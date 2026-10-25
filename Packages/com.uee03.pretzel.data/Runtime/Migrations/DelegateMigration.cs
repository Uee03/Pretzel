using System;
using Pretzel.Core;

namespace Pretzel.Data
{
    /// <summary>An <see cref="IMigration"/> backed by a delegate.</summary>
    public sealed class DelegateMigration : IMigration
    {
        private readonly Func<string, string> _migrate;

        public int FromVersion { get; }

        public int ToVersion { get; }

        public DelegateMigration(int fromVersion, int toVersion, Func<string, string> migrate)
        {
            if (toVersion <= fromVersion)
            {
                throw new ArgumentException("ToVersion must be greater than FromVersion.", nameof(toVersion));
            }

            FromVersion = fromVersion;
            ToVersion = toVersion;
            _migrate = Guard.NotNull(migrate, nameof(migrate));
        }

        public string Migrate(string payload) => _migrate(payload);
    }
}
