using System;
using System.IO;

namespace Pretzel.Data
{
    internal static class DataFileCommit
    {
        internal static string RollbackPath(string path) => path + ".rollback";

        internal static void Recover(string path)
        {
            string rollback = RollbackPath(path);
            if (!File.Exists(rollback))
            {
                return;
            }

            if (!File.Exists(path))
            {
                File.Move(rollback, path);
            }
            else
            {
                File.Delete(rollback);
            }
        }

        internal static void Commit(string temp, string path, string backup)
        {
            if (!File.Exists(path))
            {
                File.Move(temp, path);
                return;
            }

            try
            {
                File.Replace(temp, path, backup);
            }
            catch (Exception ex) when (ex is IOException or NotSupportedException)
            {
                CommitFallback(temp, path, backup);
            }
        }

        internal static void CommitFallback(string temp, string path, string backup, Action<string, string> move = null)
        {
            move ??= File.Move;
            // File.Replace can fail after it has already moved the original away.
            if (!File.Exists(path))
            {
                move(temp, path);
                return;
            }

            string rollback = RollbackPath(path);
            if (backup != null)
            {
                File.Copy(path, backup, true);
            }

            // Keep the old file until the new file is committed, even with KeepBackup disabled.
            move(path, rollback);
            try
            {
                move(temp, path);
            }
            catch
            {
                if (!File.Exists(path))
                {
                    File.Move(rollback, path);
                }
                throw;
            }
            File.Delete(rollback);
        }
    }
}
