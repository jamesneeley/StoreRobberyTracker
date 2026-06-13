using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyEnhanced.Config;

namespace StoreRobberyEnhanced.Minigame
{
    /// <summary>
    /// Core logic for the SafeCrack minigame.
    /// Handles rotation, sweet spot detection, stage progression,
    /// direction arrows, and payout.
    /// 
    /// ⭐ This version fixes:
    /// - Infinite stage loops
    /// - Impossible sweet spots
    /// - Incorrect closeness values
    /// - Confirm input being eaten
    /// - Direction arrow desync
    /// - Rumble spam
    /// - Too many stages (now 3–4, GTA‑paced)
    /// </summary>
    internal class SafeCrackLogic : ISafeCrackLogic
    {
        private readonly int _minStageAngle = 0;
        private readonly int _maxStageAngle = 359;

        // ⭐ GTA pacing: 3–4 stages feels perfect
        private readonly int _minStages = 3;
        private readonly int _maxStages = 4;

        private int _lastTickTime = 0;

        private readonly System.Random _rand = new System.Random();

        // ------------------------------------------------------------
        // INITIALIZE
        // ------------------------------------------------------------
        public void Initialize(SafeCrackState state, SafeCrackSettings settings)
        {
            // ⭐ Random stage count (3–4)
            state.TotalStages = _rand.Next(_minStages, _maxStages + 1);

            state.Stage = 0;
            state.TargetRotation = RandomAngle();

            state.CurrentDialRotation = 0f;
            state.RotationSpeed = 0f;

            state.IsInSweetSpot = false;
            state.SweetSpotCloseness = 0f;

            // ⭐ Start by rotating right
            state.DirectionRight = true;

            // Confirm is handled by controller
            state.ConfirmRequested = false;
        }

        // ------------------------------------------------------------
        // MAIN UPDATE
        // ------------------------------------------------------------
        public void Update(SafeCrackState state, SafeCrackSettings settings)
        {
            // ------------------------------------------------------------
            // ROTATION UPDATE
            // ------------------------------------------------------------
            state.CurrentDialRotation += state.RotationSpeed;

            // Wrap angle
            if (state.CurrentDialRotation >= 360f)
                state.CurrentDialRotation -= 360f;
            if (state.CurrentDialRotation < 0f)
                state.CurrentDialRotation += 360f;

            // ------------------------------------------------------------
            // TICK‑TICK‑TICK SOUND WHILE ROTATING
            // ------------------------------------------------------------
            if (System.Math.Abs(state.RotationSpeed) > 0.1f)
            {
                int now = Game.GameTime;

                // Tick every 150 ms (≈ 6 ticks per second)
                if (now - _lastTickTime > 150)
                {
                    _lastTickTime = now;

                    // Soft mechanical tick
                    Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                }
            }

            // ------------------------------------------------------------
            // SWEET SPOT DETECTION
            // ------------------------------------------------------------
            float diff = AngleDifference(state.CurrentDialRotation, state.TargetRotation);

            state.IsInSweetSpot = diff <= state.SweetSpotTolerance;

            // ⭐ Closeness is normalized 0–1 for UI shake
            state.SweetSpotCloseness =
                1f - (diff / state.SweetSpotTolerance);

            if (state.SweetSpotCloseness < 0f)
                state.SweetSpotCloseness = 0f;

            // ------------------------------------------------------------
            // CONTROLLER FEEDBACK (LIGHT RUMBLE)
            // ------------------------------------------------------------
            if (state.IsInSweetSpot)
            {
                // ⭐ Light rumble, not spammy
                Function.Call(Hash.SET_CONTROL_SHAKE, 0, 80, 120);
            }

            // ------------------------------------------------------------
            // CONFIRM → ADVANCE STAGE
            // ------------------------------------------------------------
            if (state.IsInSweetSpot && state.ConfirmRequested)
            {
                bool finished = AdvanceStage(state);
                state.ConfirmRequested = false;

                if (finished)
                    state.Completed = true;

                return;
            }

            // If confirm was pressed outside sweet spot, ignore it
            state.ConfirmRequested = false;
        }

        // ------------------------------------------------------------
        // PLAYER STILL ELIGIBLE?
        // ------------------------------------------------------------
        public bool ValidatePlayerStillEligible(SafeCrackState state)
        {
            if (state.Player == null || !state.Player.Exists())
                return false;

            // ⭐ Distance check
            float dist = state.Player.Position.DistanceTo(state.SafePos);
            if (dist > 2.0f)
                return false;

            // ⭐ Facing check
            Vector3 dir = (state.SafePos - state.Player.Position).Normalized;
            float dot = Vector3.Dot(state.Player.ForwardVector, dir);

            state.PlayerFacingSafe = dot > 0.65f;

            return true;
        }

        // ------------------------------------------------------------
        // ADVANCE STAGE
        // ------------------------------------------------------------
        private bool AdvanceStage(SafeCrackState state)
        {
            state.Stage++;

            // ⭐ Play lock unlock sound (only if not final stage)
            if (state.Stage < state.TotalStages)
            {
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PIN_BUTTON", "ATM_SOUNDS");
            }

            // ⭐ Completed all stages
            if (state.Stage >= state.TotalStages)
                return true;

            // ⭐ New target angle
            state.TargetRotation = RandomAngle();

            // ⭐ Alternate direction (right → left → right)
            state.DirectionRight = !state.DirectionRight;

            return false;
        }

        // ------------------------------------------------------------
        // PAYOUT
        // ------------------------------------------------------------
        public int CalculatePayout(SafeCrackSettings settings)
        {
            int min = settings.MinCash;
            int max = settings.MaxCash;

            if (max < min)
                max = min;

            // ⭐ Slightly weighted toward higher payouts
            int roll = _rand.Next(min, max + 1);
            int bonus = _rand.Next(0, (max - min) / 4 + 1);

            return roll + bonus;
        }

        // ------------------------------------------------------------
        // UTILITY: RANDOM ANGLE
        // ------------------------------------------------------------
        private float RandomAngle()
        {
            return (float)_rand.Next(_minStageAngle, _maxStageAngle + 1);
        }

        // ------------------------------------------------------------
        // UTILITY: ANGLE DIFFERENCE
        // ------------------------------------------------------------
        private float AngleDifference(float a, float b)
        {
            float diff = System.Math.Abs(a - b);
            if (diff > 180f)
                diff = 360f - diff;
            return diff;
        }
    }
}
