using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;
using System;

namespace StoreRobberyEnhanced.Systems
{
    internal class ClerkSystem
    {
        private readonly StoreContext _ctx;
        private readonly Random _rng;

        public ClerkSystem(StoreContext ctx)
        {
            _ctx = ctx;
            _rng = new Random();
        }

        private bool IsThreateningSoft(Ped player, Ped clerk)
        {
            if (player == null || !player.Exists() || clerk == null || !clerk.Exists())
                return false;

            // Direct aim at clerk
            if (Function.Call<bool>(Hash.IS_PLAYER_FREE_AIMING_AT_ENTITY, Game.Player, clerk))
                return true;

            // Weapon out + close range
            if (player.Weapons.Current.Hash != WeaponHash.Unarmed &&
                player.Position.DistanceTo(clerk.Position) < 4.5f)
                return true;

            // Gun pointed (even if not aiming directly at clerk)
            if (Game.IsControlPressed(Control.Aim) &&
                player.Weapons.Current.Hash != WeaponHash.Unarmed)
                return true;

            return false;
        }

        // ------------------------------------------------------------
        // MAIN UPDATE
        // ------------------------------------------------------------
        public void UpdateClerk(TrackedStore store, Ped player)
        {
            try
            {
                if (store == null)
                    return;

                if (store.CooldownActive)
                    return;

                if (player == null || !player.Exists())
                    return;

                // Track player inside store
                store.IsPlayerInsideStore =
                    player.Position.DistanceTo(store.StorePos) <= store.Radius;

                // BLOCK spawning until replacement system has removed defaults
                if (!store.DefaultClerkRemoved)
                    return;

                // Ensure we have a real clerk
                if (store.Clerk == null || !store.Clerk.Exists())
                {
                    SpawnClerk(store);
                    store.IsRobberyActive = false;
                    store.ClerkReacted = false;
                    store.HeatLevel = 0; // or whatever your heat variable is

                    return;
                }

                Ped clerk = store.Clerk;

                if (clerk == null || !clerk.Exists())
                    return;

                // ⭐ SAFETY RESET: only if clerk is actually stuck
                bool usingScenario = Function.Call<bool>(Hash.IS_PED_USING_ANY_SCENARIO, clerk);

                if (clerk.IsRagdoll || usingScenario)
                {
                    DebugLogger.Info(string.Format(
                        "[RESET] Forcing task clear on clerk {0} (ragdoll={1} scenario={2})",
                        clerk.Handle,
                        clerk.IsRagdoll,
                        usingScenario
                    ));

                    Function.Call(Hash.CLEAR_PED_TASKS_IMMEDIATELY, clerk);
                }

                // HARD GUARD: never run behavior on dummy clerk
                if (store.DummyClerk != null && store.DummyClerk.Exists() &&
                    clerk.Handle == store.DummyClerk.Handle)
                {
                    return;
                }

                if (clerk.IsDead)
                {
                    if (store.IsOurClerk)
                        HandleClerkDeath(store);
                    return;
                }

                // Normal idle logic
                if (!store.ClerkReacted &&
                    !store.ClerkStalling &&
                    !store.ClerkOpeningRegister &&
                    !store.ClerkGrabbingCash &&
                    !store.ClerkThrowingBag &&
                    !store.ClerkPanicking &&
                    !store.ClerkFleeing)
                {
                    RunIdleBehavior(store, clerk);
                }

                // ------------------------------------------------------------
                // REACTION / ROBBERY LOGIC
                // ------------------------------------------------------------
                // Threat detection
                if (!store.ClerkReacted && IsThreateningSoft(player, clerk))
                {
                    BeginFearReaction(store, clerk);
                    return;
                }
                //if (!store.ClerkReacted && _ctx.Player.IsThreatening(clerk))
                //{
                //    BeginFearReaction(store, clerk);
                //    return;
                //}

                // Stall
                if (store.ClerkStalling)
                {
                    ProcessStall(store, clerk);
                    return;
                }

                // Register opening
                if (store.ClerkOpeningRegister)
                {
                    ProcessRegisterOpening(store, clerk);
                    return;
                }

                // Cash grab
                if (store.ClerkGrabbingCash)
                {
                    ProcessCashGrab(store, clerk);
                    return;
                }

                // Bag toss
                if (store.ClerkThrowingBag)
                {
                    ProcessBagToss(store, clerk);
                    return;
                }

                // Panic / flee
                if (store.ClerkPanicking)
                {
                    ProcessPanic(store, clerk);
                    return;
                }

                // Fleeing is disabled, but if it somehow triggers, override with surrender or feelfroggy fight back logic chance
                if (store.ClerkFleeing)
                {
                    ProcessFlee(store, clerk);
                    return;
                }

                // After bag toss, clerk may call police or press silent alarm
                TryTriggerSilentAlarm(store, clerk);
                TryTriggerPoliceCall(store, clerk, player);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.UpdateClerk", ex);
            }
    }

        // ------------------------------------------------------------
        // HELPER: Determine if a ped is one of our custom clerks
        // ------------------------------------------------------------
        public bool IsOurClerk(Ped ped)
        {
            if (ped == null || !ped.Exists())
                return false;

            foreach (TrackedStore s in _ctx.Stores)
            {
                if (s.Clerk != null && s.Clerk.Exists() && s.Clerk.Handle == ped.Handle)
                    return true;
            }

            return false;
        }

        // ------------------------------------------------------------
        // SPAWN CLERK
        // ------------------------------------------------------------
        private void SpawnClerk(TrackedStore store)
        {
            try
            {
                if (store == null)
                    return;

                // If clerk already exists, do nothing
                if (store.Clerk != null && store.Clerk.Exists())
                    return;

                // Replace with our clerk model
                Ped clerk = World.CreatePed(PedHash.Business01AMM, store.ClerkPos, store.ClerkHeading);

                if (clerk == null || !clerk.Exists())
                    return;

                store.IsOurClerk = true;
                store.Clerk = clerk;

                // ⭐ Record spawn time for interior detach logic
                store.ClerkSpawnTime = Game.GameTime;

                clerk.IsPersistent = true;
                clerk.BlockPermanentEvents = true;

                store.ClerkIdle = true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.SpawnClerk", ex);
            }
        }

        // ------------------------------------------------------------
        // FORCE SPAWN CLERK (Used by ClerkReplacementSystem)
        // ------------------------------------------------------------
        public void ForceSpawnClerk(TrackedStore store)
        {
            try
            {
                if (store == null)
                    return;

                // If clerk already exists, do nothing
                if (store.Clerk != null && store.Clerk.Exists())
                return;

                Ped clerk = World.CreatePed(PedHash.Business01AMM, store.ClerkPos, store.ClerkHeading);

                if (clerk == null || !clerk.Exists())
                    return;

                store.Clerk = clerk;

                // ⭐ Record spawn time for interior detach logic
                store.ClerkSpawnTime = Game.GameTime;

                store.IsOurClerk = true;

                clerk.IsPersistent = true;
                clerk.BlockPermanentEvents = true;
                clerk.Task.ClearAllImmediately();

                // Idle state
                store.ClerkIdle = true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ForceSpawnClerk", ex);
            }
        }

        // ------------------------------------------------------------
        // SPAWN DUMMY CLERK
        // ------------------------------------------------------------
        public void SpawnDummyClerk(TrackedStore store)
        {
            try
            {
                if (store == null)
                    return;

                // If dummy already exists, skip
                if (store.DummyClerk != null && store.DummyClerk.Exists())
                    return;

                // Spawn underground so player never sees it
                Vector3 spawnPos = store.ClerkPos + new Vector3(0f, 0f, -10f);

                Ped dummy = World.CreatePed(PedHash.ShopKeep01, spawnPos, store.ClerkHeading);

                if (dummy == null || !dummy.Exists())
                {
                    DebugLogger.Warn($"SpawnDummyClerk: Failed to spawn dummy clerk for store {store.Id}");
                    return;
                }

                store.DummyClerk = dummy;

                // Make invisible and non-interactive
                dummy.IsVisible = false;
                dummy.IsCollisionEnabled = false;
                dummy.IsInvincible = true;
                dummy.IsPersistent = true;
                dummy.BlockPermanentEvents = true;

                dummy.Task.ClearAllImmediately();
                dummy.IsPositionFrozen = true;

                Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, dummy.Handle, true, true);

                DebugLogger.Info($"SpawnDummyClerk: Dummy clerk spawned for store {store.Id}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.SpawnDummyClerk", ex);
            }
        }

        // ------------------------------------------------------------
        // SMALL HELPER: ANIM CHECK
        // ------------------------------------------------------------
        private bool IsPlayingAnim(Ped ped, string dict, string name)
        {
            if (ped == null || !ped.Exists())
                return false;

            try
            {
                return Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, ped.Handle, dict, name, 3);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.IsPlayingAnim", ex);
                return false;
            }
        }

        // ------------------------------------------------------------
        // NATIVE ANIMATION WRAPPER (SHVDN 3.9.0 SAFE, SIMPLE REQUEST)
        // ------------------------------------------------------------
        private void PlayAnimNative(Ped ped, string dict, string anim, AnimationFlags flags)
        {
            if (ped == null || !ped.Exists())
                return;

            // ⭐ BLOCK ALL HOLD-UP ANIMS (root motion)
            if (dict == "mp_am_hold_up")
            {
                DebugLogger.Info($"[ANIM-BLOCK] Suppressed root-motion anim {dict}/{anim} on ped {ped.Handle}");
                return;
            }

            try
            {
                Function.Call(Hash.REQUEST_ANIM_DICT, dict);

                DebugLogger.Info($"[ANIM] Requesting anim {dict}/{anim} on ped {ped.Handle}");

                Function.Call(
                    Hash.TASK_PLAY_ANIM,
                    ped.Handle,
                    dict,
                    anim,
                    8.0f,
                    -8.0f,
                    -1,
                    (int)flags,
                    0,
                    false, false, false
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogException($"ClerkSystem.PlayAnimNative {dict}/{anim}", ex);
            }
        }

        // ------------------------------------------------------------
        // IDLE BEHAVIOR
        // ------------------------------------------------------------
        private void RunIdleBehavior(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                if (store.ClerkReacted || store.ClerkStalling || store.ClerkOpeningRegister ||
                    store.ClerkGrabbingCash || store.ClerkThrowingBag ||
                    store.ClerkPanicking || store.ClerkFleeing)
                    return;

                if (!store.ClerkIdle)
                    return;

                // Cooldown so we don't spam anim requests
                int now = Game.GameTime;
                if (now - store.LastIdleTime < 4000) // 4‑second buffer
                    return;

                string dict = "amb@world_human_shopkeeper@male@idle_a";
                string[] idles = { "idle_a", "idle_b", "idle_c" };

                bool playing =
                    IsPlayingAnim(clerk, dict, "idle_a") ||
                    IsPlayingAnim(clerk, dict, "idle_b") ||
                    IsPlayingAnim(clerk, dict, "idle_c");

                if (!playing)
                {
                    string anim = idles[_rng.Next(idles.Length)];
                    DebugLogger.Info(string.Format("[IDLE] Starting idle '{0}' on clerk {1}", anim, clerk.Handle));
                    //clerk.Task.PlayAnimation(dict, anim, 4f, -1, AnimationFlags.Loop);
                    PlayAnimNative(clerk, dict, anim, AnimationFlags.Loop);

                    // Record timestamp so we don't restart immediately
                    store.LastIdleTime = now;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.RunIdleBehavior", ex);
            }
        }

        // ------------------------------------------------------------
        // FEAR REACTION
        // ------------------------------------------------------------
        private void BeginFearReaction(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                store.ClerkReacted = true;
                store.ClerkIdle = false;
                store.IsRobberyActive = true;

                clerk.Task.ClearAllImmediately();
                clerk.Task.HandsUp(-1);

                Function.Call(Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE, clerk, "SHOP_CLERK_REACT", "SPEECH_PARAMS_FORCE", 0);

                // Recognition escalation
                if (store.TimesRobbed >= 2)
                    store.ClerkRecognizedPlayer = true;

                // 🎲 Random chance to fight back (10–20% typical)
                int roll = _rng.Next(0, 100);
                if (roll < 15) // 15% chance to fight
                {
                    // Pick weapon type randomly
                    bool useShotgun = _rng.Next(0, 2) == 0;
                    store.ReactionType = useShotgun ? ClerkReactionType.FightShotgun : ClerkReactionType.FightPistol;

                    DebugLogger.Info($"Clerk at store {store.Id} decided to fight back ({store.ReactionType})");

                    // Trigger combat behavior immediately
                    ProcessfeelingFroggy(store, clerk);
                    return;
                }

                // Default panic behavior
                store.ReactionType = ClerkReactionType.NormalPanic;

                // Stall
                store.ClerkStalling = true;
                store.StallStartUtc = DateTime.UtcNow;
                store.StallDurationMs = _rng.Next(3000, 7000);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.BeginFearReaction", ex);
            }
        }

        // ------------------------------------------------------------
        // STALL PROCESSING
        // ------------------------------------------------------------
        private void ProcessStall(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                // Still stalling? Keep playing nervous idle
                if ((DateTime.UtcNow - store.StallStartUtc).TotalMilliseconds < store.StallDurationMs)
                {
                    // Ensure nervous idle is playing
                    if (!IsPlayingAnim(clerk, "missheist_agency2aig_2", "look_around_guard"))
                    {
                        Function.Call(Hash.REQUEST_ANIM_DICT, "missheist_agency2aig_2");

                        if (Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "missheist_agency2aig_2"))
                        {
                            Function.Call(
                                Hash.TASK_PLAY_ANIM,
                                clerk.Handle,
                                "missheist_agency2aig_2",
                                "look_around_guard",
                                4.0f,
                                -4.0f,
                                -1,
                                (int)AnimationFlags.Loop,
                                0f,
                                false, false, false
                            );
                        }
                    }

                    return;
                }

                // Stall finished → move to register opening
                store.ClerkStalling = false;

                clerk.Task.ClearAllImmediately();
                clerk.Position = store.RegisterPos;
                clerk.Heading = store.RegisterHeading;

                store.ClerkOpeningRegister = true;
                store.ClerkAnimStartUtc = DateTime.UtcNow;
                store.ClerkAnimDurationMs = 1800;

                //clerk.Task.PlayAnimation("mp_am_hold_up", "purchase_beer_shopkeeper", 8f, -1, AnimationFlags.None);
                PlayAnimNative(clerk, "mp_am_hold_up", "purchase_beer_shopkeeper", AnimationFlags.None);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessStall", ex);
            }
        }

        // ------------------------------------------------------------
        // REGISTER OPENING
        // ------------------------------------------------------------
        private void ProcessRegisterOpening(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                // Still playing the "enter" animation?
                if ((DateTime.UtcNow - store.ClerkAnimStartUtc).TotalMilliseconds < store.ClerkAnimDurationMs)
                    return;

                // First phase: play "enter"
                if (!store.ClerkGrabbingCash)
                {
                    store.ClerkGrabbingCash = true; // move to cash grab phase

                    clerk.Task.ClearAllImmediately();

                    Function.Call(Hash.REQUEST_ANIM_DICT, "anim@heists@ornate_bank@grab_cash");

                    if (Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "anim@heists@ornate_bank@grab_cash"))
                    {
                        // Clerk leans forward and opens the register
                        Function.Call(
                            Hash.TASK_PLAY_ANIM,
                            clerk.Handle,
                            "anim@heists@ornate_bank@grab_cash",
                            "enter",
                            8.0f,
                            -8.0f,
                            1500,
                            (int)AnimationFlags.None,
                            0f,
                            false, false, false
                        );
                    }

                    // Set timer for next phase
                    store.ClerkAnimStartUtc = DateTime.UtcNow;
                    store.ClerkAnimDurationMs = 1500;
                    return;
                }

                // Second phase: idle at open register
                store.ClerkGrabbingCash = false;
                store.ClerkThrowingBag = true;

                Function.Call(
                    Hash.TASK_PLAY_ANIM,
                    clerk.Handle,
                    "anim@heists@ornate_bank@grab_cash",
                    "idle",
                    8.0f,
                    -8.0f,
                    -1,
                    (int)AnimationFlags.Loop,
                    0f,
                    false, false, false
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessRegisterOpening", ex);
            }
        }

        // ------------------------------------------------------------
        // CASH GRAB
        // ------------------------------------------------------------
        private void ProcessCashGrab(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                // Still playing previous phase?
                if ((DateTime.UtcNow - store.ClerkAnimStartUtc).TotalMilliseconds < store.ClerkAnimDurationMs)
                    return;

                store.ClerkGrabbingCash = false;
                store.ClerkThrowingBag = true; // correct, but only AFTER anim finishes

                clerk.Task.ClearAllImmediately();

                // Load anim dict
                Function.Call(Hash.REQUEST_ANIM_DICT, "mp_common");

                if (Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "mp_common"))
                {
                    // Play the give-money animation
                    Function.Call(
                        Hash.TASK_PLAY_ANIM,
                        clerk.Handle,
                        "mp_common",
                        "givetake1_a",
                        8.0f,
                        -8.0f,
                        1500,
                        (int)AnimationFlags.None,
                        0f,
                        false, false, false
                    );
                }

                // Set timing for next phase
                store.ClerkAnimStartUtc = DateTime.UtcNow;
                store.ClerkAnimDurationMs = 1500;

                // Add payout
                int payout = _ctx.Rng.Next(_ctx.Config.RegisterMinAmount, _ctx.Config.RegisterMaxAmount + 1);
                payout = (int)(payout * _ctx.Config.PayoutMultiplier);
                store.PendingPayout += payout;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessCashGrab", ex);
            }
        }

        // ------------------------------------------------------------
        // BAG TOSS
        // ------------------------------------------------------------
        private void ProcessBagToss(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                // Wait for previous animation to finish
                if ((DateTime.UtcNow - store.ClerkAnimStartUtc).TotalMilliseconds < store.ClerkAnimDurationMs)
                    return;

                store.ClerkThrowingBag = false;

                // ⭐ Play the actual toss animation
                clerk.Task.ClearAllImmediately();
                Function.Call(Hash.REQUEST_ANIM_DICT, "mp_common");

                if (Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "mp_common"))
                {
                    Function.Call(
                        Hash.TASK_PLAY_ANIM,
                        clerk.Handle,
                        "mp_common",
                        "givetake2_a",   // toss animation
                        8.0f,
                        -8.0f,
                        1200,
                        (int)AnimationFlags.None,
                        0f,
                        false, false, false
                    );
                }

                // ⭐ Spawn the REAL loot bag on the floor
                _ctx.Robberies.SpawnLootBag(store, clerk);

                // ⭐ Force surrender instead of flee
                store.ClerkPanicking = false;
                store.ClerkFleeing = true; // triggers surrender handler
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessBagToss", ex);
            }
        }

        // ------------------------------------------------------------
        // PANIC
        // ------------------------------------------------------------
        private void ProcessPanic(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                // Simple cower behavior
                if (!clerk.IsInCombat && !clerk.IsFleeing)
                {
                    clerk.Task.ClearAllImmediately();
                    clerk.Task.Cower(-1);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessPanic", ex);
            }
        }

        // ------------------------------------------------------------
        // FLEE / SURRENDER OVERRIDE (SAFE, FULLY PATCHED)
        // ------------------------------------------------------------
        private void ProcessFlee(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                // Fleeing is disabled — clerks surrender instead
                store.ClerkFleeing = false;

                if (store.ClerkSurrenderStage == 0)
                {
                    StartClerkSurrender(store, clerk);
                }
                else
                {
                    UpdateClerkSurrender(store, clerk);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessFlee", ex);
            }
        }

        // ------------------------------------------------------------
        // CLERK SURRENDER & KNEEL HANDLING
        // ------------------------------------------------------------
        private void StartClerkSurrender(TrackedStore store, Ped clerk)
        {
            if (clerk == null || !clerk.Exists())
                return;

            clerk.Task.ClearAllImmediately();

            // Preload anims
            Function.Call(Hash.REQUEST_ANIM_DICT, "random@arrests");
            Function.Call(Hash.REQUEST_ANIM_DICT, "random@arrests@busted");

            // Stage 1: Hands up
            Function.Call(
                Hash.TASK_PLAY_ANIM,
                clerk.Handle,
                "random@arrests",
                "idle_2_hands_up",
                8.0f, -8.0f,
                1500,
                0,
                0f,
                false, false, false
            );

            store.ClerkSurrenderStage = 1;
            store.ClerkAnimStartUtc = DateTime.UtcNow;
            store.ClerkAnimDurationMs = 1500;
        }

        private void UpdateClerkSurrender(TrackedStore store, Ped clerk)
        {
            if (clerk == null || !clerk.Exists())
                return;

            double elapsed = (DateTime.UtcNow - store.ClerkAnimStartUtc).TotalMilliseconds;

            // Stage 1 → Stage 2
            if (store.ClerkSurrenderStage == 1 && elapsed >= store.ClerkAnimDurationMs)
            {
                Function.Call(
                    Hash.TASK_PLAY_ANIM,
                    clerk.Handle,
                    "random@arrests@busted",
                    "enter",
                    8.0f, -8.0f,
                    2000,
                    0,
                    0f,
                    false, false, false
                );

                store.ClerkSurrenderStage = 2;
                store.ClerkAnimStartUtc = DateTime.UtcNow;
                store.ClerkAnimDurationMs = 2000;
                return;
            }

            // Stage 2 → Stage 3
            if (store.ClerkSurrenderStage == 2 && elapsed >= store.ClerkAnimDurationMs)
            {
                Function.Call(
                    Hash.TASK_PLAY_ANIM,
                    clerk.Handle,
                    "random@arrests@busted",
                    "idle_a",
                    8.0f, -8.0f,
                    -1,
                    1,
                    0f,
                    false, false, false
                );

                store.ClerkSurrenderStage = 3; // final
                return;
            }
        }

        // ------------------------------------------------------------
        // FIGHT OR FLIGHT PISTOL / SHOTGUN
        // ------------------------------------------------------------
        private void ProcessfeelingFroggy(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                if ((DateTime.UtcNow - store.ClerkAnimStartUtc).TotalMilliseconds < store.ClerkAnimDurationMs)
                    return;

                Ped player = Game.Player.Character;

                switch (store.ReactionType)
                {
                    case ClerkReactionType.FightPistol:
                        clerk.Weapons.Give(WeaponHash.Pistol, 60, true, true);
                        if (player != null && player.Exists())
                            clerk.Task.Combat(player);
                        break;

                    case ClerkReactionType.FightShotgun:
                        clerk.Weapons.Give(WeaponHash.PumpShotgun, 20, true, true);
                        if (player != null && player.Exists())
                            clerk.Task.Combat(player);
                        break;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessBagToss", ex);
            }
        }

        // ------------------------------------------------------------
        // SILENT ALARM
        // ------------------------------------------------------------
        private void TryTriggerSilentAlarm(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                if (store.SilentAlarmPressed)
                    return;

                if (!store.ClerkReacted)
                    return;

                // Chance-based trigger
                int chance = store.ClerkRecognizedPlayer ? 40 : 20;
                if (_rng.Next(0, 100) >= chance)
                    return;

                // Mark alarm pressed
                store.SilentAlarmPressed = true;
                store.SilentAlarmUtc = DateTime.UtcNow;

                // Clear tasks so animation can play
                clerk.Task.ClearAllImmediately();

                // Load keypad animation
                Function.Call(Hash.REQUEST_ANIM_DICT, "anim@heists@keypad@");

                if (Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "anim@heists@keypad@"))
                {
                    // Press keypad
                    Function.Call(
                        Hash.TASK_PLAY_ANIM,
                        clerk.Handle,
                        "anim@heists@keypad@",
                        "enter",
                        8.0f,
                        -8.0f,
                        1500,
                        (int)AnimationFlags.None,
                        0f,
                        false, false, false
                    );
                }

                // Set up next phase timing
                store.ClerkAnimStartUtc = DateTime.UtcNow;
                store.ClerkAnimDurationMs = 1500;

                // ⭐ After animation finishes, clerk will hold idle pose
                Script.Wait(1500);
                if (clerk != null && clerk.Exists())
                {
                    Function.Call(
                        Hash.TASK_PLAY_ANIM,
                        clerk.Handle,
                        "anim@heists@keypad@",
                        "idle_a",
                        8.0f,
                        -8.0f,
                        2000,
                        (int)AnimationFlags.None,
                        0f,
                        false, false, false
                    );
                }

                // Trigger police response
                Game.Player.WantedLevel = Math.Max(Game.Player.WantedLevel, 2);

                // Speech
                SafePlaySpeech(clerk, "GENERIC_SHOCKED_MED", "SPEECH_PARAMS_FORCE");

                DebugLogger.Info($"Silent alarm triggered at store {store.Id}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.TryTriggerSilentAlarm", ex);
            }
        }

        // ------------------------------------------------------------
        // POLICE CALL
        // ------------------------------------------------------------
        private void TryTriggerPoliceCall(TrackedStore store, Ped clerk, Ped player)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists() || player == null || !player.Exists())
                    return;

                if (store.ClerkStalling || store.ClerkOpeningRegister || store.ClerkGrabbingCash || store.ClerkThrowingBag)
                    return;

                if (store.ClerkCallingPolice)
                    return;

                if (!store.ClerkReacted)
                    return;

                if (store.ClerkFleeing)
                    return;

                if (_ctx.Player.IsThreatening(clerk))
                    return;

                if (!store.IsRobberyActive)
                    return;

                // If player leaves the store radius, clerk may call police
                if (!store.IsPlayerInsideStore)
                {
                    int chance = store.ClerkRecognizedPlayer ? 50 : 25;
                    if (_rng.Next(0, 100) < chance)
                    {
                        store.ClerkCallingPolice = true;
                        store.ClerkCallStartUtc = DateTime.UtcNow;

                        SafePlaySpeech(clerk, "GENERIC_SHOCKED_MED", "SPEECH_PARAMS_FORCE");

                        Game.Player.WantedLevel = Math.Max(Game.Player.WantedLevel, 2);

                        DebugLogger.Info($"Police called for robbery at store {store.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.TryTriggerPoliceCall", ex);
            }
        }

        // ------------------------------------------------------------
        // CLERK DEATH HANDLING — SAFE KO DETECTION
        // ------------------------------------------------------------
        private bool IsPedKnockedOut(Ped ped)
        {
            try
            {
                if (ped == null || !ped.Exists())
                    return false;

                // KO states that are NOT death
                return ped.IsRagdoll ||
                       ped.IsInjured ||
                       (ped.Health <= 1 && !ped.IsDead);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.IsPedKnockedOut", ex);
                return false;
            }
        }

        // ------------------------------------------------------------
        // CLERK DEATH HANDLING — SAFE RAGDOLL KO
        // ------------------------------------------------------------
        private void KnockOutPed(Ped ped)
        {
            try
            {
                if (ped == null || !ped.Exists())
                    return;

                ped.Health = 1; // keep alive
                ped.Armor = 0;

                // Clear tasks safely
                ped.Task.ClearAllImmediately();

                // Apply ragdoll KO
                ped.SetToRagdoll(5000, 5000, 0);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.KnockOutPed", ex);
            }
        }

        private void HandleClerkDeath(TrackedStore store)
        {
            try
            {
                if (store == null)
                    return;

                // Prevent double-processing
                if (store.ClerkDeathHandled)
                    return;

                Ped clerk = store.Clerk;
                Ped player = Game.Player.Character;

                // If clerk reference is gone, treat as dead
                bool clerkExists = (clerk != null && clerk.Exists());

                // ============================================================
                // KO / DEATH DETECTION (OLD SYSTEM LOGIC)
                // ============================================================

                // 1) NON-LETHAL KNOCKOUT
                if (clerkExists && IsPedKnockedOut(clerk))
                {
                    store.ClerkDeathHandled = true;
                    store.ClerkKilledWithGun = false;
                    store.SilentRobbery = true;

                    // Force KO ragdoll
                    KnockOutPed(clerk);

                    // UI + Stalker
                    _ctx.Ui.TextNotification(
                        "DIA_SILENT",
                        "Silent Takedown",
                        "LOS ANGELES PD",
                        "Clerk knocked out silently."
                    );

                    _ctx.Stalker.QueueKnockoutMessage();

                    // Continue to cleanup below
                }
                else
                {
                    // Determine weapon
                    WeaponHash weapon = WeaponHash.Unarmed;
                    if (player != null && player.Exists())
                        weapon = player.Weapons.Current.Hash;

                    bool melee = _ctx.Player.IsMeleeWeapon(weapon);

                    // 2) LETHAL KILL (GUN)
                    if (!clerkExists || (clerk != null && clerk.IsDead && !melee))
                    {
                        store.ClerkDeathHandled = true;
                        store.ClerkKilledWithGun = true;

                        _ctx.Ui.TextNotification(
                            "DIA_POLICE",
                            "All Units Responding",
                            "LOS ANGELES PD",
                            "Reported armed robbery in progress, shots fired at " + store.Name
                        );

                        _ctx.Stalker.QueueGunKillMessage();

                        // Gun kill ALWAYS activates robbery
                        store.IsRobberyActive = true;
                    }
                    // 3) LETHAL KILL (MELEE)
                    else if (clerk != null && clerk.IsDead && melee)
                    {
                        store.ClerkDeathHandled = true;
                        store.ClerkKilledWithGun = false;

                        _ctx.Ui.TextNotification(
                            "DIA_POLICE",
                            "Robbery Reported",
                            "LOS ANGELES PD",
                            "Clerk found injured at " + store.Name
                        );

                        _ctx.Stalker.QueueMeleeKillMessage();
                    }
                }

                // ============================================================
                // NEW SYSTEM CLEANUP (KEEP THIS)
                // ============================================================
                store.Clerk = null;
                store.IsOurClerk = false;
                store.ClerkIdle = false;
                store.ClerkReacted = false;
                store.ClerkStalling = false;
                store.ClerkOpeningRegister = false;
                store.ClerkGrabbingCash = false;
                store.ClerkThrowingBag = false;
                store.ClerkPanicking = false;
                store.ClerkFleeing = false;

                // Dummy clerk safety
                if (store.DummyClerk != null && store.DummyClerk.Exists())
                {
                    store.DummyClerk.Delete();
                    store.DummyClerk = null;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.HandleClerkDeath", ex);
            }
        }

        // ------------------------------------------------------------
        // SAFE SPEECH WRAPPER (SHVDN 3.9.0 SAFE)
        // ------------------------------------------------------------
        private void SafePlaySpeech(Ped ped, string speechName, string speechParam)
        {
            if (ped == null || !ped.Exists())
                return;

            try
            {
                // SHVDN 3.9.0: use native PLAY_PED_AMBIENT_SPEECH_NATIVE
                Function.Call(
                    Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE,
                    ped.Handle,
                    speechName,
                    speechParam,
                    0 // p3 (always 0 in game scripts)
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogException($"ClerkSystem.SafePlaySpeech {speechName}", ex);
            }
        }
    }
}
