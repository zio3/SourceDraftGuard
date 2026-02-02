using System;
using System.IO;

namespace SourceDraftGuard
{
    internal class LogService : IDisposable
    {
        private readonly StreamWriter? _writer;
        private readonly string? _logFilePath;

        public LogService(string? logFilePath = null)
        {
            _logFilePath = logFilePath;
            if (!string.IsNullOrEmpty(logFilePath))
            {
                var logDir = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                _writer = new StreamWriter(logFilePath, append: true);
            }
        }

        public void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logMessage = $"[{timestamp}] {message}";

            Console.WriteLine(logMessage);
            _writer?.WriteLine(logMessage);
            _writer?.Flush();
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }
    }
}
