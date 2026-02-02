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
        private readonly LogService _log;

        public BackupService(LogService log)
        {
            _log = log;
        }

        public Backuptarget EnumerateBackuptarget(string rootPath, List<Regex> ignoreRegexes, List<Regex> negateRegexes)
        {
            var backupTarget = new Backuptarget();
            RecursiveEnumerate(rootPath, backupTarget, ignoreRegexes, negateRegexes);
            return backupTarget;
        }

        void RecursiveEnumerate(string path, Backuptarget backupTarget, List<Regex> ignoreRegexes, List<Regex> negateRegexes)
        {
            // パス区切りを正規化してチェック（フォルダは末尾に\を付けてマッチ）
            var normalizedPath = path.Replace("/", "\\");
            if (IsIgnored(normalizedPath + "\\", ignoreRegexes, negateRegexes))
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
                    var normalizedFile = file.Replace("/", "\\");
                    if (!IsIgnored(normalizedFile, ignoreRegexes, negateRegexes))
                    {
                        backupTarget.Files.Add(file);
                    }
                }

                foreach (var subDir in Directory.GetDirectories(path))
                {
                    RecursiveEnumerate(subDir, backupTarget, ignoreRegexes, negateRegexes);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // アクセス権限がない場合の処理
                _log.Log($"Access denied: {path}");
            }
            catch (PathTooLongException)
            {
                // パスが長すぎる場合の処理
                _log.Log($"Path too long: {path}");
            }
        }

        public (List<Regex> ignoreRegexes, List<Regex> negateRegexes) LoadGitignoreRegexes(string gitignoreFile)
        {
            var allPatterns = LoadGitignorePatterns(gitignoreFile);

            // 否定パターン（!で始まる）と通常パターンを分離
            var negatePatterns = allPatterns
                .Where(p => p.StartsWith("!"))
                .Select(p => p.Substring(1)) // !を除去
                .Select(pattern => new Regex(WildcardToRegex(pattern), RegexOptions.Compiled))
                .ToList();

            var ignorePatterns = allPatterns
                .Where(p => !p.StartsWith("!"))
                .Select(pattern => new Regex(WildcardToRegex(pattern), RegexOptions.Compiled))
                .ToList();

            return (ignorePatterns, negatePatterns);
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

        bool IsIgnored(string path, List<Regex> ignoreRegexes, List<Regex> negateRegexes)
        {
            // まず否定パターンでマッチするか確認（マッチすれば除外しない）
            foreach (var regex in negateRegexes)
            {
                if (regex.IsMatch(path))
                {
                    return false;
                }
            }

            // 通常の除外パターンでマッチするか確認
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
                .Replace("\\[", "[")
                .Replace("\\]", "]")
                .Replace("/", "\\\\"); // /を\\に変換

            // パターンが/で終わる場合（ディレクトリ指定）は、その配下全てにマッチ
            // それ以外は末尾マッチ
            // .*\\で始めることで、パスの任意の位置でマッチ
            if (pattern.EndsWith("/"))
            {
                return ".*\\\\" + escapedPattern;
            }
            return ".*\\\\" + escapedPattern + "$";
        }
    }

    public class Backuptarget
    {
        public List<string> GitFolders { get; set; } = new List<string>();
        public List<string> Folders { get; set; } = new List<string>();
        public List<string> Files { get; set; } = new List<string>();
    }
}
