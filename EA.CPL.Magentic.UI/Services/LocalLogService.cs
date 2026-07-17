using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EA.CPL.Magentic.Orchestration.Models;

namespace EA.CPL.Magentic.UI.Services;

/// <summary>
/// Service for storing orchestration logs to local file system
/// </summary>
public class LocalLogService
{
    private readonly string _logsDirectory;

    public LocalLogService()
    {
        // Create logs directory in user's local app data
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _logsDirectory = Path.Combine(appDataPath, "EscrowAutomation", "Logs", "Orchestrations");

        if (!Directory.Exists(_logsDirectory))
        {
            Directory.CreateDirectory(_logsDirectory);
        }
    }

    /// <summary>
    /// Get the log file path for an orchestration
    /// </summary>
    public string GetLogFilePath(string orchestrationId)
    {
        return Path.Combine(_logsDirectory, $"orchestration_{orchestrationId}.log");
    }

    /// <summary>
    /// Save a single log entry to file
    /// </summary>
    public async Task AppendLogAsync(string orchestrationId, LogEntry entry)
    {
        try
        {
            var filePath = GetLogFilePath(orchestrationId);
            var logLine = FormatLogEntry(entry);

            await File.AppendAllTextAsync(filePath, logLine + Environment.NewLine, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error writing log: {ex.Message}");
        }
    }

    /// <summary>
    /// Save multiple log entries to file
    /// </summary>
    public async Task AppendLogsAsync(string orchestrationId, IEnumerable<LogEntry> entries)
    {
        try
        {
            var filePath = GetLogFilePath(orchestrationId);
            var logLines = entries.Select(FormatLogEntry);

            await File.AppendAllLinesAsync(filePath, logLines, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error writing logs: {ex.Message}");
        }
    }

    /// <summary>
    /// Save a summary to file
    /// </summary>
    public async Task WriteSummaryAsync(string orchestrationId, string status, string? plan, string? error = null)
    {
        try
        {
            var filePath = GetLogFilePath(orchestrationId);
            var summary = new StringBuilder();
            summary.AppendLine();
            summary.AppendLine("=".PadRight(80, '='));
            summary.AppendLine($"ORCHESTRATION SUMMARY - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            summary.AppendLine("=".PadRight(80, '='));
            summary.AppendLine($"Status: {status}");

            if (!string.IsNullOrEmpty(error))
            {
                summary.AppendLine($"Error: {error}");
            }

            if (!string.IsNullOrEmpty(plan))
            {
                summary.AppendLine("Final Plan:");
                summary.AppendLine(plan);
            }

            summary.AppendLine("=".PadRight(80, '='));

            await File.AppendAllTextAsync(filePath, summary.ToString(), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error writing summary: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all log files
    /// </summary>
    public List<(string FileName, DateTime CreatedTime)> GetAllLogFiles()
    {
        try
        {
            var directory = new DirectoryInfo(_logsDirectory);
            return directory.GetFiles("*.log")
                .OrderByDescending(f => f.CreationTime)
                .Select(f => (f.Name, f.CreationTime))
                .ToList();
        }
        catch
        {
            return new List<(string, DateTime)>();
        }
    }

    /// <summary>
    /// Get logs directory path
    /// </summary>
    public string GetLogsDirectoryPath() => _logsDirectory;

    private static string FormatLogEntry(LogEntry entry)
    {
        return $"[{entry.TimestampUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}] [{entry.Source}] {entry.Message}";
    }
}
