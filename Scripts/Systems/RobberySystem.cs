using System;
using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyTrackerMod.Data;
using StoreRobberyTrackerMod.Debug;
using StoreRobberyTrackerMod.Minigame;

namespace StoreRobberyTrackerMod.Systems
{
    internal class RobberySystem
    {
        private readonly StoreContext _ctx;
        private int _lastTimerUpdate;

        // DEBUG TIMER FIELDS
        private bool _testTimerActive = false;
        private int _testTimerEnd = 0;
        // DEBUG ESCAPE STATE
        private bool _debugEscapeActive = false;
        private int _debugEscapeStoreId = -1;
        private int _lastDebugSubtitleTime = 0;

        public RobberySystem(StoreContext ctx)
        {
            try
            {
                _ctx = ctx;
                _lastTimerUpdate = 0;

                DebugLogger.Info("RobberySystem initialized");

                // ⭐ Hook SafeCrack payout into robbery flow
                // SafeCrack DOES NOT pay the player directly.
                // It ONLY:
                //  - Marks the safe as cracked
                //  - Adds its payout into store.PendingPayout
                //SafeCrackEvents.SafeCracked += (pos, payout) =>
                //{
                //    try
                //    {
                //        var store = _ctx.GetNearestStore();
                //        if (store == null)
                //            return;

                //        // Mark safe as cracked
                //        store.SafeCracked = true;

                //        // Add safe payout into robbery total
                //        store.PendingPayout += payout;

                //        DebugLogger.Info($"[SafeCrack] Store {store.Id} safe cracked, added payout={payout}, totalPending={store.PendingPayout}");
                //    }
                //    catch (Exception ex)
                //    {
                //        DebugLogger.LogException("RobberySystem.SafeCrackEvents.SafeCracked", ex);
                //    }
                //};

                if (_ctx.Config.EnableDebug && _ctx.Config.EnableDebugTimer)
                {
                    _testTimerActive = true;
                    _testTimerEnd = Game.GameTime + 15000;

                    DebugLogger.Info("Debug timer enabled (15 seconds)");
                    _ctx.Ui.SetTimerText("TEST TIMER: 15", 15);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.ctor", ex);
            }
        }

        // ------------------------------------------------------------
        // DEBUG ROBBERY START
        // ------------------------------------------------------------
        public bool TryStartDebugRobbery(out string msg)
        {
            try
            {
                var store = _ctx.GetNearestStore();
                if (store == null)
                {
                    msg = "No store nearby";
                    DebugLogger.Info(msg);
                    return false;
                }

                if (store.IsRobbed || store.CooldownActive)
                {
                    msg = "Store already robbed or on cooldown";
                    DebugLogger.Info(msg);
                    return false;
                }

                DebugLogger.Info($"Debug robbery started at store {store.Id}");
                StartRegisterRobbery(store);

                msg = store.Name;
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.TryStartDebugRobbery", ex);
                msg = "Error";
                return false;
            }
        }

        // Overload for SafeCrack integration
        public int DebugForcePayout(TrackedStore store)
        {
            // Forward to the existing parameterless version
            return DebugForcePayout();
        }

        // Overload for SafeCrack integration
        public void DebugForceEscape(TrackedStore store)
        {
            // Forward to the existing parameterless version
            DebugForceEscape();
        }

        // ------------------------------------------------------------
        // DEBUG FORCE ESCAPE
        // ------------------------------------------------------------
        public void DebugForceEscape()
        {
            try
            {
                var store = _ctx.GetNearestStore();
                if (store == null)
                    return;

                // ⭐ Mark this as a debug escape run
                _debugEscapeActive = true;
                _debugEscapeStoreId = store.Id;

                // ⭐ Clear any stuck police state
                Game.Player.WantedLevel = 0;
                store.AlarmTriggered = false;
                store.PlayerMaskedAtStart = false;

                // ⭐ Enable debug police suppression
                _ctx.Police.SuppressPoliceForDebug = true;

                // ⭐ Force ALL required robbery state
                store.IsRobbed = true;
                store.IsRobberyActive = true;
                store.SafeCracked = true;
                store.PendingCompletion = true;
                store.CooldownActive = false;
                store.AlarmTriggered = false;

                // ⭐ Prevent camera auto-complete
                foreach (var cam in store.Cameras)
                    cam.Destroyed = false;

                // ⭐ Simulate robbery start so the REAL timer runs
                store.RobberyStartUtc = DateTime.UtcNow;

                // ⭐ Ensure payout exists
                if (store.PendingPayout <= 0)
                    store.PendingPayout = _ctx.Rng.Next(2500, 50000);

                DebugLogger.Info($"DebugForceEscape armed for store {store.Id}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.DebugForceEscape", ex);
            }
        }

        // ------------------------------------------------------------
        // DEBUG FORCE PAYOUT
        // ------------------------------------------------------------
        public int DebugForcePayout()
        {
            try
            {
                var store = _ctx.GetNearestStore();
                if (store == null)
                    return 0;

                // ⭐ Mark this as a debug escape run
                _debugEscapeActive = true;
                _debugEscapeStoreId = store.Id;

                // ⭐ Suppress ALL police (both systems)
                _ctx.Police.SuppressPoliceForDebug = true;

                // ⭐ Force all required robbery state
                store.IsRobbed = true;
                store.IsRobberyActive = true;
                store.SafeCracked = true;
                store.PendingCompletion = true;
                store.CooldownActive = false;
                store.AlarmTriggered = false;

                // ⭐ Prevent camera auto-complete (same fix as DebugForceEscape)
                foreach (var cam in store.Cameras)
                    cam.Destroyed = false;

                // ⭐ Ensure payout exists
                if (store.PendingPayout <= 0)
                    store.PendingPayout = _ctx.Rng.Next(2500, 50000);

                float dist = Game.Player.Character.Position.DistanceTo(store.StorePos);

                // ⭐ If player is still inside escape radius → show persistent subtitle
                if (dist < _ctx.Config.EscapeDistance)
                {
                    _ctx.Ui.ShowSubtitle("Robbery complete! Escape the area.", 3000);
                    return 0;
                }

                // ⭐ Player is far enough → complete robbery
                DebugLogger.Info($"DebugForcePayout: awarding payout + cooldown for store {store.Id}");

                store.IsRobberyActive = false;

                AwardPayout(store);
                BeginCooldown(store);

                int payout = store.PendingPayout;

                // ⭐ Disable debug suppression
                _ctx.Police.SuppressPoliceForDebug = false;

                return payout;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.DebugForcePayout", ex);
                return 0;
            }
        }

        // ------------------------------------------------------------
        // DEBUG RESET STORE
        // ------------------------------------------------------------
        public void DebugResetStore(TrackedStore store)
        {
            // Core robbery state
            store.IsRobbed = false;
            store.IsRobberyActive = false;
            store.SafeCracked = false;
            store.PendingCompletion = false;
            store.PendingPayout = 0;
            store.CooldownActive = false;
            store.AlarmTriggered = false;
            store.PlayerMaskedAtStart = false;

            // Escalation / behavior flags
            store.ClerkDeathHandled = false;
            store.ClerkCallingPolice = false;
            store.SilentAlarmPressed = false;
            store.RepeatRobberyEscalationApplied = false;
            store.MaskEscalationApplied = false;
            store.FightEscalationApplied = false;
            store.TimeEscalationApplied = false;
            store.ClerkRecognizedPlayer = false;
            store.ClerkKilledWithGun = false;

            // 🔸 Completely clear timestamps so INI can write them as blank
            store.LastRobbedUtc = DateTime.MinValue;
            store.RobberyStartUtc = DateTime.MinValue;

            // 🔸 Reset all cameras including grace fields
            // No longer being used.
            //for (int i = 0; i < store.Cameras.Count; i++)
            //{
            //    var cam = store.Cameras[i];
            //    cam.Destroyed = false;
            //    cam.GraceActive = false;
            //    cam.GraceStartUtc = DateTime.MinValue;
            //}

            if (store.DummyClerk != null && store.DummyClerk.Exists())
            {
                store.DummyClerk.Delete();
                store.DummyClerk = null;
            }

            // Clear wanted level and police suppression
            Game.Player.WantedLevel = 0;
            _ctx.Police.SuppressPoliceForDebug = false;
            _ctx.Clerks.SpawnDummyClerk(store);

            DebugLogger.Info($"DebugResetStore: store {store.Id} fully reset");

            // Persist clean state to INI
            _ctx.SaveStoreState(store);

            // Optional: force immediate INI rewrite if your save routine buffers writes
            // _ctx.FileManager.WriteStoreSection(store.Id, store);
        }

        // ------------------------------------------------------------
        // MAIN ENTRY
        // ------------------------------------------------------------
        public void UpdateRobbery(TrackedStore store, Ped player)
        {
            try
            {
                // ⭐ Pause ALL robbery logic while SafeCrack is running
                // ⭐ Prevent robbery subtitles from firing during SafeCrack
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                {
                    _ctx.Ui.ClearTimer();
                    return;
                }

                // 🔸 Dedicated debug-escape subtitle loop
                if (_debugEscapeActive && store.Id == _debugEscapeStoreId && !store.CooldownActive)
                {
                    float dist = player.Position.DistanceTo(store.StorePos);

                    if (dist < _ctx.Config.EscapeDistance)
                    {
                        // Throttle to once per second so it doesn’t spam
                        if (Game.GameTime - _lastDebugSubtitleTime > 1000)
                        {
                            _ctx.Ui.ShowSubtitle("Robbery complete! Escape the area.", 3000);
                            _lastDebugSubtitleTime = Game.GameTime;
                        }
                    }
                }

                // ⭐ If robbery is active, run core robbery logic
                if (store.IsRobbed)
                {
                    UpdateRobberyTimer(store);
                    CheckCameraTriggeredAlarm(store);
                    CheckLeavingEarly(store, player);
                    CheckEarlyEscapeSuccess(store, player);
                }

                // DEBUG TIMER
                if (_ctx.Config.EnableDebugTimer && _testTimerActive)
                {
                    int remaining = (_testTimerEnd - Game.GameTime) / 1000;

                    if (remaining < 0)
                    {
                        DebugLogger.Info("Debug timer expired — showing heist banner");

                        _testTimerActive = false;
                        _ctx.Ui.ClearTimer();
                        _ctx.Ui.ShowHeistPassedBanner("~o~ROBBERY COMPLETE", "~y~Earned $100000");
                        return;
                    }
                    else
                    {
                        _ctx.Ui.SetTimerText($"TEST TIMER: {remaining}", remaining);
                    }

                    return;
                }

                // NORMAL LOGIC
                if (store.CooldownActive)
                    return;

                TryStartRegisterRobbery(store, player);

                // ⭐ Prevent ANY system from restarting SafeCrack while active
                if (_ctx.SafeState.Active)
                    return;

                if (store.IsRobbed)
                {
                    UpdateRobberyTimer(store);
                    CheckCameraTriggeredAlarm(store);
                    CheckLeavingEarly(store, player);
                    CheckEarlyEscapeSuccess(store, player);
                }

                // ------------------------------------------------------------
                // ⭐ SAFECRACK INTERACTION TRIGGER (FINAL PATCHED)
                // ------------------------------------------------------------
                if (store.IsRobbed &&
                    store.SafePos != Vector3.Zero &&
                    !store.SafeCracked)
                {
                    // If SafeCrack is already running → do nothing
                    if (_ctx.SafeState.Active)
                        return;

                    float safeDist = player.Position.DistanceTo(store.SafePos);

                    if (safeDist < 1.2f)
                    {
                        _ctx.Ui.ShowHelpText("Press ~y~E~w~ to crack the safe");

                        // Only start ONCE when E is PRESSED THIS FRAME
                        if (Game.IsControlJustPressed(GTA.Control.Context))
                        {
                            DebugLogger.Info($"Starting SafeCrack at store {store.Id}");
                            _ctx.SafeCrack.Start(store, store.SafePos, store.SafeHeading, player);
                        }
                    }
                }

                // ------------------------------------------------------------
                // COMPLETION LOGIC
                // ------------------------------------------------------------
                if (store.IsRobbed &&
                    store.PendingCompletion &&
                    store.PendingPayout > 0 &&
                    (store.SafePos == Vector3.Zero || store.SafeCracked))
                {
                    CompleteRobbery(store, player);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.UpdateRobbery", ex);
            }
        }

        // ------------------------------------------------------------
        // START ROBBERY
        // ------------------------------------------------------------
        private void TryStartRegisterRobbery(TrackedStore store, Ped player)
        {
            try
            {
                if (_ctx.Config.EnableDebugTimer && _testTimerActive)
                    return;

                if (store.IsRobbed)
                    return;

                if (store.Clerk == null || !store.Clerk.Exists())
                    return;

                float dist = player.Position.DistanceTo(store.Clerk.Position);
                if (dist < 12f && _ctx.Player.IsAiming() && _ctx.Player.IsArmed())
                {
                    store.PlayerMaskedAtStart = _ctx.Player.IsMasked();

                    DebugLogger.Info($"Robbery started at store {store.Id}");
                    StartRegisterRobbery(store);

                    if (_ctx.Config.EnableMessages)
                        _ctx.Ui.ShowNotification("~y~Robbery started!");

                    if (_ctx.Config.EnableStalkerMsg)
                        _ctx.Stalker.QueueRobberyMessage();

                    Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "TIMER_STOP", "HUD_MINI_GAME_SOUNDSET");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.TryStartRegisterRobbery", ex);
            }
        }

        private void StartRegisterRobbery(TrackedStore store)
        {
            try
            {
                store.IsRobbed = true;
                store.RobberyStartUtc = DateTime.UtcNow;
                store.PendingCompletion = true;

                int payout = _ctx.Rng.Next(_ctx.Config.RegisterMinAmount, _ctx.Config.RegisterMaxAmount + 1);
                payout = (int)(payout * _ctx.Config.PayoutMultiplier);

                store.PendingPayout += payout;

                DebugLogger.Info($"Register robbery payout: store={store.Id}, payout={payout}");

                // Subtitle #1
                _ctx.Ui.ShowSubtitle("Rob the store and escape.", 3000);

                // ⭐ If store has a safe, show follow-up subtitle
                if (store.SafePos != Vector3.Zero)
                {
                    Script.Wait(3200);
                    _ctx.Ui.ShowSubtitle("There is a safe in the office — crack it too.", 4000);
                }

                _ctx.SaveStoreState(store);
                _ctx.Cooldowns.UpdateStoreBlip(store);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.StartRegisterRobbery", ex);
            }
        }

        // ------------------------------------------------------------
        // ROBBERY TIMER
        // ------------------------------------------------------------
        private void UpdateRobberyTimer(TrackedStore store)
        {
            try
            {
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                {
                    _ctx.Ui.ClearTimer();
                    return;
                }

                if (store.AlarmTriggered)
                    return;

                double elapsed = (DateTime.UtcNow - store.RobberyStartUtc).TotalSeconds;
                int remaining = _ctx.Config.RobberyTimeLimit - (int)elapsed;

                if (remaining <= 0)
                {
                    DebugLogger.Info($"Robbery timer expired for store {store.Id}");
                    TriggerPoliceIfNeeded(store);
                    _ctx.Ui.ClearTimer();
                    return;
                }

                if (Game.GameTime - _lastTimerUpdate > 1000)
                {
                    int mm = remaining / 60;
                    int ss = remaining % 60;

                    _ctx.Ui.SetTimerText($"Police in: {mm:00}:{ss:00}", remaining);
                    _lastTimerUpdate = Game.GameTime;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.UpdateRobberyTimer", ex);
            }
        }

        private void TriggerPoliceIfNeeded(TrackedStore store)
        {
            try
            {
                // ⭐ Debug override — completely disable timer-based police
                if (_ctx.Police.SuppressPoliceForDebug)
                {
                    DebugLogger.Info("TriggerPoliceIfNeeded suppressed for debug");
                    return;
                }

                _ctx.Ui.ClearTimer();

                if (store.PlayerMaskedAtStart)
                {
                    Game.Player.WantedLevel = 1;

                    if (_ctx.Config.EnableMessages)
                        _ctx.Ui.ShowNotification("~y~Police searching the area.");
                }
                else
                {
                    Game.Player.WantedLevel = 2;

                    if (_ctx.Config.EnableMessages)
                        _ctx.Ui.ShowNotification("~r~Police alerted!");
                }

                store.AlarmTriggered = true;

                DebugLogger.Info($"Police triggered by timer for store {store.Id}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.TriggerPoliceIfNeeded", ex);
            }
        }

        // ------------------------------------------------------------
        // CAMERA-BASED ALARM
        // ------------------------------------------------------------
        private void CheckCameraTriggeredAlarm(TrackedStore store)
        {
            try
            {
                // ⭐ Disable camera alarms during debug escape
                if (_debugEscapeActive)
                    return;

                if (store.AlarmTriggered)
                    return;

                if (!_ctx.Config.EnableCameras)
                    return;

                int count = store.Cameras.Count;
                for (int i = 0; i < count; i++)
                {
                    CameraData cam = store.Cameras[i];

                    if (cam.Destroyed)
                        continue;

                    // Camera sees dead clerk
                    if (store.ClerkDeathHandled && !store.ClerkKilledWithGun)
                    {
                        DebugLogger.Info($"Camera detected dead clerk at store {store.Id}");

                        Game.Player.WantedLevel = 2;
                        store.AlarmTriggered = true;

                        if (_ctx.Config.EnableMessages)
                            _ctx.Ui.ShowNotification("~r~Camera detected the dead clerk!");

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.CheckCameraTriggeredAlarm", ex);
            }
        }

        // ------------------------------------------------------------
        // DON'T LEAVE YET WARNING
        // ------------------------------------------------------------
        private void CheckLeavingEarly(TrackedStore store, Ped player)
        {
            if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                return;

            // ⭐ Updated to use SafeCracked
            if (store.IsRobbed &&
                !store.SafeCracked &&
                store.SafePos != new Vector3(0f, 0f, 0f) &&
                !store.CooldownActive)
            {
                if (player.Position.DistanceTo(store.StorePos) > 10f)
                {
                    _ctx.Ui.ShowNotification("~y~Don't leave yet! Crack the safe to finish the robbery.");
                }
            }
        }

        // ------------------------------------------------------------
        // EARLY ESCAPE
        // ------------------------------------------------------------
        private void CheckEarlyEscapeSuccess(TrackedStore store, Ped player)
        {
            try
            {
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return;

                // ⭐ DEBUG ESCAPE OVERRIDE
                if (_debugEscapeActive && store.Id == _debugEscapeStoreId)
                {
                    float distdebug = player.Position.DistanceTo(store.StorePos);

                    if (distdebug < _ctx.Config.EscapeDistance)
                    {
                        _ctx.Ui.ShowSubtitle("Robbery complete! Escape the area.", 3000);
                        return;
                    }

                    DebugLogger.Info($"Debug escape success at store {store.Id}");

                    store.IsRobberyActive = false;
                    AwardPayout(store);
                    BeginCooldown(store);
                    return;
                }

                // ⭐ Require safe cracked for early escape
                if (!store.IsRobbed || !store.SafeCracked)
                    return;

                if (store.AlarmTriggered)
                    return;

                if (Game.Player.WantedLevel > 0)
                {
                    _ctx.Ui.ShowSubtitle("Escape the area & lose the cops.", 3000);
                    return;
                }

                float dist = player.Position.DistanceTo(store.StorePos);

                // All cameras must be destroyed
                bool allCamsDown = true;
                int count = store.Cameras.Count;
                for (int i = 0; i < count; i++)
                {
                    if (!store.Cameras[i].Destroyed)
                    {
                        allCamsDown = false;
                        break;
                    }
                }

                // ⭐ Early escape success
                if (dist > _ctx.Config.EscapeDistance && allCamsDown)
                {
                    DebugLogger.Info($"Early escape success at store {store.Id}");

                    store.IsRobberyActive = false;

                    AwardPayout(store);
                    BeginCooldown(store);

                    return;
                }
                else
                {
                    _ctx.Ui.ShowSubtitle("Robbery complete! Escape the area.");
                    return;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.CheckEarlyEscapeSuccess", ex);
            }
        }

        // ------------------------------------------------------------
        // FINAL COMPLETION
        // ------------------------------------------------------------
        private void CompleteRobbery(TrackedStore store, Ped player)
        {
            try
            {
                // Require safe cracked if store has a safe
                if (store.SafePos != Vector3.Zero && !store.SafeCracked)
                {
                    _ctx.Ui.ShowSubtitle("Crack the safe to finish the robbery.", 3000);
                    return;
                }

                if (!store.PendingCompletion || store.PendingPayout <= 0)
                    return;

                if (Game.Player.WantedLevel > 0)
                {
                    _ctx.Ui.ShowSubtitle("Escape the area & lose the cops.", 3000);
                    return;
                }

                // Debug escape still respects distance gate
                if (_debugEscapeActive && store.Id == _debugEscapeStoreId)
                {
                    float distdebug = player.Position.DistanceTo(store.StorePos);

                    if (distdebug < _ctx.Config.EscapeDistance)
                    {
                        _ctx.Ui.ShowSubtitle("Robbery complete! Escape the area.", 3000);
                        return;
                    }

                    DebugLogger.Info($"Robbery completion (debug escape) for store {store.Id}");
                    store.IsRobberyActive = false;
                    AwardPayout(store);
                    BeginCooldown(store);
                    return;
                }

                float dist = player.Position.DistanceTo(store.StorePos);
                if (dist < _ctx.Config.EscapeDistance)
                {
                    _ctx.Ui.ShowSubtitle("Robbery complete! Escape the area.", 3000);
                    return;
                }

                DebugLogger.Info($"Robbery completion triggered for store {store.Id}");

                store.IsRobberyActive = false;

                AwardPayout(store);
                BeginCooldown(store);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.CompleteRobbery", ex);
            }
        }

        // ------------------------------------------------------------
        // PAYOUT + COOLDOWN
        // ------------------------------------------------------------
        private void AwardPayout(TrackedStore store)
        {
            try
            {
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return;

                store.IsRobberyActive = false;

                // ⭐ Capture debug state BEFORE clearing it
                bool wasDebugEscape = _debugEscapeActive;
                int payout = store.PendingPayout;

                DebugLogger.Info($"Awarding payout: store={store.Id}, payout={payout}");

                // ⭐ Debug escape → do NOT pay player
                if (wasDebugEscape)
                    Game.Player.Money += 0;
                else
                    Game.Player.Money += payout;

                _ctx.Ui.ClearTimer();

                Script.Yield(); // Prevent UI overwrite

                _ctx.Ui.ShowHeistPassedBanner("~o~ROBBERY COMPLETE", "~y~Earned $" + payout);

                store.PendingPayout = 0;
                store.PendingCompletion = false;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.AwardPayout", ex);
            }
        }

        private void BeginCooldown(TrackedStore store)
        {
            try
            {
                DebugLogger.Info($"BeginCooldown({store.Id})");

                store.IsRobbed = true;
                store.LastRobbedUtc = DateTime.UtcNow;
                store.CooldownActive = true;

                _ctx.Cooldowns.ApplyCooldownBlocker(store);
                _ctx.Cooldowns.UpdateStoreBlip(store);
                _ctx.SaveStoreState(store);
                _ctx.Blips.RefreshBlip(store.Id);

                if (_ctx.Config.EnableStalkerMsg)
                    _ctx.Stalker.QueueEscapeMessage();

                _ctx.Stalker.TryTriggerCall();

                // ⭐ Capture debug state BEFORE clearing it
                bool wasDebugEscape = _debugEscapeActive;

                // ⭐ Auto-reset store AFTER payout + cooldown
                if (wasDebugEscape)
                {
                    _debugEscapeActive = false;
                    _debugEscapeStoreId = -1;
                    DebugLogger.Info("Debug escape state cleared after cooldown.");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.BeginCooldown", ex);
            }
        }
    }
}
