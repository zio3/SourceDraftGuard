using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using LibGit2Sharp;
using SourceDraftGuard;

// 多重実行防止用ミューテックス
using var mutex = new Mutex(true, "SourceDraftGuard_SingleInstance", out bool createdNew);
if (!createdNew)
{
    Console.WriteLine("SourceDraftGuard is already running. Exiting.");
    return;
}

string rootFolderPath = @"C:\Users\info\source\repos";
string backupRootPath = @"C:\Users\info\OneDrive\SourceDraftGuard";
var gitignoreFile = ".gitignore";
var defaultIgnoreFile = "defaultIgnore.txt";
var logFilePath = Path.Combine(backupRootPath, "SourceDraftGuard.log");

using var log = new LogService(logFilePath);
log.Log("=== SourceDraftGuard started ===");

var backupService = new BackupService(log);

// デフォルト除外パターンを読み込み
var (defaultIgnoreRegexes, defaultNegateRegexes) = backupService.LoadGitignoreRegexes(defaultIgnoreFile);

// .gitignoreのパターンを読み込み
var (gitIgnoreRegexes, gitNegateRegexes) = backupService.LoadGitignoreRegexes(gitignoreFile);

// パターンをマージ
var ignoreRegexes = defaultIgnoreRegexes.Concat(gitIgnoreRegexes).ToList();
var negateRegexes = defaultNegateRegexes.Concat(gitNegateRegexes).ToList();

var results = backupService.EnumerateBackuptarget(rootFolderPath, ignoreRegexes, negateRegexes);

var fileCopyService = new FileCopyService(log);
var gitService = new GitService(log);

foreach (var folder in results.GitFolders)
{
    var srcFolder = folder; // バックアップ元
    var dstFolder = folder.Replace(rootFolderPath, backupRootPath); // バックアップ先

    var pathList = gitService.GetUnpushedAndTrackedFiles(folder);

    // バックアップ対象がない場合に、バックアップ先のフォルダがあれば削除する
    if (pathList.Length == 0)
    {
        if (Directory.Exists(dstFolder))
        {
            try
            {
                Directory.Delete(dstFolder, true);
                log.Log($"Deleted backup folder (no changes): {dstFolder}");
            }
            catch (IOException ex)
            {
                log.Log($"Could not delete folder: {dstFolder}. Error: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                log.Log($"Access denied to folder: {dstFolder}. Error: {ex.Message}");
            }
        }
        continue;
    }

    fileCopyService.BackupFiles(rootFolderPath, backupRootPath, pathList);

    // バックアップ対象外のファイルを削除する（対象リストに含まれないファイルを削除）
    if (Directory.Exists(dstFolder))
    {
        var pathListSet = new HashSet<string>(pathList, StringComparer.OrdinalIgnoreCase);
        var backupFiles = Directory.EnumerateFiles(dstFolder, "*", SearchOption.AllDirectories);
        foreach (var backupFile in backupFiles)
        {
            var srcFile = backupFile.Replace(backupRootPath, rootFolderPath);
            if (!pathListSet.Contains(srcFile))
            {
                File.Delete(backupFile);
                log.Log($"Deleted non-target file: {backupFile}");
            }
        }

        // 空ディレクトリを削除
        fileCopyService.DeleteEmptyDirectories(dstFolder);
    }
}

fileCopyService.BackupFiles(rootFolderPath, backupRootPath, results.Files);

// 非Gitフォルダのバックアップ先で、バックアップ対象外のファイルを削除
var filesSet = new HashSet<string>(results.Files, StringComparer.OrdinalIgnoreCase);
foreach (var folder in results.Folders)
{
    var srcFolder = folder;
    var destFolder = folder.Replace(rootFolderPath, backupRootPath);

    if (!Directory.Exists(destFolder))
    {
        continue;
    }

    // バックアップ先の直下のファイルをチェック
    foreach (var backupFile in Directory.GetFiles(destFolder))
    {
        var srcFile = backupFile.Replace(backupRootPath, rootFolderPath);
        // バックアップ対象リストに含まれていなければ削除
        if (!filesSet.Contains(srcFile))
        {
            try
            {
                File.Delete(backupFile);
                log.Log($"Deleted non-target file: {backupFile}");
            }
            catch (IOException ex)
            {
                log.Log($"Could not delete file: {backupFile}. Error: {ex.Message}");
            }
        }
    }

    // バックアップ先のサブディレクトリをチェック（除外されたディレクトリを削除）
    foreach (var backupSubDir in Directory.GetDirectories(destFolder))
    {
        var srcSubDir = backupSubDir.Replace(backupRootPath, rootFolderPath);
        // ソースに存在しない、またはバックアップ対象フォルダに含まれていなければ削除
        if (!results.Folders.Contains(srcSubDir) && !results.GitFolders.Contains(srcSubDir))
        {
            try
            {
                Directory.Delete(backupSubDir, true);
                log.Log($"Deleted non-target directory: {backupSubDir}");
            }
            catch (IOException ex)
            {
                log.Log($"Could not delete directory: {backupSubDir}. Error: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                log.Log($"Access denied to directory: {backupSubDir}. Error: {ex.Message}");
            }
        }
    }
}

// バックアップルート全体の空ディレクトリをクリーンアップ
fileCopyService.DeleteEmptyDirectories(backupRootPath);

log.Log("=== SourceDraftGuard completed ===");
