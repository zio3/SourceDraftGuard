using System.Security.Cryptography;

string sourceRootPath = @"C:\Users\info\source\repos";
string backupRootPath = @"C:\Users\info\OneDrive\SourceDraftGuard";
var file = "work.csv";//

var backupPaths = File.ReadAllLines(file).ToList();
BackupFiles(sourceRootPath, backupRootPath, backupPaths);

    void BackupFiles(string sourceRootPath, string backupRootPath, List<string> backupPaths)
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