using System.Collections.Generic;
using GTA;
using GTA.Math;

namespace StoreRobberyTrackerMod.Config
{
    /// <summary>
    /// Read‑only settings for the SafeCrack minigame.
    /// Values are loaded from the main StoreRobberyTracker.ini
    /// under the [Store Settings] section.
    /// </summary>
    internal class SafeCrackSettings
    {
        // ------------------------------------------------------------
        // SAFECRACK ECONOMY
        // ------------------------------------------------------------
        public int MinCash { get; set; }
        public int MaxCash { get; set; }

        // ------------------------------------------------------------
        // SAFECRACK BEHAVIOR
        // ------------------------------------------------------------
        public int CooldownMs { get; set; }
        public bool PadShake { get; set; }
        public bool LoadOptionalSafes { get; set; }

        // ------------------------------------------------------------
        // STATIC SAFE POSITIONS (FROM ORIGINAL SCRIPT)
        // ------------------------------------------------------------
        public List<Vector3> SafeLocations { get; set; } = new List<Vector3>();
        public List<float> SafeRotations { get; set; } = new List<float>();
    }
}
