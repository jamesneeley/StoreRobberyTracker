using GTA;
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace StoreRobberyEnhanced.Debug
{
    internal static class DebugFileManager
    {
        private static readonly object _lock = new object();
        private static string _rootFolder;

        // ⭐ NEW — global enable/disable flag
        private static bool _enabled = true;
        private static bool _initialized = false;

        /// <summary>
        /// Initialize the file manager.
        /// If disabled, no files will be written.
        /// </summary>
        internal static void Initialize(bool enableFileManager)
        {
            try
            {
                _enabled = enableFileManager;

                // If disabled → do not create folder, do not write anything
                if (!_enabled)
                {
                    _initialized = true;
                    return;
                }

                _rootFolder = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "StoreRobberyEnhanced",
                    "DebugOutput"
                );

                if (!Directory.Exists(_rootFolder))
                    Directory.CreateDirectory(_rootFolder);

                DebugLogger.Info("DebugFileManager initialized.");
                _initialized = true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("DebugFileManager.Init", ex);
            }
        }

        // ------------------------------
        // Public API
        // ------------------------------

        internal static void WriteText(string fileName, string content)
        {
            if (!_initialized || !_enabled)
                return;

            SafeWrite(fileName, content);
        }

        internal static void WriteJson(string fileName, object data, bool pretty = true)
        {
            if (!_initialized || !_enabled)
                return;

            try
            {
                string json = pretty
                    ? JsonConvert.SerializeObject(data, Formatting.Indented)
                    : JsonConvert.SerializeObject(data);

                SafeWrite(fileName, json);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("DebugFileManager.WriteJson", ex);
            }
        }

        internal static void WriteSnapshot(string name, object data)
        {
            if (!_initialized || !_enabled)
                return;

            try
            {
                string file = $"{Timestamp()}_{name}.json";
                WriteJson(file, data, true);

                DebugEvents.EmitCustom("FileSnapshot", new { File = file });
                DebugLogger.Info($"Snapshot saved: {file}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("DebugFileManager.WriteSnapshot", ex);
            }
        }

        // ------------------------------
        // Core Safe Write Logic
        // ------------------------------

        private static void SafeWrite(string fileName, string content)
        {
            try
            {
                lock (_lock)
                {
                    string path = Path.Combine(_rootFolder, fileName);
                    File.WriteAllText(path, content, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("DebugFileManager.SafeWrite", ex);

                // Only show subtitle if debug is enabled
                if (_enabled)
                    GTA.UI.Screen.ShowSubtitle("~r~File write failed.", 2000);
            }
        }

        private static string Timestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        }
    }
}
