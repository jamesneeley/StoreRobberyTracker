using System.Collections.Generic;
using GTA;
using GTA.Math;

namespace StoreRobberyEnhanced.Config
{
    /// <summary>
    /// Read‑only settings for the SafeCrack minigame.
    /// Values are loaded from StoreRobberyTracker.ini
    /// under the [Store Settings] section.
    /// 
    /// ⭐ This version includes:
    /// - Economy settings
    /// - Cooldown settings
    /// - Sweet spot tuning
    /// - Stage count tuning
    /// - Optional safe loading
    /// - Rumble tuning
    /// - Static safe positions (legacy support)
    /// </summary>
    internal class SafeCrackSettings
    {
        // ------------------------------------------------------------
        // SAFECRACK ECONOMY
        // ------------------------------------------------------------
        /// <summary>Minimum cash payout for cracking a safe.</summary>
        public int MinCash { get; set; }

        /// <summary>Maximum cash payout for cracking a safe.</summary>
        public int MaxCash { get; set; }

        // ------------------------------------------------------------
        // SAFECRACK BEHAVIOR
        // ------------------------------------------------------------
        /// <summary>
        /// Cooldown in milliseconds before the player can attempt
        /// another safe crack.
        /// </summary>
        public int CooldownMs { get; set; }

        /// <summary>
        /// Enables controller rumble feedback when inside the sweet spot.
        /// </summary>
        public bool PadShake { get; set; }

        /// <summary>
        /// Whether to load optional safe positions from the INI.
        /// </summary>
        public bool LoadOptionalSafes { get; set; }

        // ------------------------------------------------------------
        // SWEET SPOT TUNING
        // ------------------------------------------------------------
        /// <summary>
        /// Degrees of tolerance for sweet spot detection.
        /// Example: 8–12 degrees feels good.
        /// </summary>
        public float SweetSpotTolerance { get; set; } = 10f;

        // ------------------------------------------------------------
        // STAGE COUNT TUNING
        // ------------------------------------------------------------
        /// <summary>
        /// Minimum number of stages required to crack the safe.
        /// </summary>
        public int MinStages { get; set; } = 3;

        /// <summary>
        /// Maximum number of stages required to crack the safe.
        /// </summary>
        public int MaxStages { get; set; } = 4;

        // ------------------------------------------------------------
        // STATIC SAFE POSITIONS (LEGACY SUPPORT)
        // ------------------------------------------------------------
        /// <summary>
        /// List of safe positions loaded from the INI.
        /// </summary>
        public List<Vector3> SafeLocations { get; set; } = new List<Vector3>();

        /// <summary>
        /// List of safe headings loaded from the INI.
        /// </summary>
        public List<float> SafeRotations { get; set; } = new List<float>();
    }
}
