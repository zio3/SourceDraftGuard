using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using SourceDraftGuard;

// 使用例
string rootFolderPath = @"C:\Users\info\source\repos";
string backupRootPath = @"C:\Users\info\OneDrive\SourceDraftGuard";
var gitignoreFile = ".gitignore";


var backupService = new BackupService();
var ignoreRegexes = backupService.LoadGitignoreRegexes(gitignoreFile);
var results = backupService.EnumerateBackuptarget(rootFolderPath, ignoreRegexes);

var fileCopyService = new FileCopyService();
var gitService = new GitService();
foreach (var folder in results.GitFolders)
{
    // Console.WriteLine(folder);
    
    var srcFolder = folder; // バックアップ元
    var dstFolder = folder.Replace(rootFolderPath, backupRootPath);// バックアップ先

    var pathList = gitService.GetUnpushedAndTrackedFiles(folder);


    //バックアップ対象がない場合に、バックアップ先のフォルダがあれば削除する
    if(pathList.Length == 0)
    {
        if(Directory.Exists(dstFolder))
        {
            Directory.Delete(dstFolder, true);
        }
        continue;
    }

    fileCopyService.BackupFiles(rootFolderPath, backupRootPath, pathList);

    //バックアップに呑み存在するファイルを削除する
    var backupFiles = Directory.EnumerateFiles(dstFolder, "*", SearchOption.AllDirectories);
    foreach (var backupFile in backupFiles)
    {
        var srcFile = backupFile.Replace(backupRootPath,rootFolderPath);
        if(!File.Exists(srcFile))
        {
            File.Delete(backupFile);
        }
    }
}

fileCopyService.BackupFiles(rootFolderPath, backupRootPath, results.Files);

foreach (var folder in results.Folders)
{
    var srcFolde = folder;
    var destFolder = folder.Replace(rootFolderPath, backupRootPath);

    fileCopyService.DeleteUnmatchedBackupFiles(srcFolde, destFolder); 
}





