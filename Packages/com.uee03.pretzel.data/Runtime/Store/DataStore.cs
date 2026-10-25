using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Pretzel.Core;
using UnityEngine;

namespace Pretzel.Data
{
    /// <summary>
    /// File-backed <see cref="IDataStore"/>. All state is per instance.
    /// The first call must happen on the main thread, because it resolves <see cref="IDataRoot.FullPath"/>.
    /// </summary>
    public sealed class DataStore : IDataStore
    {
        private static readonly UTF8Encoding Utf8 = new(false);

        // Versioned types are written as {"schemaVersion": n, "data": <serializer output>}. This needs a JSON serializer.
        private static readonly Regex EnvelopeStart = new("\\A\\s*\\{\\s*\"schemaVersion\"\\s*:\\s*(\\d+)\\s*,\\s*\"data\"\\s*:");

        private readonly IDataRoot _root;
        private readonly string _extension;
        private readonly bool _prettyPrint;
        private readonly bool _keepBackup;
        private readonly string _slotPrefix;
        private readonly IDataTransform[] _transforms;
        private readonly Dictionary<Type, IMigration[]> _migrations = new();
        private readonly ConcurrentDictionary<Type, int> _versions = new();
        private readonly object _ioLock = new();

        // Last write or delete sequence per path, guarded by _ioLock. Stops a stale background save from replacing a newer one.
        private readonly Dictionary<string, long> _committed = new(StringComparer.Ordinal);
        private long _sequence;

        private volatile string _rootPath;

        public ISerializer Serializer { get; }

        /// <param name="extension">File extension without the dot, for example "json" or "sav".</param>
        public DataStore(IDataRoot root, ISerializer serializer = null, string extension = "json", DataStoreOptions options = null)
        {
            _root = Guard.NotNull(root, nameof(root));
            Serializer = serializer ?? new NewtonsoftSerializer();

            string cleanExtension = (extension ?? string.Empty).TrimStart('.');
            string error = DataNames.ValidateName(cleanExtension);
            if (error != null)
            {
                throw new ArgumentException($"Extension: {error}", nameof(extension));
            }

            _extension = "." + cleanExtension;

            options ??= new DataStoreOptions();
            _prettyPrint = options.PrettyPrint;
            _keepBackup = options.KeepBackup;
            _slotPrefix = string.IsNullOrEmpty(options.SlotPrefix) ? "Slot_" : options.SlotPrefix;
            string slotError = DataNames.ValidateName(_slotPrefix + "0");
            if (slotError != null)
            {
                throw new ArgumentException($"SlotPrefix: {slotError}", nameof(options));
            }

            _transforms = options.Transforms.ToArray();
            foreach (KeyValuePair<Type, List<IMigration>> pair in options.Migrations)
            {
                _migrations[pair.Key] = pair.Value.ToArray();
            }
        }

        #region SYNC

        public void Save<T>(DataKey key, T value)
        {
            string path = PathOf(key);
            WriteText(path, BuildText(value), NextSequence());
        }

        public LoadResult<T> Load<T>(DataKey key)
        {
            string path = PathOf(key);
            return Resolve<T>(ReadRaw(path), null, path, default, false);
        }

        public bool TryLoad<T>(DataKey key, out T value)
        {
            LoadResult<T> result = Load<T>(key);
            value = result.Value;
            return result.Ok;
        }

        public LoadResult<T> LoadInto<T>(DataKey key, T target) where T : class
        {
            Guard.NotNull(target, nameof(target));
            string path = PathOf(key);
            return Resolve(ReadRaw(path), null, path, target, true);
        }

        public bool Exists(DataKey key)
        {
            string path = PathOf(key);
            return ExistsFile(path);
        }

        private bool ExistsFile(string path)
        {
            lock (_ioLock)
            {
                DataFileCommit.Recover(path);
                return File.Exists(path) || (_keepBackup && File.Exists(BackupPath(path)));
            }
        }

        public bool Delete(DataKey key) => DeleteFile(PathOf(key), NextSequence());

        private bool DeleteFile(string path, long sequence)
        {
            lock (_ioLock)
            {
                if (!TryClaim(path, sequence))
                {
                    return false;
                }

                if (!Directory.Exists(Path.GetDirectoryName(path)))
                {
                    return false;
                }

                DataFileCommit.Recover(path);
                bool existed = File.Exists(path);
                File.Delete(path);
                File.Delete(BackupPath(path));
                File.Delete(TempPath(path));
                File.Delete(DataFileCommit.RollbackPath(path));
                return existed;
            }
        }

        public IReadOnlyList<DataKey> List(string folder = null, int? slot = null)
        {
            return ListCore(DirectoryOf(folder, slot), folder, slot);
        }

        public IReadOnlyList<int> ListSlots(string folder = null)
        {
            return ListSlotsCore(DirectoryOf(folder, null));
        }

        #endregion

        #region ASYNC

        public async Awaitable SaveAsync<T>(DataKey key, T value, CancellationToken cancellationToken = default)
        {
            string path = PathOf(key);
            // Serialize on the main thread, because JsonUtility is main-thread only.
            string text = BuildText(value);
            long sequence = NextSequence();
            await OnBackground(() =>
            {
                WriteText(path, text, sequence);
                return true;
            }, cancellationToken);
        }

        public async Awaitable<LoadResult<T>> LoadAsync<T>(DataKey key, CancellationToken cancellationToken = default)
        {
            string path = PathOf(key);
            (RawRead main, RawRead? backup) = await OnBackground(() => ReadBoth(path), cancellationToken);
            return Resolve<T>(main, backup, path, default, false);
        }

        public async Awaitable<LoadResult<T>> LoadIntoAsync<T>(DataKey key, T target, CancellationToken cancellationToken = default) where T : class
        {
            Guard.NotNull(target, nameof(target));
            string path = PathOf(key);
            (RawRead main, RawRead? backup) = await OnBackground(() => ReadBoth(path), cancellationToken);
            return Resolve(main, backup, path, target, true);
        }

        public Awaitable<bool> ExistsAsync(DataKey key, CancellationToken cancellationToken = default)
        {
            string path = PathOf(key);
            return OnBackground(() => ExistsFile(path), cancellationToken);
        }

        public Awaitable<bool> DeleteAsync(DataKey key, CancellationToken cancellationToken = default)
        {
            string path = PathOf(key);
            long sequence = NextSequence();
            return OnBackground(() => DeleteFile(path, sequence), cancellationToken);
        }

        public Awaitable<IReadOnlyList<DataKey>> ListAsync(string folder = null, int? slot = null, CancellationToken cancellationToken = default)
        {
            string directory = DirectoryOf(folder, slot);
            return OnBackground(() => ListCore(directory, folder, slot), cancellationToken);
        }

        public Awaitable<IReadOnlyList<int>> ListSlotsAsync(string folder = null, CancellationToken cancellationToken = default)
        {
            string directory = DirectoryOf(folder, null);
            return OnBackground(() => ListSlotsCore(directory), cancellationToken);
        }

        private static async Awaitable<TResult> OnBackground<TResult>(Func<TResult> work, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Awaitable.BackgroundThreadAsync();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return work();
            }
            finally
            {
                await Awaitable.MainThreadAsync();
            }
        }

        #endregion

        #region PATHS

        private string RootPath
        {
            get
            {
                string path = _rootPath;
                if (path == null)
                {
                    path = _root.FullPath;
                    _rootPath = path;
                }

                return path;
            }
        }

        private string DirectoryOf(string folder, int? slot)
        {
            string error = DataNames.ValidateFolder(folder);
            if (error != null)
            {
                throw new ArgumentException(error, nameof(folder));
            }

            if (slot.HasValue && slot.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), slot.Value, "Slot must be 0 or greater.");
            }

            string directory = RootPath;
            if (!string.IsNullOrEmpty(folder))
            {
                directory = Path.Combine(directory, folder.Replace('/', Path.DirectorySeparatorChar));
            }

            return slot.HasValue ? Path.Combine(directory, _slotPrefix + slot.Value) : directory;
        }

        private string PathOf(DataKey key)
        {
            if (!key.IsValid)
            {
                throw new ArgumentException("The key is not initialized. Create it with new DataKey(name).", nameof(key));
            }

            return Path.Combine(DirectoryOf(key.Folder, key.Slot), key.Name + _extension);
        }

        private static string BackupPath(string path) => path + ".bak";

        private static string TempPath(string path) => path + ".tmp";

        #endregion

        #region WRITE

        private string BuildText<T>(T value)
        {
            int version = VersionOf(typeof(T));
            if (version == 0)
            {
                return Serializer.Serialize(value, _prettyPrint);
            }

            string payload = Serializer.Serialize(value, _prettyPrint);
            if (_prettyPrint)
            {
                // JSON strings never contain raw line breaks, so indenting every line is safe.
                payload = payload.Replace("\n", "\n  ");
                return "{\n  \"schemaVersion\": " + version + ",\n  \"data\": " + payload + "\n}";
            }

            return "{\"schemaVersion\":" + version + ",\"data\":" + payload + "}";
        }

        private long NextSequence() => Interlocked.Increment(ref _sequence);

        // Call inside _ioLock. False when a newer write or delete of the same path has already run.
        private bool TryClaim(string path, long sequence)
        {
            if (_committed.TryGetValue(path, out long last) && last > sequence)
            {
                return false;
            }

            _committed[path] = sequence;
            return true;
        }

        private void WriteText(string path, string text, long sequence)
        {
            byte[] bytes = Utf8.GetBytes(text);
            foreach (IDataTransform transform in _transforms)
            {
                bytes = transform.Encode(bytes);
            }

            string temp = TempPath(path);
            lock (_ioLock)
            {
                if (!TryClaim(path, sequence))
                {
                    return;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                DataFileCommit.Recover(path);
                using (FileStream stream = new(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                DataFileCommit.Commit(temp, path, _keepBackup ? BackupPath(path) : null);
            }
        }

        #endregion

        #region READ

        private readonly struct RawRead
        {
            public readonly bool Exists;
            public readonly string Text;
            public readonly string Error;
            public readonly DataError ErrorCode;

            public RawRead(bool exists, string text, string error, DataError errorCode = DataError.None)
            {
                Exists = exists;
                Text = text;
                Error = error;
                ErrorCode = errorCode;
            }
        }

        private (RawRead, RawRead?) ReadBoth(string path)
        {
            RawRead main = ReadRaw(path);
            RawRead? backup = _keepBackup ? ReadRaw(BackupPath(path)) : null;
            return (main, backup);
        }

        private RawRead ReadRaw(string path)
        {
            byte[] bytes;
            lock (_ioLock)
            {
                try
                {
                    DataFileCommit.Recover(path);
                    if (!File.Exists(path))
                    {
                        return new RawRead(false, null, null);
                    }
                    bytes = File.ReadAllBytes(path);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    return new RawRead(true, null, ex.Message, DataError.IoFailure);
                }
            }

            try
            {
                for (int i = _transforms.Length - 1; i >= 0; i--)
                {
                    bytes = _transforms[i].Decode(bytes);
                }

                bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                string text = hasBom ? Utf8.GetString(bytes, 3, bytes.Length - 3) : Utf8.GetString(bytes);
                return new RawRead(true, text, null);
            }
            catch (Exception ex) when (IsReadFailure(ex))
            {
                return new RawRead(true, null, ex.Message, ErrorOf(ex));
            }
        }

        // Runs on the main thread, because JsonUtility needs it.
        private LoadResult<T> Resolve<T>(RawRead main, RawRead? preReadBackup, string path, T target, bool populate)
        {
            string mainError = null;
            DataError mainCode = DataError.None;
            if (main.Exists)
            {
                if (main.Error == null)
                {
                    LoadResult<T> parsed = Parse(main.Text, target, populate);
                    if (parsed.Ok)
                    {
                        return parsed;
                    }

                    mainError = parsed.Error;
                    mainCode = parsed.ErrorCode;
                }
                else
                {
                    mainError = main.Error;
                    mainCode = main.ErrorCode;
                }
            }

            // A newer schema, unknown format or missing key also fails on the backup, and restoring an older backup would downgrade the save.
            if (!_keepBackup || mainCode is DataError.UnsupportedFormat or DataError.SchemaMismatch or DataError.MissingKey)
            {
                return main.Exists ? LoadResult<T>.Corrupt(mainError, mainCode) : LoadResult<T>.NotFound();
            }

            RawRead backup = preReadBackup ?? ReadRaw(BackupPath(path));
            if (!backup.Exists)
            {
                return main.Exists ? LoadResult<T>.Corrupt(mainError, mainCode) : LoadResult<T>.NotFound();
            }

            if (backup.Error != null)
            {
                return LoadResult<T>.Corrupt(mainError ?? backup.Error, mainError == null ? backup.ErrorCode : mainCode);
            }

            LoadResult<T> recovered = Parse(backup.Text, target, populate);
            if (!recovered.Ok)
            {
                return LoadResult<T>.Corrupt(mainError ?? recovered.Error, mainError == null ? recovered.ErrorCode : mainCode);
            }

            // A locked or unreadable main file may still hold the newest save, so only repair a missing or corrupt one.
            if (mainCode != DataError.IoFailure)
            {
                RestoreFromBackup(path);
            }

            return LoadResult<T>.Success(recovered.Value, true);
        }

        private void RestoreFromBackup(string path)
        {
            // Best effort. Without it the next save would overwrite the good .bak with the corrupt file.
            try
            {
                lock (_ioLock)
                {
                    File.Copy(BackupPath(path), path, true);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Pretzel.Data] Could not restore '{path}' from its backup: {ex.Message}");
            }
        }

        private LoadResult<T> Parse<T>(string text, T target, bool populate)
        {
            try
            {
                string payload = Unwrap(typeof(T), text);
                if (populate)
                {
                    // Reject malformed JSON before it can partly overwrite the live target.
                    if (Serializer is NewtonsoftSerializer or JsonUtilitySerializer)
                    {
                        Newtonsoft.Json.Linq.JToken.Parse(payload);
                    }

                    Serializer.Populate(payload, target);
                    return LoadResult<T>.Success(target);
                }

                T value = Serializer.Deserialize<T>(payload);
                return value is null ? LoadResult<T>.Corrupt("The file contained no data.") : LoadResult<T>.Success(value);
            }
            catch (Exception ex) when (IsReadFailure(ex))
            {
                return LoadResult<T>.Corrupt(ex.Message, ErrorOf(ex));
            }
        }

        private static bool IsReadFailure(Exception ex)
        {
            return ex is DataReadException or IOException or UnauthorizedAccessException or InvalidDataException
                or CryptographicException or NotSupportedException or Newtonsoft.Json.JsonException
                or FormatException or ArgumentException or InvalidOperationException;
        }

        private static DataError ErrorOf(Exception ex)
        {
            return ex switch
            {
                DataReadException failure => failure.Reason,
                CryptographicException => DataError.AuthenticationFailed,
                NotSupportedException => DataError.UnsupportedFormat,
                InvalidDataException or EndOfStreamException => DataError.InvalidData,
                IOException or UnauthorizedAccessException => DataError.IoFailure,
                _ => DataError.InvalidData
            };
        }

        #endregion

        #region SCHEMA

        private int VersionOf(Type type)
        {
            return _versions.GetOrAdd(type, t => t.GetCustomAttribute<SchemaVersionAttribute>(false)?.Version ?? 0);
        }

        private string Unwrap(Type type, string text)
        {
            int current = VersionOf(type);
            if (current == 0)
            {
                return text;
            }

            Newtonsoft.Json.Linq.JToken.Parse(text);
            int version = 0;
            string payload = text;
            Match match = EnvelopeStart.Match(text);
            int end = text.LastIndexOf('}');
            if (match.Success && end > match.Length)
            {
                if (!int.TryParse(match.Groups[1].Value, out version))
                {
                    throw new DataReadException(DataError.InvalidData, $"{type.Name} has an invalid schema version.");
                }

                payload = text.Substring(match.Length, end - match.Length).Trim();
            }
            // Otherwise the file is unversioned data from before the type had a schema version.

            if (version > current)
            {
                throw new DataReadException(DataError.SchemaMismatch, $"{type.Name} was saved with schema {version}, but this build supports up to {current}.");
            }

            _migrations.TryGetValue(type, out IMigration[] steps);
            while (version < current)
            {
                IMigration step = steps == null ? null : Array.Find(steps, m => m.FromVersion == version);
                if (step == null)
                {
                    throw new DataReadException(DataError.SchemaMismatch, $"No migration registered for {type.Name} from schema {version} to {current}.");
                }

                payload = step.Migrate(payload);
                version = step.ToVersion;
            }

            return payload;
        }

        #endregion

        #region LIST

        private IReadOnlyList<DataKey> ListCore(string directory, string folder, int? slot)
        {
            List<DataKey> keys = new();
            if (!Directory.Exists(directory))
            {
                return keys;
            }

            lock (_ioLock)
            {
                foreach (string rollback in Directory.EnumerateFiles(directory, "*" + _extension + ".rollback", SearchOption.TopDirectoryOnly))
                {
                    DataFileCommit.Recover(rollback.Substring(0, rollback.Length - ".rollback".Length));
                }
            }

            foreach (string file in Directory.EnumerateFiles(directory, "*" + _extension, SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(file);
                if (!fileName.EndsWith(_extension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string name = fileName.Substring(0, fileName.Length - _extension.Length);
                if (DataNames.ValidateName(name) == null)
                {
                    keys.Add(new DataKey(name, slot, folder));
                }
            }

            keys.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return keys;
        }

        private IReadOnlyList<int> ListSlotsCore(string directory)
        {
            List<int> slots = new();
            if (!Directory.Exists(directory))
            {
                return slots;
            }

            foreach (string path in Directory.EnumerateDirectories(directory))
            {
                string name = Path.GetFileName(path);
                if (name.StartsWith(_slotPrefix, StringComparison.Ordinal)
                    && int.TryParse(name.Substring(_slotPrefix.Length), out int slot)
                    && slot >= 0)
                {
                    slots.Add(slot);
                }
            }

            slots.Sort();
            return slots;
        }

        #endregion
    }
}
