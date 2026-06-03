using GTA;
using GTA.Math;
using StoreRobberyTrackerMod.Data;
using StoreRobberyTrackerMod.Debug;
using StoreRobberyTrackerMod.Minigame;
using System;

namespace StoreRobberyTrackerMod.Systems
{
    internal class SafeSystem
    {
        private readonly StoreContext _ctx;

        public SafeSystem(StoreContext ctx)
        {
            _ctx = ctx;
        }

        // ------------------------------------------------------------
        // DEBUG: FORCE SAFE CRACK (INSTANT SUCCESS)
        // ------------------------------------------------------------
        public bool DebugForceSafeCrack(out string msg)
        {
            try
            {
                DebugLogger.Info("DebugForceSafeCrack() called — using NEW SafeCrack system");

                var store = _ctx.GetNearestStore();
                if (store == null)
                {
                    msg = "No store nearby";
                    DebugLogger.Info(msg);
                    return false;
                }

                if (!store.IsRobberyActive)
                {
                    store.IsRobberyActive = true;
                    DebugLogger.Info("Robbery was not active — activating now");
                }

                if (store.SafeCracked)
                {
                    msg = "Safe already cracked";
                    DebugLogger.Info(msg);
                    return false;
                }

                // ⭐ Clear stealth + alarm state
                store.SilentRobbery = false;
                store.AlarmTriggered = false;
                store.ClerkCallingPolice = false;
                store.SilentAlarmPressed = false;

                // Simulate instant success
                DebugLogger.Info("Simulating SafeCrack instant success...");

                int min = _ctx.Config.SafeMinAmount;
                int max = _ctx.Config.SafeMaxAmount;
                float multiplier = _ctx.Config.PayoutMultiplier;

                int baseReward = _ctx.Rng.Next(min, max + 1);
                int finalReward = (int)(baseReward * multiplier);

                DebugLogger.Info($"Simulated crack: base={baseReward}, multiplier={multiplier}, final={finalReward}");

                store.SafeCracked = true;
                store.PendingPayout += finalReward;
                store.PendingCompletion = true;

                DebugLogger.Info($"Debug safe crack complete: store={store.Id}, reward={finalReward}");

                _ctx.SaveStoreState(store);

                _ctx.Ui.ShowNotification("~g~[DEBUG] Safe cracked instantly");
                _ctx.Ui.ShowSubtitle($"~g~Safe cracked (debug) +${finalReward}", 3000);

                msg = $"Safe cracked (+${finalReward})";
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("SafeSystem.DebugForceSafeCrack", ex);
                msg = "Error";
                return false;
            }
        }

        // ------------------------------------------------------------
        // DEBUG: START REAL SAFECRACK MINIGAME
        // ------------------------------------------------------------
        public bool DebugStartSafeCrack(out string msg)
        {
            try
            {
                DebugLogger.Info("DebugStartSafeCrack() called — launching REAL SafeCrack minigame");

                if (_ctx.SafeCrack == null || _ctx.SafeState == null)
                {
                    msg = "SafeCrack system not initialized";
                    return false;
                }

                if (_ctx.SafeState.Active)
                {
                    msg = "SafeCrack already running";
                    return false;
                }

                var store = _ctx.GetNearestStore();
                if (store == null)
                {
                    msg = "No store nearby";
                    return false;
                }

                if (store.SafeCracked)
                {
                    msg = "Safe already cracked";
                    return false;
                }

                if (store.SafePos == Vector3.Zero)
                {
                    msg = "Store has no safe";
                    return false;
                }

                if (!store.IsRobberyActive)
                    store.IsRobberyActive = true;

                // ⭐ Enable stealth mode for debug SafeCrack
                store.SilentRobbery = true;
                store.AlarmTriggered = false;
                store.ClerkCallingPolice = false;
                store.SilentAlarmPressed = false;

                // ⭐ Start the minigame
                _ctx.SafeCrack.Start(store, store.SafePos, store.SafeHeading, Game.Player.Character);

                msg = "SafeCrack minigame started";
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("SafeSystem.DebugStartSafeCrack", ex);
                msg = "Error";
                return false;
            }
        }

        // ------------------------------------------------------------
        // UPDATE SAFECRACK MINIGAME (NEW SYSTEM)
        // ------------------------------------------------------------
        public void UpdateSafeCrackMiniGame()
        {
            try
            {
                // ⭐ If not active, nothing to do
                if (!_ctx.SafeState.Active)
                    return;

                // ⭐ Update logic
                _ctx.SafeCrack.Update();

                // ⭐ Draw UI (Main also draws, but this ensures debug mode works)
                _ctx.SafeCrackUI.Draw(_ctx.SafeState, _ctx.SafeCrackSettings);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("SafeSystem.UpdateSafeCrackMiniGame", ex);
            }
        }

        // ------------------------------------------------------------
        // CHECK IF ROBBERY IS FULLY DONE
        // ------------------------------------------------------------
        public bool IsRobberyFullyDone(TrackedStore store)
        {
            try
            {
                bool done = store.SafeCracked && store.PendingCompletion;
                DebugLogger.Trace($"IsRobberyFullyDone({store.Id}) = {done}");
                return done;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("SafeSystem.IsRobberyFullyDone", ex);
                return false;
            }
        }
    }
}
