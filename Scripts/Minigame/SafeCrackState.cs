using GTA;
using GTA.Math;

namespace StoreRobberyEnhanced.Minigame
{
    /// <summary>
    /// Holds all runtime state for the SafeCrack minigame.
    /// Pure state container — no logic.
    /// 
    /// ⭐ This version is fully aligned with:
    /// - SafeCrackController (patched)
    /// - SafeCrackLogic (patched)
    /// - SafeCrackInput (patched)
    /// - SafeCrackUI (patched)
    /// - SafeCrackSettings (patched)
    /// </summary>
    internal class SafeCrackState
    {
        // ------------------------------------------------------------
        // GENERAL STATE
        // ------------------------------------------------------------
        public bool Active = false;
        public bool Completed = false;
        public bool Failed = false;
        public bool CooldownActive = false;

        // ------------------------------------------------------------
        // SAFE POSITION + HEADING
        // ------------------------------------------------------------
        public Vector3 SafePos = Vector3.Zero;
        public float SafeHeading = 0f;

        // ------------------------------------------------------------
        // PLAYER INTERACTION
        // ------------------------------------------------------------
        public Ped Player = null;
        public bool PlayerInRange = false;
        public bool PlayerFacingSafe = false;

        /// <summary>
        /// Set by SafeCrackInput, consumed by SafeCrackLogic.
        /// Preserved across frames by SafeCrackController.
        /// </summary>
        public bool ConfirmRequested = false;

        // ------------------------------------------------------------
        // ROTATION / LOCK MECHANICS
        // ------------------------------------------------------------
        public float CurrentDialRotation = 0f;
        public float TargetRotation = 0f;
        public float RotationSpeed = 0f;

        public int Stage = 0;
        public int TotalStages = 3;

        /// <summary>
        /// Direction arrow (true = right, false = left).
        /// Alternates each stage.
        /// </summary>
        public bool DirectionRight = true;

        // ------------------------------------------------------------
        // FEEDBACK / EFFECTS
        // ------------------------------------------------------------
        public bool PadShakeEnabled = true;

        /// <summary>
        /// True when the dial is within SweetSpotTolerance degrees of TargetRotation.
        /// </summary>
        public bool IsInSweetSpot = false;

        /// <summary>
        /// Normalized 0–1 closeness value used by UI shake.
        /// </summary>
        public float SweetSpotCloseness = 0f;

        /// <summary>
        /// Sweet spot tolerance in degrees.
        /// Loaded from SafeCrackSettings.
        /// </summary>
        public float SweetSpotTolerance = 10f;

        // ------------------------------------------------------------
        // TIMERS
        // ------------------------------------------------------------
        public int LastUpdateTime = 0;
        public int CooldownEndTime = 0;
        public int StartTime = 0;
        public int LastTimerUpdate = 0;

        // ------------------------------------------------------------
        // PAYOUT
        // ------------------------------------------------------------
        public int Payout = 0;

        // ------------------------------------------------------------
        // RESET ALL STATE
        // ------------------------------------------------------------
        public void Reset()
        {
            Active = false;
            Completed = false;
            Failed = false;
            CooldownActive = false;

            PlayerInRange = false;
            PlayerFacingSafe = false;
            ConfirmRequested = false;

            CurrentDialRotation = 0f;
            TargetRotation = 0f;
            RotationSpeed = 0f;

            Stage = 0;
            // ⭐ TotalStages is set by SafeCrackLogic.Initialize()
            // DO NOT override it here.
            DirectionRight = true;

            IsInSweetSpot = false;
            SweetSpotCloseness = 0f;

            LastUpdateTime = 0;
            CooldownEndTime = 0;
            StartTime = 0;
            LastTimerUpdate = 0;

            Payout = 0;
        }
    }
}
