using System;
using GTA;
using GTA.Math;
using StoreRobberyTrackerMod.Data;
using StoreRobberyTrackerMod.Debug;
using StoreRobberyTrackerMod.Minigame;

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
                    msg = "Robbery was not active — activating now";
                    DebugLogger.Info(msg);
                    return false;
                }

                if (store.SafeCracked)
                {
                    msg = "Safe already cracked";
                    DebugLogger.Info(msg);
                    return false;
                }

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

                _ctx.SafeCrack.Start(store.SafePos, store.SafeHeading, Game.Player.Character);

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
                _ctx.SafeCrack.Update();
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
