using GTA;
using GTA.Math;
using GTA.Native;

namespace StoreRobberyTrackerMod.Minigame
{
    /// <summary>
    /// Handles animations for the SafeCrack minigame.
    /// This stripped version leaves all positioning, freezing,
    /// and control suppression to SafeCrackController.
    /// </summary>
    internal class SafeCrackAnimation : ISafeCrackAnimation
    {
        private const string ANIM_DICT = "mini@safe_cracking";
        private const string ANIM_ENTER = "enter";
        private const string ANIM_LOOP = "dial_turn";
        private const string ANIM_SUCCESS = "success";
        private const string ANIM_EXIT = "exit";

        // ------------------------------------------------------------
        // BEGIN ANIMATION (STRIPPED + SAFE)
        // ------------------------------------------------------------
        public void Begin(Ped player, Vector3 safePos, float safeHeading)
        {
            if (player == null || !player.Exists())
                return;

            // Controller already:
            // - teleported
            // - grounded
            // - aligned
            // - froze the player
            // - disabled gameplay controls

            // Clear tasks only (do NOT reposition or freeze here)
            player.Task.ClearAllImmediately();

            // Load animation dictionary
            Function.Call(Hash.REQUEST_ANIM_DICT, ANIM_DICT);
            while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, ANIM_DICT))
                Script.Yield();

            // Play entry animation
            player.Task.PlayAnimation(
                ANIM_DICT,
                ANIM_ENTER,
                8.0f,
                -8.0f,
                -1,
                AnimationFlags.None,
                0f
            );
        }

        // ------------------------------------------------------------
        // LOOP ANIMATION (DIAL TURN)
        // ------------------------------------------------------------
        public void UpdateLoop(Ped player)
        {
            if (player == null || !player.Exists())
                return;

            if (!Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, player, ANIM_DICT, ANIM_LOOP, 3))
            {
                player.Task.PlayAnimation(
                    ANIM_DICT,
                    ANIM_LOOP,
                    8.0f,
                    -8.0f,
                    -1,
                    AnimationFlags.Loop,
                    0f
                );
            }
        }

        // ------------------------------------------------------------
        // SUCCESS ANIMATION
        // ------------------------------------------------------------
        public void EndSuccess(Ped player)
        {
            if (player == null || !player.Exists())
                return;

            player.Task.PlayAnimation(
                ANIM_DICT,
                ANIM_SUCCESS,
                8.0f,
                -8.0f,
                -1,
                AnimationFlags.None,
                0f
            );

            // Controller will unfreeze + restore controls
        }

        // ------------------------------------------------------------
        // END / CANCEL ANIMATION
        // ------------------------------------------------------------
        public void End(Ped player, bool success)
        {
            if (player == null || !player.Exists())
                return;

            if (!success)
            {
                player.Task.PlayAnimation(
                    ANIM_DICT,
                    ANIM_EXIT,
                    8.0f,
                    -8.0f,
                    -1,
                    AnimationFlags.None,
                    0f
                );
            }

            // Controller will unfreeze + restore controls

            // Safe to remove anim dict now
            Function.Call(Hash.REMOVE_ANIM_DICT, ANIM_DICT);
        }
    }
}
