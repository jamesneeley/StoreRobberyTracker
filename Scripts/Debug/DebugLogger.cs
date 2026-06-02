using GTA;
using System;
using System.IO;
using System.Text;

namespace StoreRobberyTrackerMod.Debug
{
    internal static class DebugLogger
    {
        private static readonly object _lock = new object();
        private static string _logFilePath;
        private static bool _initialized = false;

        /// <summary>
        /// Initializes the logger and creates a new session log file.
        /// Call once from main script startup.
        /// </summary>
        internal static void Initialize()
        {
            try
            {
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StoreRobberyTracker", "Logs");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                _logFilePath = Path.Combine(folder, $"DebugLog_{timestamp}.log");

                File.WriteAllText(_logFilePath, $"[Session Start] {DateTime.Now}\n");

                _initialized = true;
            }
            catch (Exception ex)
            {
                GTA.UI.Notification.Show($"~r~DebugLogger Init Failed: {ex.Message}");
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

        internal static void LogException(string context, Exception ex)
        {
            Write(0, "EXCEPTION", $"{context}: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
