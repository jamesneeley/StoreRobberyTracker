using GTA;
using GTA.Native;
using StoreRobberyTrackerMod.Config;

namespace StoreRobberyTrackerMod.Minigame
{
    /// <summary>
    /// Handles player input for the SafeCrack minigame.
    /// Converts keyboard/controller input into rotation + confirm/cancel.
    /// </summary>
    internal class SafeCrackInput : ISafeCrackInput
    {
        private const float ROTATION_STEP = 0.8f;
        private const float ROTATION_SLOW = 0.45f;

        public void Process(SafeCrackState state, SafeCrackSettings settings)
        {
            if (!state.Active)
                return;

            // Reset per tick
            state.RotationSpeed = 0f;
            state.ConfirmRequested = false;

            // ------------------------------------------------------------
            // KEYBOARD ROTATION (A / D)
            // ------------------------------------------------------------
            if (Game.IsKeyPressed(System.Windows.Forms.Keys.A))
                state.RotationSpeed = -ROTATION_STEP;
            else if (Game.IsKeyPressed(System.Windows.Forms.Keys.D))
                state.RotationSpeed = ROTATION_STEP;

            // ------------------------------------------------------------
            // CONTROLLER ROTATION (Left Stick X)
            // NOTE: When gameplay controls are restricted, group 2 is used.
            // ------------------------------------------------------------
            float axisX = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 2, (int)Control.MoveLeftRight);

            if (System.Math.Abs(axisX) > 0.05f)
                state.RotationSpeed = axisX * ROTATION_SLOW;

            // ------------------------------------------------------------
            // CONFIRM INPUT (E or A)
            // ------------------------------------------------------------
            bool confirmKey = Game.IsKeyPressed(System.Windows.Forms.Keys.E);
            bool confirmPad = Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 2, (int)Control.Context);

            if (confirmKey || confirmPad)
                state.ConfirmRequested = true;

            // ------------------------------------------------------------
            // CANCEL INPUT (ESC or B)
            // ------------------------------------------------------------
            bool cancelKey = Game.IsKeyPressed(System.Windows.Forms.Keys.Escape);
            bool cancelPad = Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 2, (int)Control.PhoneCancel);

            if (cancelKey || cancelPad)
                state.Failed = true;
        }
    }
}
