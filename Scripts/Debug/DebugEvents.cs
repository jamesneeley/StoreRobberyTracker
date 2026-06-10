using GTA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace StoreRobberyEnhanced.Debug
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

        // ⭐ NEW — global enable/disable flag
        private static bool _enabled = true;

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
            StalkerCall,
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
        internal static void Initialize(bool enableEvents)
        {
            try
            {
                _enabled = enableEvents;

                // If disabled → do not create folder or file
                if (!_enabled)
                {
                    _initialized = true;
                    return;
                }

                string folder = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "StoreRobberyEnhanced",
                    "Events"
                );

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                _eventFilePath = Path.Combine(folder, $"DebugEvents_{timestamp}.json");

                File.WriteAllText(_eventFilePath, ""); // create empty file
                _initialized = true;
                DebugLogger.Info("DebugEvents initialized");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("DebugEvents.Init", ex);
            }
        }

        /// <summary>
        /// Emit a structured debug event.
        /// </summary>
        internal static void Emit(EventType type, string source, object payload = null)
        {
            try
            {
                if (!_initialized || !_enabled)
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
                    File.AppendAllText(_eventFilePath, json + Environment.NewLine, Encoding.UTF8);
                }

                // Also log to DebugLogger (only if debug enabled)
                DebugLogger.Event(source, $"{type} event emitted");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("DebugEvents.Emit", ex);

                // Only show subtitle if events are enabled
                if (_enabled)
                    GTA.UI.Screen.ShowSubtitle("~r~DebugEvents write failed.", 2000);
            }
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
