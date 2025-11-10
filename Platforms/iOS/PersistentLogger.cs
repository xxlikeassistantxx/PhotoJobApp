using Foundation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace PhotoJobApp
{
	/// <summary>
	/// Persistent logger that writes to both file and NSLog.
	/// File logs persist across app terminations and can be viewed later.
	/// NSLog messages appear in Xcode's device console.
	/// </summary>
	public static class PersistentLogger
	{
		private static readonly object _lock = new object();
		private static string? _logFilePath;
		private static bool _initialized = false;
		private static readonly LinkedList<LogEntry> _rollingEntries = new LinkedList<LogEntry>();
		private static readonly TimeSpan RollingWindow = TimeSpan.FromMinutes(10);

		private readonly record struct LogEntry(DateTime Timestamp, string Payload);

		static PersistentLogger()
		{
			Initialize();
		}

		private static void Initialize()
		{
			if (_initialized) return;

			try
			{
				// Get the Documents directory (persists across app restarts)
				var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				var logFileName = $"PhotoJobApp_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
				_logFilePath = Path.Combine(documentsPath, logFileName);

				// Also keep a "latest" log file for easy access
				var latestLogPath = Path.Combine(documentsPath, "PhotoJobApp_Log_Latest.txt");

				// Write initialization message
				var separator = new string('=', 80);
				var initMessage = $"\n{separator}\n" +
				                 $"APP STARTED: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\n" +
				                 $"Log file: {_logFilePath}\n" +
				                 $"{separator}\n\n";

				File.AppendAllText(_logFilePath, initMessage);
				File.WriteAllText(latestLogPath, initMessage); // Overwrite latest log

				_initialized = true;

				// Log initialization
				Log("PersistentLogger", "Initialized", $"Log file: {_logFilePath}");
			}
			catch (Exception ex)
			{
				// If file logging fails, at least use console output
				System.Diagnostics.Debug.WriteLine($"ERROR: Failed to initialize PersistentLogger: {ex.Message}");
				Console.WriteLine($"ERROR: Failed to initialize PersistentLogger: {ex.Message}");
			}
		}

		/// <summary>
		/// Log a message to both file and NSLog
		/// </summary>
		public static void Log(string category, string message, string? details = null)
		{
			try
			{
				if (!_initialized)
				{
					Initialize();
				}

				var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
				var logEntry = $"[{timestamp}] [{category}] {message}";
				if (!string.IsNullOrEmpty(details))
				{
					logEntry += $"\n  Details: {details}";
				}
				logEntry += "\n";
				var entryTimestamp = DateTime.Now;

				// Write to console (appears in Visual Studio debug output and Xcode device console)
				// Both Debug.WriteLine and Console.WriteLine work on iOS and persist in logs
				System.Diagnostics.Debug.WriteLine(logEntry.Trim());
				Console.WriteLine(logEntry.Trim());

				// Write to file (persists across app restarts)
				if (!string.IsNullOrEmpty(_logFilePath))
				{
					lock (_lock)
					{
						try
						{
							File.AppendAllText(_logFilePath, logEntry);
							
							// Also update latest log
							var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
							var latestLogPath = Path.Combine(documentsPath, "PhotoJobApp_Log_Latest.txt");

							// Maintain an in-memory rolling window of the last 10 minutes
							_rollingEntries.AddLast(new LogEntry(entryTimestamp, logEntry));
							var cutoff = DateTime.Now - RollingWindow;
							while (_rollingEntries.First != null && _rollingEntries.First.Value.Timestamp < cutoff)
							{
								_rollingEntries.RemoveFirst();
							}

							// Rewrite the rolling log to ensure it only contains the last 10 minutes
							var builder = new StringBuilder();
							foreach (var entry in _rollingEntries)
							{
								builder.Append(entry.Payload);
							}
							File.WriteAllText(latestLogPath, builder.ToString());
						}
						catch (Exception ex)
						{
							// If file write fails, at least console output worked
							System.Diagnostics.Debug.WriteLine($"ERROR: Failed to write to log file: {ex.Message}");
							Console.WriteLine($"ERROR: Failed to write to log file: {ex.Message}");
						}
					}
				}
			}
			catch (Exception ex)
			{
				// Last resort: just use console output
				System.Diagnostics.Debug.WriteLine($"ERROR in PersistentLogger.Log: {ex.Message}");
				Console.WriteLine($"ERROR in PersistentLogger.Log: {ex.Message}");
			}
		}

		/// <summary>
		/// Log a critical message with extra visibility
		/// </summary>
		public static void LogCritical(string category, string message, string? details = null)
		{
			var criticalMessage = $"🔴 CRITICAL: {message}";
			Log(category, criticalMessage, details);
		}

		/// <summary>
		/// Get the path to the latest log file
		/// </summary>
		public static string? GetLatestLogPath()
		{
			try
			{
				var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				return Path.Combine(documentsPath, "PhotoJobApp_Log_Latest.txt");
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Get all log file paths
		/// </summary>
		public static string[] GetLogFiles()
		{
			try
			{
				var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				if (Directory.Exists(documentsPath))
				{
					return Directory.GetFiles(documentsPath, "PhotoJobApp_Log_*.txt");
				}
			}
			catch
			{
				// Ignore errors
			}
			return Array.Empty<string>();
		}
	}
}

