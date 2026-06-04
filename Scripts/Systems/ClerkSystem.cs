using System;
using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyTrackerMod.Data;
using StoreRobberyTrackerMod.Debug;

namespace StoreRobberyTrackerMod.Systems
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
                    return;
                }

                Ped clerk = store.Clerk;
                if (clerk == null || !clerk.Exists())
                    return;

                // ⭐ SAFETY RESET: only if clerk is actually stuck
                bool usingScenario = Function.Call<bool>(Hash.IS_PED_USING_ANY_SCENARIO, clerk);

                if (clerk.IsRagdoll || usingScenario || clerk.IsPositionFrozen)
                {
                    DebugLogger.Info(string.Format(
                        "[RESET] Forcing task clear on clerk {0} (frozen={1} ragdoll={2} scenario={3})",
                        clerk.Handle,
                        clerk.IsPositionFrozen,
                        clerk.IsRagdoll,
                        usingScenario
                    ));

                    Function.Call(Hash.CLEAR_PED_TASKS_IMMEDIATELY, clerk);
                    clerk.IsPositionFrozen = false;
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

                // SAFETY: make sure our clerk can actually animate
                clerk.IsPositionFrozen = false;

                // ⭐ Extra safety: shortly after spawn, ensure no leftover tasks
                if (store.ClerkSpawnTime > 0 && Game.GameTime - store.ClerkSpawnTime < 2000)
                {
                    clerk.Task.ClearAllImmediately();
                    clerk.BlockPermanentEvents = false;
                    clerk.IsPositionFrozen = false;
                }

                // Preload anim dicts (non-blocking)
                Function.Call(Hash.REQUEST_ANIM_DICT, "amb@world_human_shopkeeper@male@idle_a");
                Function.Call(Hash.REQUEST_ANIM_DICT, "mp_am_hold_up");

                // ------------------------------------------------------------
                // PLAYER ENTERED STORE → GREETING LOGIC
                // ------------------------------------------------------------
                if (store.IsPlayerInsideStore && !store.GreetedPlayer)
                {
                    int now = Game.GameTime;

                    // Prevent spam if player leaves and re-enters quickly
                    if (now - store.LastGreetTime > 8000)
                    {
                        store.GreetedPlayer = true;
                        store.LastGreetTime = now;

                        DebugLogger.Info($"Clerk greeting player at store {store.Id}");
                        PlayClerkGreeting(store, clerk);
                    }
                }
                else if (!store.IsPlayerInsideStore)
                {
                    // Reset greeting when player leaves
                    store.GreetedPlayer = false;
                }

                // ------------------------------------------------------------
                // IDLE LOOP — DO NOT INTERRUPT GREETING
                // ------------------------------------------------------------

                // ⭐ If greeting window is active, do nothing
                if (Game.GameTime < store.GreetingEndTime)
                    return;

                // If clerk is currently playing the greet anim, don't touch them
                if (IsPlayingAnim(clerk, "mp_am_hold_up", "shoplift_high"))
                    return;

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
                if (!store.ClerkReacted && _ctx.Player.IsThreatening(clerk))
                {
                    BeginFearReaction(store, clerk);
                    return;
                }

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
        // CLERK VOICE + AMBIENT SETTINGS
        // ------------------------------------------------------------
        private static readonly string[] ClerkVoices =
        {
            "S_M_M_SHOP_KEEP_01_WHITE_MINI_01",
            "S_M_M_SHOP_KEEP_01_BLACK_MINI_01",
            "S_M_M_SHOP_KEEP_01_LATINO_MINI_01",
            "S_M_M_SHOP_KEEP_01_ASIAN_MINI_01"
        };

        private void ApplyClerkVoiceAndAmbientSettings(Ped clerk)
        {
            if (clerk == null || !clerk.Exists())
                return;

            // Random voice
            string voice = ClerkVoices[_ctx.Rng.Next(ClerkVoices.Length)];
            Function.Call(Hash.SET_AMBIENT_VOICE_NAME, clerk, voice);

            // Enable all ambient systems
            clerk.BlockPermanentEvents = false;
            Function.Call(Hash.SET_PED_CAN_PLAY_AMBIENT_ANIMS, clerk, true);
            Function.Call(Hash.SET_PED_CAN_PLAY_AMBIENT_BASE_ANIMS, clerk, true);
            Function.Call(Hash.SET_PED_CAN_PLAY_GESTURE_ANIMS, clerk, true);
            Function.Call(Hash.SET_PED_CAN_USE_AUTO_CONVERSATION_LOOKAT, clerk, true);

            // Ensure speech is allowed
            Function.Call(Hash.STOP_PED_SPEAKING, clerk, false);
        }

        // ------------------------------------------------------------
        // HELPER: GREET PLAYERS (FULL SPEECH SET, SHVDN 3.9.0 SAFE)
        // ------------------------------------------------------------
        private void PlayClerkGreeting(TrackedStore store, Ped clerk)
        {
            if (clerk == null || !clerk.Exists())
                return;

            // ⭐ PREVENT DUMMY CLERKS FROM EVER GREETING
            if (store.DummyClerk != null && store.DummyClerk.Exists() &&
                store.Clerk != null && store.Clerk.Exists() &&
                store.Clerk.Handle == store.DummyClerk.Handle)
                return;

            // ⭐ REQUIRED FOR SPEECH TO WORK IN INTERIORS
            Function.Call(Hash.SET_PED_CAN_PLAY_AMBIENT_ANIMS, clerk, true);
            Function.Call(Hash.SET_PED_CAN_PLAY_AMBIENT_BASE_ANIMS, clerk, true);
            Function.Call(Hash.SET_PED_CAN_PLAY_GESTURE_ANIMS, clerk, true);
            Function.Call(Hash.SET_PED_CAN_USE_AUTO_CONVERSATION_LOOKAT, clerk, true);

            // ⭐ Allow gestures + speech
            Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, clerk, false);

            // ⭐ Set greeting window (2 seconds)
            store.GreetingEndTime = Game.GameTime + 2000;

            Ped player = Game.Player.Character;

            bool masked = _ctx.Player.IsMasked();
            bool repeatRobber = store.TimesRobbed >= 1;
            bool veryRepeatRobber = store.TimesRobbed >= 2;
            bool aiming = player.IsAiming;

            string speechName = "SHOP_GREET"; // default fallback

            // ------------------------------------------------------------
            // SPEECH PRIORITY (highest → lowest)
            // ------------------------------------------------------------
            if (aiming)
            {
                speechName = "SHOP_SCARED";
                SafePlaySpeech(clerk, speechName, "SPEECH_PARAMS_FORCE");
                PlayClerkGreetingAnim(clerk);
                DebugLogger.Info($"[GREET] {speechName} on clerk {clerk.Handle}");
                return;
            }

            if (masked)
            {
                speechName = "SHOP_GREET_MASKED";
                SafePlaySpeech(clerk, speechName, "SPEECH_PARAMS_FORCE");
                PlayClerkGreetingAnim(clerk);
                DebugLogger.Info($"[GREET] {speechName} on clerk {clerk.Handle}");
                return;
            }

            if (repeatRobber && !veryRepeatRobber)
            {
                speechName = "SHOP_GREET_REPEAT";
                SafePlaySpeech(clerk, speechName, "SPEECH_PARAMS_FORCE");
                PlayClerkGreetingAnim(clerk);
                DebugLogger.Info($"[GREET] {speechName} on clerk {clerk.Handle}");
                return;
            }

            if (veryRepeatRobber)
            {
                speechName = "SHOP_GREET_NERVOUS";
                SafePlaySpeech(clerk, speechName, "SPEECH_PARAMS_FORCE");
                PlayClerkGreetingAnim(clerk);
                DebugLogger.Info($"[GREET] {speechName} on clerk {clerk.Handle}");
                return;
            }

            // Suspicious
            if (player.Position.DistanceTo(store.StorePos) < store.Radius * 0.5f && player.IsOnFoot)
            {
                speechName = "SHOP_SUSPICIOUS";
                SafePlaySpeech(clerk, speechName, "SPEECH_PARAMS_FORCE");
                PlayClerkGreetingAnim(clerk);
                DebugLogger.Info($"[GREET] {speechName} on clerk {clerk.Handle}");
                return;
            }

            // Browsing
            if (player.IsOnFoot && player.Velocity.Length() < 0.1f)
            {
                speechName = "SHOP_BROWSE";
                SafePlaySpeech(clerk, speechName, "SPEECH_PARAMS_FORCE");
                PlayClerkGreetingAnim(clerk);
                DebugLogger.Info($"[GREET] {speechName} on clerk {clerk.Handle}");
                return;
            }

            // Default
            speechName = "SHOP_GREET";
            SafePlaySpeech(clerk, speechName, "SPEECH_PARAMS_FORCE");
            PlayClerkGreetingAnim(clerk);
            DebugLogger.Info($"[GREET] {speechName} on clerk {clerk.Handle}");
        }

        private void PlayClerkGreetingAnim(Ped clerk)
        {
            if (clerk == null || !clerk.Exists())
                return;

            string dict = "mp_am_hold_up";
            string anim = "shoplift_high";

            // ⭐ Clear ALL tasks so greeting can override idle / any other anim
            clerk.Task.ClearAllImmediately();

            // ⭐ Full-body anim so it always shows on shopkeepers
            PlayAnimNative(
                clerk,
                dict,
                anim,
                AnimationFlags.None
            );

            DebugLogger.Info(string.Format("[GREET-ANIM] Requested {0}/{1} on clerk {2}", dict, anim, clerk.Handle));
        }

        // ------------------------------------------------------------
        // HELPER: Determine if a ped is one of our custom clerks
        // ------------------------------------------------------------
        public static bool IsOurClerk(Ped ped)
        {
            try
            {
                if (ped == null || !ped.Exists())
                    return false;

                // A clerk is "ours" ONLY if it is assigned to a store
                foreach (TrackedStore s in StoreContext.Active.Stores)
                {
                    // Real store clerk
                    if (s.Clerk != null && s.Clerk.Exists() && s.Clerk.Handle == ped.Handle)
                        return true;

                    // ⭐ Dummy clerks are NOT treated as real clerks for behavior
                    // They are only used internally by the replacement system
                    // so we explicitly do NOT return true for them here.
                }

                return false;
            }
            catch
            {
                return false;
            }
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

                // ⭐ Remove dummy clerk before spawning real clerk
                if (store.DummyClerk != null && store.DummyClerk.Exists())
                {
                    store.DummyClerk.Delete();
                    store.DummyClerk = null;
                }

                Ped clerk = World.CreatePed(PedHash.ShopKeep01, store.ClerkPos, store.ClerkHeading);
                if (clerk == null || !clerk.Exists())
                    return;

                store.IsOurClerk = true;
                store.Clerk = clerk;

                // ⭐ Record spawn time for interior detach logic
                store.ClerkSpawnTime = Game.GameTime;

                clerk.IsPersistent = true;

                // Voice + ambient
                ApplyClerkVoiceAndAmbientSettings(clerk);

                // Allow ambient / speech / gestures
                clerk.BlockPermanentEvents = false;
                Function.Call(Hash.SET_PED_CAN_PLAY_AMBIENT_ANIMS, clerk, true);
                Function.Call(Hash.SET_PED_CAN_PLAY_AMBIENT_BASE_ANIMS, clerk, true);
                Function.Call(Hash.SET_PED_CAN_PLAY_GESTURE_ANIMS, clerk, true);
                Function.Call(Hash.SET_PED_CAN_USE_AUTO_CONVERSATION_LOOKAT, clerk, true);

                // ⭐ Ensure ped is free to animate immediately
                clerk.IsPositionFrozen = false;
                Function.Call(Hash.CLEAR_PED_TASKS_IMMEDIATELY, clerk);

                // ⭐ Preload anim dicts for idle and greeting
                Function.Call(Hash.REQUEST_ANIM_DICT, "amb@world_human_shopkeeper@male@idle_a");
                Function.Call(Hash.REQUEST_ANIM_DICT, "mp_am_hold_up");

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

                if (store.Clerk != null && store.Clerk.Exists())
                    return;

                // ⭐ Remove dummy clerk before spawning real clerk
                if (store.DummyClerk != null && store.DummyClerk.Exists())
                {
                    store.DummyClerk.Delete();
                    store.DummyClerk = null;
                }

                Ped clerk = World.CreatePed(PedHash.ShopKeep01, store.ClerkPos, store.ClerkHeading);
                if (clerk == null || !clerk.Exists())
                    return;

                store.Clerk = clerk;
                store.IsOurClerk = true;

                // ⭐ Record spawn time for interior detach logic
                store.ClerkSpawnTime = Game.GameTime;

                clerk.IsPersistent = true;

                // Voice + ambient
                ApplyClerkVoiceAndAmbientSettings(clerk);

                // Same flags as SpawnClerk
                clerk.BlockPermanentEvents = false;
                Function.Call(Hash.SET_PED_CAN_PLAY_AMBIENT_ANIMS, clerk, true);
                Function.Call(Hash.SET_PED_CAN_PLAY_AMBIENT_BASE_ANIMS, clerk, true);
                Function.Call(Hash.SET_PED_CAN_PLAY_GESTURE_ANIMS, clerk, true);
                Function.Call(Hash.SET_PED_CAN_USE_AUTO_CONVERSATION_LOOKAT, clerk, true);

                clerk.IsPositionFrozen = false;
                clerk.Task.ClearAllImmediately();

                // ⭐ Preload anim dicts for idle and greeting
                Function.Call(Hash.REQUEST_ANIM_DICT, "amb@world_human_shopkeeper@male@idle_a");
                Function.Call(Hash.REQUEST_ANIM_DICT, "mp_am_hold_up");

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
                return Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, ped, dict, name, 3);
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

            try
            {
                // Always request the dict – game will cache it
                Function.Call(Hash.REQUEST_ANIM_DICT, dict);

                DebugLogger.Info(string.Format(
                    "[ANIM] Requesting anim {0}/{1} on ped {2}",
                    dict,
                    anim,
                    ped.Handle
                ));

                Function.Call(
                    Hash.TASK_PLAY_ANIM,
                    ped,
                    dict,
                    anim,
                    8.0f,    // speed
                    -8.0f,   // speedMult
                    -1,      // duration
                    (int)flags,
                    0.0f,    // playbackRate
                    false, false, false
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(
                    string.Format("ClerkSystem.PlayAnimNative {0}/{1}", dict, anim),
                    ex
                );
            }
        }

        // ------------------------------------------------------------
        // IDLE BEHAVIOR (patched to prevent flicker)
        // ------------------------------------------------------------
        private void RunIdleBehavior(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
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

                SafePlaySpeech(clerk, "SHOP_CLERK_REACT", "SPEECH_PARAMS_FORCE");

                if (store.TimesRobbed >= 2)
                    store.ClerkRecognizedPlayer = true;

                int roll = _rng.Next(0, 100);

                if (roll < 70)
                    store.ReactionType = ClerkReactionType.NormalPanic;
                else if (roll < 90)
                    store.ReactionType = ClerkReactionType.Flee;
                else if (roll < 98)
                    store.ReactionType = ClerkReactionType.FightPistol;
                else
                    store.ReactionType = ClerkReactionType.FightShotgun;

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

                if ((DateTime.UtcNow - store.StallStartUtc).TotalMilliseconds < store.StallDurationMs)
                    return;

                store.ClerkStalling = false;

                clerk.Task.ClearAllImmediately();
                clerk.Position = store.RegisterPos;
                clerk.Heading = store.RegisterHeading;

                store.ClerkOpeningRegister = true;
                store.ClerkAnimStartUtc = DateTime.UtcNow;
                store.ClerkAnimDurationMs = 1800;

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

                if ((DateTime.UtcNow - store.ClerkAnimStartUtc).TotalMilliseconds < store.ClerkAnimDurationMs)
                    return;

                store.ClerkOpeningRegister = false;
                store.ClerkGrabbingCash = true;

                store.ClerkAnimStartUtc = DateTime.UtcNow;
                store.ClerkAnimDurationMs = 1200;

                PlayAnimNative(clerk, "mp_common", "givetake1_a", AnimationFlags.UpperBodyOnly);
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

                if ((DateTime.UtcNow - store.ClerkAnimStartUtc).TotalMilliseconds < store.ClerkAnimDurationMs)
                    return;

                store.ClerkGrabbingCash = false;
                store.ClerkThrowingBag = true;

                store.ClerkAnimStartUtc = DateTime.UtcNow;
                store.ClerkAnimDurationMs = 1000;

                PlayAnimNative(clerk, "mp_am_hold_up", "holdup_victim_20s", AnimationFlags.None);

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

                if ((DateTime.UtcNow - store.ClerkAnimStartUtc).TotalMilliseconds < store.ClerkAnimDurationMs)
                    return;

                store.ClerkThrowingBag = false;

                // After bag toss, clerk may panic or flee depending on reaction type
                switch (store.ReactionType)
                {
                    case ClerkReactionType.NormalPanic:
                        store.ClerkPanicking = true;
                        break;
                    case ClerkReactionType.Flee:
                    case ClerkReactionType.FightPistol:
                    case ClerkReactionType.FightShotgun:
                        store.ClerkFleeing = true;
                        break;
                }
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
        // FLEE
        // ------------------------------------------------------------
        private void ProcessFlee(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                Ped player = Game.Player.Character;

                if (!clerk.IsFleeing)
                {
                    clerk.Task.ClearAllImmediately();
                    clerk.Task.ReactAndFlee(player);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessFlee", ex);
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

                // Simple chance-based alarm
                int chance = store.ClerkRecognizedPlayer ? 40 : 20;
                if (_rng.Next(0, 100) < chance)
                {
                    store.SilentAlarmPressed = true;
                    store.SilentAlarmUtc = DateTime.UtcNow;

                    Game.Player.WantedLevel = Math.Max(Game.Player.WantedLevel, 2);

                    SafePlaySpeech(clerk, "GENERIC_SHOCKED_HIGH", "SPEECH_PARAMS_FORCE");

                    DebugLogger.Info($"Silent alarm triggered at store {store.Id}");
                }
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
        // CLERK DEATH HANDLING
        // ------------------------------------------------------------
        private void HandleClerkDeath(TrackedStore store)
        {
            try
            {
                if (store == null)
                    return;

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
                    ped,
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
