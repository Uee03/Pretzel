namespace Pretzel.Data
{
    /// <summary>Where <see cref="DataIOConfig"/> looks for encryption keys. Pick one explicitly for a shipped game.</summary>
    public enum KeySource
    {
        /// <summary>Provider, environment variables, the Editor-only key file, then the bundled JSON. Handy while developing.</summary>
        Automatic,

        /// <summary><see cref="EncryptionKeys.Provider"/> only: the generated embedded setup, a token flow, or your own code.</summary>
        Provider,

        /// <summary>Environment variables only. Players do not have these, so use it for servers and tools.</summary>
        Environment,

        /// <summary>The bundled <see cref="GameEncryptionKeyConfig"/> JSON only.</summary>
        BundledJson
    }
}
