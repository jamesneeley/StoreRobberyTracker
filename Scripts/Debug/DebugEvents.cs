using GTA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace StoreRobberyTrackerMod.Debug
{
    /// <summary>
    /// Central event bus for structured debug events.
    /// Supports JSON payloads, file logging, and future replay.
    /// </summary>
    internal static class DebugEvents
    {
        private static readonly object _lock = new object();
        private static string _eventFilePath;
        private static bool _initialized = false;

        /// <summary>
        /// Event types for structured debugging.
        /// Add more as your system grows.
        /// </summary>
        internal enum EventType
        {
            RobberyStart,
            SafeCrack,
            CameraAlarm,
            Escape,
            Payout,
            Cooldown,
            Stalker,
            UI,
            Banner,
            Timer,
            DebugToggle,
            MultiPos,
            MiscActions,
            ScenarioFullRob,
            ScenarioQuickLoot,
            Custom
        }

        /// <summary>
        /// Event data container.
        /// </summary>
        internal class DebugEvent
        {
            public EventType Type { get; set; }
            public string Source { get; set; }
            public object Payload { get; set; }
            public DateTime Timestamp { get; set; }
        }

        /// <summary>
        /// Initialize event logging (called once at startup).
        /// </summary>
        internal static void Initialize()
        {
            try
            {
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StoreRobberyTracker", "Events");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                _eventFilePath = Path.Combine(folder, $"DebugEvents_{timestamp}.json");

                File.WriteAllText(_eventFilePath, ""); // create empty file
                _initialized = true;
            }
            catch (Exception ex)
            {
                GTA.UI.Notification.Show($"~r~DebugEvents Init Failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Emit a structured debug event.
        /// </summary>
        internal static void Emit(EventType type, string source, object payload = null)
        {
            if (!_initialized)
                return;

            DebugEvent evt = new DebugEvent
            {
                Type = type,
                Source = source,
                Payload = payload,
                Timestamp = DateTime.UtcNow
            };

            string json = JsonConvert.SerializeObject(evt);

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_eventFilePath, json + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    GTA.UI.Screen.ShowSubtitle("~r~DebugEvents write failed.", 2000);
                }
            }

            // Also log to DebugLogger
            DebugLogger.Event(source, $"{type} event emitted");
        }

        /// <summary>
        /// Convenience helper for custom events.
        /// </summary>
        internal static void EmitCustom(string source, object payload)
        {
            Emit(EventType.Custom, source, payload);
        }
    }
}
