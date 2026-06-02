using GTA;
using GTA.Math;
using GTA.Native;

namespace StoreRobberyTrackerMod.Minigame
{
    internal class SafeCrackAnimation : ISafeCrackAnimation
    {
        private const string ANIM_DICT = "mini@safe_cracking";
        private const string ANIM_ENTER = "enter";
        private const string ANIM_LOOP = "dial_turn";
        private const string ANIM_SUCCESS = "success";
        private const string ANIM_EXIT = "exit";

        // ------------------------------------------------------------
        // BEGIN ANIMATION
        // ------------------------------------------------------------
        public void Begin(Ped player, Vector3 safePos, float safeHeading)
        {
            if (player == null || !player.Exists())
                return;

            // ⭐ Freeze player movement
            player.Task.ClearAllImmediately();
            player.IsPositionFrozen = true;

            // ⭐ Disable all controls (prevents walking, turning, etc.)
            Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 0);

            // Align player to safe
            player.Position = safePos + player.ForwardVector * -0.5f;
            player.Heading = safeHeading;

            // Load animation dictionary
            Function.Call(Hash.REQUEST_ANIM_DICT, ANIM_DICT);
            while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, ANIM_DICT))
            {
                Script.Yield();
            }

            // Play entry animation
            player.Task.PlayAnimation(ANIM_DICT, ANIM_ENTER, 8.0f, -8.0f, -1, AnimationFlags.None, 0f);
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
                player.Task.PlayAnimation(ANIM_DICT, ANIM_LOOP, 8.0f, -8.0f, -1, AnimationFlags.Loop, 0f);
            }
        }

        // ------------------------------------------------------------
        // SUCCESS ANIMATION
        // ------------------------------------------------------------
        public void EndSuccess(Ped player)
        {
            if (player == null || !player.Exists())
                return;

            player.Task.PlayAnimation(ANIM_DICT, ANIM_SUCCESS, 8.0f, -8.0f, -1, AnimationFlags.None, 0f);

            // ⭐ Unfreeze player after success animation
            player.IsPositionFrozen = false;
            Function.Call(Hash.ENABLE_ALL_CONTROL_ACTIONS, 0);
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
                player.Task.PlayAnimation(ANIM_DICT, ANIM_EXIT, 8.0f, -8.0f, -1, AnimationFlags.None, 0f);
            }

            // ⭐ Unfreeze player on exit
            player.IsPositionFrozen = false;
            Function.Call(Hash.ENABLE_ALL_CONTROL_ACTIONS, 0);

            Function.Call(Hash.REMOVE_ANIM_DICT, ANIM_DICT);
        }
    }
}
