using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SourceDraftGuard
{
    internal class FileCopyService
    {
        private readonly LogService _log;

        public FileCopyService(LogService log)
        {
            _log = log;
        }

        public void BackupFiles(string sourceRootPath, string backupRootPath, IEnumerable<string> backupPaths)
        {
            foreach (string backupPath in backupPaths)
            {
                // ディレクトリはスキップ
                if (Directory.Exists(backupPath))
                {
                    continue;
                }

                string relativePath = Path.GetRelativePath(sourceRootPath, backupPath);
                string backupFilePath = Path.Combine(backupRootPath, relativePath);

                FileInfo sourceFile = new FileInfo(backupPath);
                FileInfo backupFile = new FileInfo(backupFilePath);

                if (!sourceFile.Exists)
                {
                    _log.Log($"Not found: {backupPath}");
                    continue;
                }

                try
                {
                    bool copyFile = !backupFile.Exists;

                    if (!copyFile)
                    {
                        string sourceHash = CalculateFileHash(sourceFile);
                        string backupHash = CalculateFileHash(backupFile);
                        copyFile = sourceHash != backupHash;
                    }

                    if (copyFile)
                    {
                        // Create the directory if it doesn't exist
                        Directory.CreateDirectory(backupFile.DirectoryName!);

                        sourceFile.CopyTo(backupFilePath, true);
                        backupFile.CreationTime = sourceFile.CreationTime;
                        backupFile.LastAccessTime = sourceFile.LastAccessTime;
                        backupFile.LastWriteTime = sourceFile.LastWriteTime;

                        _log.Log($"Copied: {backupPath} -> {backupFilePath}");
                    }
                }
                catch (IOException ex)
                {
                    _log.Log($"Could not backup file: {backupPath}. Error: {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    _log.Log($"Access denied: {backupPath}. Error: {ex.Message}");
                }
            }
        }

        internal void DeleteUnmatchedBackupFiles(string sourceDirectory, string backupDirectory)
        {
            // バックアップ先のディレクトリ情報を取得
            DirectoryInfo backupDirInfo = new DirectoryInfo(backupDirectory);

            if (!backupDirInfo.Exists)
            {
                return;
            }

            // バックアップ先の直下のファイル一覧を取得
            FileInfo[] backupFiles = backupDirInfo.GetFiles();

            foreach (var file in backupFiles)
            {
                // バックアップ元の同等のファイルパスを構築
                string equivalentSourceFilePath = Path.Combine(sourceDirectory, file.Name);

                // バックアップ元にファイルが存在しなければ削除
                if (!File.Exists(equivalentSourceFilePath))
                {
                    _log.Log($"Deleting unmatched file: {file.FullName}");
                    try
                    {
                        file.Delete();
                    }
                    catch (IOException ex)
                    {
                        _log.Log($"Could not delete file: {file.FullName}. Error: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 空のディレクトリを再帰的に削除する
        /// </summary>
        internal void DeleteEmptyDirectories(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            // 子ディレクトリを先に処理（深さ優先）
            foreach (var subDir in Directory.GetDirectories(directory))
            {
                DeleteEmptyDirectories(subDir);
            }

            // 自身が空なら削除
            if (!Directory.EnumerateFileSystemEntries(directory).Any())
            {
                try
                {
                    Directory.Delete(directory);
                    _log.Log($"Deleted empty directory: {directory}");
                }
                catch (IOException ex)
                {
                    _log.Log($"Could not delete directory: {directory}. Error: {ex.Message}");
                }
            }
        }

        string CalculateFileHash(FileInfo file)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var stream = file.OpenRead())
                {
                    var hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
