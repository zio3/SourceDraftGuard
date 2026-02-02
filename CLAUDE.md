# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

SourceDraftGuardは、Gitリポジトリを認識するスマートなバックアップユーティリティです。未コミットの変更、未プッシュのコミット、および非Gitファイルのみを選択的にバックアップします。

## 技術スタック

- C# / .NET 8.0 / Windows デスクトップアプリケーション (WinExe)
- LibGit2Sharp v0.30.0

## ビルド・実行コマンド

```bash
dotnet build                         # デバッグビルド
dotnet build --configuration Release # リリースビルド
dotnet run                           # 実行
dotnet publish --configuration Release # 発行
```

## アーキテクチャ

### 処理フロー

1. `BackupService.LoadGitignoreRegexes()` で `.gitignore` パターンを読み込み
2. `BackupService.EnumerateBackuptarget()` でディレクトリツリーを再帰スキャンし、Gitフォルダ/通常フォルダ/ファイルを分類
3. 各Gitフォルダに対して `GitService.GetUnpushedAndTrackedFiles()` で未プッシュ/未コミットファイルを取得
4. `FileCopyService.BackupFiles()` でSHA256ハッシュ比較により変更ファイルのみをコピー
5. バックアップ先の不要ファイルを削除

### 主要クラス

- **GitService**: LibGit2Sharpを使用し、トラッキングブランチとの差分から未プッシュファイルを検出
- **BackupService**: `Backuptarget`クラスでGitフォルダ/通常フォルダ/ファイルを分類管理
- **FileCopyService**: タイムスタンプを保持したファイルコピーと不要バックアップの削除

## 設定値（Program.cs内でハードコード）

- ソースルート: `C:\Users\info\source\repos`
- バックアップ先: `C:\Users\info\OneDrive\SourceDraftGuard`
- 除外パターン: プロジェクトルートの `.gitignore` を使用

## コーディング規約

- 変数・パラメータ: camelCase
- クラス・パブリックメソッド: PascalCase
- コメント: 日本語
