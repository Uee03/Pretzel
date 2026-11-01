using System.Collections.Generic;

namespace Pretzel.Setup
{
    /// <summary>What <see cref="PretzelSetup.GenerateComposition"/> did.</summary>
    public sealed class CompositionResult
    {
        public CompositionResult(IReadOnlyList<string> writtenFiles, IReadOnlyList<string> assetLog, bool assetsPending)
        {
            WrittenFiles = writtenFiles;
            AssetLog = assetLog;
            AssetsPending = assetsPending;
        }

        /// <summary>Source files that were created or rewritten. Empty when everything was already up to date.</summary>
        public IReadOnlyList<string> WrittenFiles { get; }

        /// <summary>One line per asset that was created or repaired. Empty while <see cref="AssetsPending"/>.</summary>
        public IReadOnlyList<string> AssetLog { get; }

        /// <summary>True when new code was written: the installer, scope prefab and settings asset are created after Unity compiles it.</summary>
        public bool AssetsPending { get; }
    }
}
