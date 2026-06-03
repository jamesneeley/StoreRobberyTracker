using GTA;
using GTA.Math;

namespace StoreRobberyTrackerMod.Minigame
{
    /// <summary>
    /// Holds all runtime state for the SafeCrack minigame.
    /// Pure state container — no logic.
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

        // Confirm input (separate from facing)
        public bool ConfirmRequested = false;

        // ------------------------------------------------------------
        // ROTATION / LOCK MECHANICS
        // ------------------------------------------------------------
        public float CurrentDialRotation = 0f;
        public float TargetRotation = 0f;
        public float RotationSpeed = 0f;

        public int Stage = 0;
        public int TotalStages = 3;

        // Direction arrow (right/left)
        public bool DirectionRight = true;

        // ------------------------------------------------------------
        // FEEDBACK / EFFECTS
        // ------------------------------------------------------------
        public bool PadShakeEnabled = true;
        public bool IsInSweetSpot = false;

        // How close to the sweet spot (0+)
        public float SweetSpotCloseness = 0f;

        public int SweetSpotTolerance = 6;

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
            TotalStages = 3;
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
