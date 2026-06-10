using StoreRobberyEnhanced.Data;
using System;
using System.Collections.Generic;

namespace StoreRobberyEnhanced.Debug
{
    /// <summary>
    /// Centralized state container for the entire debug platform.
    /// No logic, no input handling — pure state only.
    /// </summary>
    public static class DebugState
    {
        // --- Core Debug Mode ---
        public static bool IsDebugMode { get; set; } = false;          // Toggled by CTRL+F9
        public static bool HotkeysEnabled { get; set; } = true;        // Can be disabled via INI
        public static int DebugLevel { get; set; } = 2;                // 0–4 (Error → Trace)

        // --- Overlay ---
        public static bool OverlayVisible { get; set; } = false;       // CTRL+F9 toggles this
        public static DateTime LastOverlayToggle { get; set; } = DateTime.MinValue;

        // --- Last Action Tracking ---
        public static string LastActionName { get; set; } = "None";
        public static DateTime LastActionTime { get; set; } = DateTime.MinValue;

        // --- Debug Flags (loaded from INI) ---
        public static Dictionary<string, bool> Flags { get; private set; } = new Dictionary<string, bool>();

        // --- Session Info ---
        public static DateTime SessionStart { get; set; } = DateTime.UtcNow;
        public static int ActionCount { get; private set; } = 0;

        // --- NEW: Global Debug Toggles ---
        public static bool EnableLogging { get; set; } = true;         // Renamed from EnableDebug
        public static bool EnableFileManager { get; set; } = true;
        public static bool EnableEvents { get; set; } = true;
        public static bool EnableProfiler { get; set; } = true;

        // ------------------------------------------------------------
        // APPLY CONFIG FROM INI
        // ------------------------------------------------------------
        /// <summary>
        /// Called by IniConfig after loading debug settings.
        /// </summary>
        public static void ApplyConfig(
            bool enableLogging,
            int level,
            bool hotkeys,
            Dictionary<string, bool> flags,
            bool enableFileManager,
            bool enableEvents,
            bool enableProfiler)
        {
            IsDebugMode = enableLogging;      // Debug mode follows logging toggle
            EnableLogging = enableLogging;    // NEW name

            DebugLevel = level;
            HotkeysEnabled = hotkeys;

            Flags = flags ?? new Dictionary<string, bool>();

            EnableFileManager = enableFileManager;
            EnableEvents = enableEvents;
            EnableProfiler = enableProfiler;
        }

        // ------------------------------------------------------------
        // RECORD ACTION
        // ------------------------------------------------------------
        public static void RecordAction(string actionName)
        {
            LastActionName = actionName;
            LastActionTime = DateTime.UtcNow;
            ActionCount++;
        }

        // ------------------------------------------------------------
        // TOGGLE OVERLAY
        // ------------------------------------------------------------
        public static void ToggleOverlay()
        {
            OverlayVisible = !OverlayVisible;
            LastOverlayToggle = DateTime.UtcNow;
        }

        // ------------------------------------------------------------
        // FLAG LOOKUP
        // ------------------------------------------------------------
        public static bool Flag(string name)
        {
            if (Flags.TryGetValue(name, out bool value))
                return value;

            return false;
        }
        // ------------------------------------------------------------
        // PATCH 12 — Global Debug References
        // ------------------------------------------------------------

        /// <summary>
        /// The last store processed by UpdateClerk(), used by DebugOverlay.
        /// This is optional and only for debug display.
        /// </summary>
        internal static TrackedStore LastStore { get; set; } = null;

        /// <summary>
        /// Indicates whether SafeCrack is currently running.
        /// Set by DebugController or SafeCrack system.
        /// </summary>
        public static bool SafeCrackRunning { get; set; } = false;

    }
}
