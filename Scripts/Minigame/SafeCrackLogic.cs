using GTA.Math;
using StoreRobberyTrackerMod.Config;

namespace StoreRobberyTrackerMod.Minigame
{
    /// <summary>
    /// Core logic for the SafeCrack minigame.
    /// Handles rotation, sweet spot detection, stage progression,
    /// direction arrows, and payout.
    /// </summary>
    internal class SafeCrackLogic : ISafeCrackLogic
    {
        private readonly int _minStageAngle = 0;
        private readonly int _maxStageAngle = 359;

        private readonly int _minStages = 2;
        private readonly int _maxStages = 5;

        private readonly System.Random _rand = new System.Random();

        // ------------------------------------------------------------
        // INITIALIZE
        // ------------------------------------------------------------
        public void Initialize(SafeCrackState state, SafeCrackSettings settings)
        {
            state.TotalStages = _rand.Next(_minStages, _maxStages + 1);

            state.Stage = 0;
            state.TargetRotation = RandomAngle();

            state.CurrentDialRotation = 0f;
            state.RotationSpeed = 0f;

            state.IsInSweetSpot = false;
            state.SweetSpotCloseness = 0f;

            state.DirectionRight = true;
            state.ConfirmRequested = false;
        }

        // ------------------------------------------------------------
        // MAIN UPDATE
        // ------------------------------------------------------------
        public void Update(SafeCrackState state, SafeCrackSettings settings)
        {
            // Update rotation
            state.CurrentDialRotation += state.RotationSpeed;

            // Wrap angle
            if (state.CurrentDialRotation >= 360f)
                state.CurrentDialRotation -= 360f;
            if (state.CurrentDialRotation < 0f)
                state.CurrentDialRotation += 360f;

            // Sweet spot detection
            float diff = AngleDifference(state.CurrentDialRotation, state.TargetRotation);
            state.IsInSweetSpot = diff <= state.SweetSpotTolerance;

            // Closeness for UI shake
            state.SweetSpotCloseness =
                (float)System.Math.Max(0.0, state.SweetSpotTolerance - diff);

            // Confirm → advance stage
            if (state.IsInSweetSpot && state.ConfirmRequested)
            {
                bool finished = AdvanceStage(state);
                state.ConfirmRequested = false;

                if (finished)
                    state.Completed = true;
            }
            else
            {
                state.ConfirmRequested = false;
            }
        }

        // ------------------------------------------------------------
        // PLAYER STILL ELIGIBLE?
        // ------------------------------------------------------------
        public bool ValidatePlayerStillEligible(SafeCrackState state)
        {
            if (state.Player == null || !state.Player.Exists())
                return false;

            float dist = state.Player.Position.DistanceTo(state.SafePos);
            if (dist > 2.0f)
                return false;

            // Facing check
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

            // Completed all stages
            if (state.Stage >= state.TotalStages)
                return true;

            // New target
            state.TargetRotation = RandomAngle();

            // Alternate direction (right → left → right)
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

            return _rand.Next(min, max + 1);
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
