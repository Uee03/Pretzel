using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Pretzel.Extensions
{
    /// <summary>
    /// File helpers that read and write through a path string, such as <c>path.WriteText("hello")</c>.
    /// An invalid path logs an error and does nothing, and reads return an empty result.
    /// </summary>
    public static class IOExtensions
    {
        #region FILE IO

        /// <summary>
        /// Write text to a file.
        /// </summary>
        /// <param name="path">A file path</param>
        /// <param name="data">The text to write</param>
        /// <param name="encodingType">Default Encoding used is UTF8</param>
        public static void WriteText(this string path, string data, EncodingType encodingType = EncodingType.UTF8)
        {
            if (TryValidate(path))
            {
                File.WriteAllText(path, data, encodingType.ToEncoding());
            }
        }

        /// <summary>
        /// Write bytes to a file.
        /// </summary>
        public static void WriteBytes(this string path, byte[] data)
        {
            if (TryValidate(path))
            {
                File.WriteAllBytes(path, data);
            }
        }

        /// <summary>
        /// Write lines to a file.
        /// </summary>
        public static void WriteLines(this string path, IEnumerable<string> data, EncodingType encodingType = EncodingType.UTF8)
        {
            if (TryValidate(path))
            {
                File.WriteAllLines(path, data, encodingType.ToEncoding());
            }
        }

        /// <summary>
        /// Read all text from a file.
        /// </summary>
        public static string ReadText(this string path, EncodingType encodingType = EncodingType.UTF8)
        {
            return TryValidate(path) ? File.ReadAllText(path, encodingType.ToEncoding()) : string.Empty;
        }

        /// <summary>
        /// Read all bytes from a file.
        /// </summary>
        public static byte[] ReadBytes(this string path)
        {
            return TryValidate(path) ? File.ReadAllBytes(path) : System.Array.Empty<byte>();
        }

        /// <summary>
        /// Read the lines of a file lazily. The file stays open until the enumeration finishes or is disposed.
        /// </summary>
        public static IEnumerable<string> ReadLines(this string path, EncodingType encodingType = EncodingType.UTF8)
        {
            return TryValidate(path) ? File.ReadLines(path, encodingType.ToEncoding()) : System.Array.Empty<string>();
        }

        public static Task WriteTextAsync(this string path, string data, EncodingType encodingType = EncodingType.UTF8, CancellationToken cancellationToken = default)
        {
            return TryValidate(path) ? File.WriteAllTextAsync(path, data, encodingType.ToEncoding(), cancellationToken) : Task.CompletedTask;
        }

        public static Task WriteBytesAsync(this string path, byte[] data, CancellationToken cancellationToken = default)
        {
            return TryValidate(path) ? File.WriteAllBytesAsync(path, data, cancellationToken) : Task.CompletedTask;
        }

        public static Task WriteLinesAsync(this string path, IEnumerable<string> data, EncodingType encodingType = EncodingType.UTF8, CancellationToken cancellationToken = default)
        {
            return TryValidate(path) ? File.WriteAllLinesAsync(path, data, encodingType.ToEncoding(), cancellationToken) : Task.CompletedTask;
        }

        public static Task<string> ReadTextAsync(this string path, EncodingType encodingType = EncodingType.UTF8, CancellationToken cancellationToken = default)
        {
            return TryValidate(path) ? File.ReadAllTextAsync(path, encodingType.ToEncoding(), cancellationToken) : Task.FromResult(string.Empty);
        }

        public static Task<byte[]> ReadBytesAsync(this string path, CancellationToken cancellationToken = default)
        {
            return TryValidate(path) ? File.ReadAllBytesAsync(path, cancellationToken) : Task.FromResult(System.Array.Empty<byte>());
        }

        public static Task<string[]> ReadLinesAsync(this string path, EncodingType encodingType = EncodingType.UTF8, CancellationToken cancellationToken = default)
        {
            return TryValidate(path) ? File.ReadAllLinesAsync(path, encodingType.ToEncoding(), cancellationToken) : Task.FromResult(System.Array.Empty<string>());
        }

        #endregion

        #region STREAM FILE IO

        /// <summary>
        /// Write text through a stream, which allows appending and controlling how the file is shared.
        /// </summary>
        /// <param name="path">A file path</param>
        /// <param name="data">The text to write</param>
        /// <param name="fileMode">Use FileMode.Append to add to the end of a file. Default is Create</param>
        /// <param name="fileAccess">Default is Write</param>
        /// <param name="fileShare">Default is None, so other processes cannot open the file while it is written</param>
        /// <param name="encodingType">Default Encoding used is UTF8</param>
        public static void WriteWithStream(
            this string path,
            string data,
            FileMode fileMode = FileMode.Create,
            FileAccess fileAccess = FileAccess.Write,
            FileShare fileShare = FileShare.None,
            EncodingType encodingType = EncodingType.UTF8)
        {
            if (!TryValidate(path))
            {
                return;
            }

            using FileStream stream = new FileStream(path, fileMode, fileAccess, fileShare);
            using StreamWriter writer = new StreamWriter(stream, encodingType.ToEncoding());
            writer.Write(data);
        }

        public static async Task WriteWithStreamAsync(
            this string path,
            string data,
            FileMode fileMode = FileMode.Create,
            FileAccess fileAccess = FileAccess.Write,
            FileShare fileShare = FileShare.None,
            EncodingType encodingType = EncodingType.UTF8)
        {
            if (!TryValidate(path))
            {
                return;
            }

            await using FileStream stream = new FileStream(path, fileMode, fileAccess, fileShare);
            await using StreamWriter writer = new StreamWriter(stream, encodingType.ToEncoding());
            await writer.WriteAsync(data);
        }

        /// <summary>
        /// Read text through a stream. Other processes may still read the file by default.
        /// </summary>
        public static string ReadWithStream(
            this string path,
            FileMode fileMode = FileMode.Open,
            FileAccess fileAccess = FileAccess.Read,
            FileShare fileShare = FileShare.Read,
            EncodingType encodingType = EncodingType.UTF8)
        {
            if (!TryValidate(path))
            {
                return string.Empty;
            }

            using FileStream stream = new FileStream(path, fileMode, fileAccess, fileShare);
            using StreamReader reader = new StreamReader(stream, encodingType.ToEncoding());
            return reader.ReadToEnd();
        }

        public static async Task<string> ReadWithStreamAsync(
            this string path,
            FileMode fileMode = FileMode.Open,
            FileAccess fileAccess = FileAccess.Read,
            FileShare fileShare = FileShare.Read,
            EncodingType encodingType = EncodingType.UTF8)
        {
            if (!TryValidate(path))
            {
                return string.Empty;
            }

            await using FileStream stream = new FileStream(path, fileMode, fileAccess, fileShare);
            using StreamReader reader = new StreamReader(stream, encodingType.ToEncoding());
            return await reader.ReadToEndAsync();
        }

        #endregion

        #region HELPER METHODS

        /// <summary>
        /// Check that a path is not empty, has no illegal characters and ends in a file name.
        /// </summary>
        public static bool IsPathValid(this string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                   && path.IndexOfAny(Path.GetInvalidPathChars()) < 0
                   && !string.IsNullOrEmpty(Path.GetFileName(path));
        }

        private static bool TryValidate(string path)
        {
            if (IsPathValid(path))
            {
                return true;
            }

            Debug.unityLogger.LogError("IO Exception", $"Path is invalid: '{path}'");
            return false;
        }

        #endregion
    }
}
