using GTA;
using System;
using System.IO;
using System.Text;

namespace StoreRobberyEnhanced.Debug
{
    internal static class DebugLogger
    {
        private static readonly object _lock = new object();
        private static string _logFilePath;
        private static bool _initialized = false;

        // ⭐ NEW — global enable/disable flag
        private static bool _enabled = true;

        /// <summary>
        /// Initializes the logger and creates a new session log file.
        /// Call once from main script startup.
        /// </summary>
        internal static void Initialize(bool enableDebug)
        {
            try
            {
                _enabled = enableDebug;

                // If debug disabled → do NOT create log file
                if (!_enabled)
                {
                    _initialized = true;   // still mark as initialized
                    return;
                }

                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StoreRobberyEnhanced", "Logs");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                _logFilePath = Path.Combine(folder, $"DebugLog_{timestamp}.log");

                File.WriteAllText(_logFilePath, $"[Session Start] {DateTime.Now}\n");

                _initialized = true;
                DebugLogger.Info("DebugLogger initialized");
            }
            catch (Exception ex)
            {
                GTA.UI.Notification.PostTicker($"~r~DebugLogger Init Failed: {ex.Message}", true);
            }
        }

        // ------------------------------
        // Public Logging API
        // ------------------------------

        internal static void Info(string message) => Write(2, "INFO", message);
        internal static void Warn(string message) => Write(1, "WARN", message);
        internal static void Error(string message) => Write(0, "ERROR", message);
        internal static void Trace(string message) => Write(4, "TRACE", message);

        internal static void Event(string category, string message)
        {
            Write(3, category.ToUpper(), message);
        }

        // ------------------------------
        // Core Write Logic
        // ------------------------------

        private static void Write(int level, string tag, string message)
        {
            if (!_initialized)
                return;

            // ⭐ NEW — skip all non-exception logs when disabled
            if (!_enabled)
                return;

            if (level > DebugState.DebugLevel)
                return;

            string line = $"[{DateTime.Now:HH:mm:ss}] [{tag}] {message}";

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // If file write fails, fallback to subtitle
                    GTA.UI.Screen.ShowSubtitle("~r~DebugLogger write failed.", 2000);
                }
            }

            // Update DebugState for overlay
            DebugState.LastActionName = tag;
            DebugState.LastActionTime = DateTime.UtcNow;
        }

        // ------------------------------
        // Convenience Helpers
        // ------------------------------

        internal static void LogAction(string actionName)
        {
            Write(2, "ACTION", actionName);
        }

        // ⭐ EXCEPTIONS ALWAYS LOG — even when debug disabled
        internal static void LogException(string context, Exception ex)
        {
            try
            {
                if (!_initialized)
                    return;

                // If debug disabled → create a minimal emergency log file
                if (!_enabled)
                {
                    string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StoreRobberyEnhanced", "Logs");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    if (string.IsNullOrEmpty(_logFilePath))
                        _logFilePath = Path.Combine(folder, "Exceptions.log");
                }

                string line = $"[{DateTime.Now:HH:mm:ss}] [EXCEPTION] {context}: {ex.Message}\n{ex.StackTrace}";

                lock (_lock)
                {
                    File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Never throw from exception logger
            }
        }
    }
}
