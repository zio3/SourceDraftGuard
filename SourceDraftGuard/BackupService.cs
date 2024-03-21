using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SourceDraftGuard
{
    internal class BackupService
    {

        public Backuptarget EnumerateBackuptarget(string rootPath,List<Regex> ignoreRegexes)
        {
            var backupTarget = new Backuptarget();
            RecursiveEnumerate(rootPath, backupTarget, ignoreRegexes);
            return backupTarget;
        }

        void RecursiveEnumerate(string path, Backuptarget backupTarget, List<Regex> ignoreRegexes)
        {
            if (IsIgnored(path + "/", ignoreRegexes))
            {
                //ignoreFolders.Add(path);
                return;
            }

            if (Repository.IsValid(path))
            {
                backupTarget.GitFolders.Add(path);
                return;
            }

            backupTarget.Folders.Add(path);

            try
            {
                foreach (var file in Directory.GetFiles(path))
                {
                    if (!IsIgnored(file, ignoreRegexes))
                    {

                        backupTarget.Files.Add(file);
                    }
                    //else
                    //{
                    //    ignoreFiles.Add(file);
                    //}
                }

                foreach (var subDir in Directory.GetDirectories(path))
                {
                    RecursiveEnumerate(subDir, backupTarget, ignoreRegexes);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // アクセス権限がない場合の処理
                Console.WriteLine($"Access denied: {path}");
            }
            catch (PathTooLongException)
            {
                // パスが長すぎる場合の処理
                Console.WriteLine($"Path too long: {path}");
            }
        }

        void ProcessFilesAndFolders(List<string> folders, List<string> files)
        {
            Console.WriteLine("Folders:");
            foreach (var folder in folders)
            {
                if (Repository.IsValid(folder))
                {
                    Console.WriteLine($"[Git] {folder}");
                }
                else
                {
                    Console.WriteLine($"[Non-Git] {folder}");
                }
            }

            Console.WriteLine("\nFiles:");
            foreach (var file in files)
            {
                var folder = Path.GetDirectoryName(file);
                if (Repository.IsValid(folder))
                {
                    Console.WriteLine($"[Git] {file}");
                }
                else
                {
                    Console.WriteLine($"[Non-Git] {file}");
                }
            }
        }

        public List<Regex> LoadGitignoreRegexes(string gitignoreFile)
        {
            var ignorePatterns = LoadGitignorePatterns(gitignoreFile);
            return ignorePatterns.Select(pattern => new Regex(WildcardToRegex(pattern), RegexOptions.Compiled)).ToList();
        }

        List<string> LoadGitignorePatterns(string gitignorePath)
        {
            if (File.Exists(gitignorePath))
            {
                return File.ReadAllLines(gitignorePath)
                    .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                    .Select(line => line.Trim())
                    .ToList();
            }
            return new List<string>();
        }

        bool IsIgnored(string path, List<Regex> ignoreRegexes)
        {
            foreach (var regex in ignoreRegexes)
            {
                if (regex.IsMatch(path))
                {
                    return true;
                }
            }
            return false;
        }
        string WildcardToRegex(string pattern)
        {
            var escapedPattern = Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".")
                .Replace("\\[", "[");

            return "\\\\" + escapedPattern + "$";
        }
    }

    public class Backuptarget
    {
        public List<string> GitFolders { get; set; } = new List<string>();
        public List<string> Folders { get; set; } = new List<string>();
        public List<string> Files { get; set; } = new List<string>();
    }
}
