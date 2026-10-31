namespace Pretzel.Multiplayer
{
    /// <summary>Persists the last session so a relaunch can still offer to rejoin it. Implement it to use another storage than PlayerPrefs.</summary>
    public interface IReconnectStore
    {
        /// <summary>Returns the stored record, or null when there is none or it cannot be read.</summary>
        ReconnectRecord Load();

        void Save(ReconnectRecord record);

        void Clear();
    }
}
