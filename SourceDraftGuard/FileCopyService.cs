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
        public void BackupFiles(string sourceRootPath, string backupRootPath, IEnumerable<string> backupPaths)
        {
            foreach (string backupPath in backupPaths)
            {
                string relativePath = Path.GetRelativePath(sourceRootPath, backupPath);
                string backupFilePath = Path.Combine(backupRootPath, relativePath);



                FileInfo sourceFile = new FileInfo(backupPath);
                FileInfo backupFile = new FileInfo(backupFilePath);

                if (!sourceFile.Exists)
                {
                    Console.WriteLine($"Not found: {backupPath}");
                    continue;
                }


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

                    Console.WriteLine($"Copied: {backupPath} -> {backupFilePath}");
                }
                else
                {
                    Console.WriteLine($"Skipped: {backupPath}");
                }
            }
        }

        internal void DeleteUnmatchedBackupFiles(string sourceDirectory, string backupDirectory)
        {
            // バックアップ先のディレクトリ情報を取得
            DirectoryInfo backupDirInfo = new DirectoryInfo(backupDirectory);

            // バックアップ先の直下のファイル一覧を取得
            FileInfo[] backupFiles = backupDirInfo.GetFiles();

            foreach (var file in backupFiles)
            {
                // バックアップ元の同等のファイルパスを構築
                string equivalentSourceFilePath = Path.Combine(sourceDirectory, file.Name);

                // バックアップ元にファイルが存在しなければ削除
                if (!File.Exists(equivalentSourceFilePath))
                {
                    Console.WriteLine($"Deleting unmatched file: {file.FullName}");
                    try
                    {
                        file.Delete();
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"Could not delete file: {file.FullName}. Error: {ex.Message}");
                    }
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
