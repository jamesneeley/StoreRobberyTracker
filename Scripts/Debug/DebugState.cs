using System;
using System.Collections.Generic;

namespace StoreRobberyTrackerMod.Debug
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

        // --- Methods ---

        /// <summary>
        /// Called by IniConfig after loading debug settings.
        /// </summary>
        public static void ApplyConfig(bool enabled, int level, bool hotkeys, Dictionary<string, bool> flags)
        {
            IsDebugMode = enabled;
            DebugLevel = level;
            HotkeysEnabled = hotkeys;

            Flags = flags ?? new Dictionary<string, bool>();
        }

        /// <summary>
        /// Records the last triggered debug action for overlay display.
        /// </summary>
        public static void RecordAction(string actionName)
        {
            LastActionName = actionName;
            LastActionTime = DateTime.UtcNow;
            ActionCount++;
        }

        /// <summary>
        /// Toggles the overlay visibility.
        /// </summary>
        public static void ToggleOverlay()
        {
            OverlayVisible = !OverlayVisible;
            LastOverlayToggle = DateTime.UtcNow;
        }

        /// <summary>
        /// Returns true if a named debug flag exists and is enabled.
        /// </summary>
        public static bool Flag(string name)
        {
            if (Flags.TryGetValue(name, out bool value))
                return value;

            return false;
        }
    }
}
