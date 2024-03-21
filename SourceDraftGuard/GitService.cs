using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceDraftGuard
{
    internal class GitService
    {
        public string[] GetUnpushedAndTrackedFiles(string repoPath)
        {
            using (var repo = new Repository(repoPath))
            {
                // 追跡されているが、コミットされていないファイルのリストを取得
                var uncommittedFiles = repo.RetrieveStatus(new StatusOptions { IncludeUntracked = true })
                                           .Where(status => status.State != FileStatus.Ignored)
                                           .Select(status => status.FilePath)
                                           .ToList();

                // 現在のブランチとトラッキングブランチの比較
                var branch = repo.Head;
                var trackingBranch = branch.TrackedBranch;

                if (trackingBranch != null)
                {
                    // 最新のブランチがPushされているかチェック
                    if (branch.Tip.Sha == trackingBranch.Tip.Sha)
                    {
                        // 最新のコミットがPushされている場合
                        return uncommittedFiles.Distinct()
                            .Select(c => Path.Combine(repoPath, c))
                            .ToArray();
                    }

                    var unpushedFiles = repo.Diff.Compare<TreeChanges>(trackingBranch.Tip.Tree, branch.Tip.Tree)
                                          .Select(diff => diff.Path);


                    // リストをマージし、重複を排除
                    uncommittedFiles.AddRange(unpushedFiles);
                }

                return uncommittedFiles.Distinct()
                    .Select(c => Path.Combine(repoPath, c))
                    .ToArray();
            }
        }
    }
}
