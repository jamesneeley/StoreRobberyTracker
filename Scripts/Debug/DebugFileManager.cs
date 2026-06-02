using GTA;
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace StoreRobberyTrackerMod.Debug
{
    internal static class DebugFileManager
    {
        private static readonly object _lock = new object();
        private static string _rootFolder;

        internal static void Initialize()
        {
            try
            {
                _rootFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StoreRobberyTracker", "DebugOutput");

                if (!Directory.Exists(_rootFolder))
                    Directory.CreateDirectory(_rootFolder);

                DebugLogger.Info("DebugFileManager initialized.");
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
            SafeWrite(fileName, content);
        }

        internal static void WriteJson(string fileName, object data, bool pretty = true)
        {
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
            string file = $"{Timestamp()}_{name}.json";
            WriteJson(file, data, true);

            DebugEvents.EmitCustom("FileSnapshot", new { File = file });
            DebugLogger.Info($"Snapshot saved: {file}");
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
                GTA.UI.Screen.ShowSubtitle("~r~File write failed.", 2000);
            }
        }

        private static string Timestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        }
    }
}
