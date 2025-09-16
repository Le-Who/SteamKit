using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace SteamTradeConfirmer.Services
{
    public class LoggingService
    {
        private static LoggingService? _instance;
        public static LoggingService Instance => _instance ??= new LoggingService();

        private readonly List<LogEntry> _logs = new List<LogEntry>();
        private readonly object _lock = new object();

        public event EventHandler<LogEntry>? LogAdded;

        public void LogInfo(string message, string? accountName = null)
        {
            AddLog(LogLevel.Info, message, accountName);
        }

        public void LogWarning(string message, string? accountName = null)
        {
            AddLog(LogLevel.Warning, message, accountName);
        }

        public void LogError(string message, string? accountName = null, Exception? exception = null)
        {
            var fullMessage = exception != null ? $"{message}\nИсключение: {exception}" : message;
            AddLog(LogLevel.Error, fullMessage, accountName);
        }

        public void LogDebug(string message, string? accountName = null)
        {
            AddLog(LogLevel.Debug, message, accountName);
        }

        private void AddLog(LogLevel level, string message, string? accountName)
        {
            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                AccountName = accountName
            };

            lock (_lock)
            {
                _logs.Add(logEntry);
                
                // Ограничиваем количество логов в памяти
                if (_logs.Count > 1000)
                {
                    _logs.RemoveAt(0);
                }
            }

            // Выводим в консоль для отладки
            Console.WriteLine($"[{logEntry.Timestamp:HH:mm:ss.fff}] [{level}] {(accountName != null ? $"[{accountName}] " : "")}{message}");
            
            // Автоматически сохраняем в файл
            SaveLogToFile(logEntry);
            
            LogAdded?.Invoke(this, logEntry);
        }

        public List<LogEntry> GetLogs()
        {
            lock (_lock)
            {
                return new List<LogEntry>(_logs);
            }
        }

        public void ClearLogs()
        {
            lock (_lock)
            {
                _logs.Clear();
            }
        }

        private void SaveLogToFile(LogEntry logEntry)
        {
            try
            {
                var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SteamTradeConfirmer", "Logs");
                Directory.CreateDirectory(logDir);
                
                var logFile = Path.Combine(logDir, $"steam_trade_confirmer_{DateTime.Now:yyyyMMdd}.log");
                var logLine = $"[{logEntry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{logEntry.Level}] {(logEntry.AccountName != null ? $"[{logEntry.AccountName}] " : "")}{logEntry.Message}";
                
                File.AppendAllText(logFile, logLine + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                // Если не удалось сохранить в файл, выводим ошибку в консоль
                Console.WriteLine($"Ошибка сохранения лога: {ex.Message}");
            }
        }

        public void SaveLogsToFile(string filePath)
        {
            lock (_lock)
            {
                var sb = new StringBuilder();
                foreach (var log in _logs)
                {
                    sb.AppendLine($"[{log.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{log.Level}] {(log.AccountName != null ? $"[{log.AccountName}] " : "")}{log.Message}");
                }
                
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            }
        }

        public string GetLogFilePath()
        {
            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SteamTradeConfirmer", "Logs");
            return Path.Combine(logDir, $"steam_trade_confirmer_{DateTime.Now:yyyyMMdd}.log");
        }
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? AccountName { get; set; }
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}
