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

                if (clerk.IsDead)
                {
                    if (store.IsOurClerk)
                        HandleClerkDeath(store);
                    return;
                }

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

                // Idle loop
                if (!store.ClerkReacted && !store.ClerkStalling)
                    RunIdleBehavior(store, clerk);

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
        // HELPER: GREET PLAYERS (FULL SPEECH SET)
        // ------------------------------------------------------------
        // ------------------------------------------------------------
        // HELPER: GREET PLAYERS (FULL SPEECH SET, SHVDN 3.9.0 SAFE)
        // ------------------------------------------------------------
        private void PlayClerkGreeting(TrackedStore store, Ped clerk)
        {
            if (clerk == null || !clerk.Exists())
                return;

            // ⭐ REQUIRED FOR SPEECH TO WORK IN INTERIORS
            Function.Call(Hash.SET_PED_CAN_PLAY_AMBIENT_ANIMS, clerk, true);
            Function.Call(Hash.SET_PED_CAN_PLAY_AMBIENT_BASE_ANIMS, clerk, true);
            Function.Call(Hash.SET_PED_CAN_PLAY_GESTURE_ANIMS, clerk, true);
            Function.Call(Hash.SET_PED_CAN_USE_AUTO_CONVERSATION_LOOKAT, clerk, true);

            // ✅ Replace the invalid hash with this valid one
            Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, clerk, false);

            Ped player = Game.Player.Character;  // ✅ define player here

            bool masked = _ctx.Player.IsMasked();
            bool repeatRobber = store.TimesRobbed >= 1;
            bool veryRepeatRobber = store.TimesRobbed >= 2;
            bool aiming = player.IsAiming;

            // ------------------------------------------------------------
            // SPEECH PRIORITY (highest → lowest)
            // ------------------------------------------------------------
            if (aiming)
            {
                SafePlaySpeech(clerk, "SHOP_SCARED", "SPEECH_PARAMS_FORCE");
                PlayClerkGreetingAnim(clerk);
                return;
            }

            if (masked)
            {
                SafePlaySpeech(clerk, "SHOP_GREET_MASKED", "SPEECH_PARAMS_FORCE");
                PlayClerkGreetingAnim(clerk);
                return;
            }

            if (repeatRobber && !veryRepeatRobber)
            {
                SafePlaySpeech(clerk, "SHOP_GREET_REPEAT", "SPEECH_PARAMS_FORCE");
                PlayClerkGreetingAnim(clerk);
                return;
            }

            if (veryRepeatRobber)
            {
                SafePlaySpeech(clerk, "SHOP_GREET_NERVOUS", "SPEECH_PARAMS_FORCE");
                PlayClerkGreetingAnim(clerk);
                return;
            }

            // 5. Player lingering / suspicious behavior
            if (player.Position.DistanceTo(store.StorePos) < store.Radius * 0.5f && player.IsOnFoot)
            {
                SafePlaySpeech(clerk, "SHOP_SUSPICIOUS", "SPEECH_PARAMS_FORCE");
                PlayClerkGreetingAnim(clerk);
                return;
            }

            // 6. Player browsing (optional flavor)
            if (player.IsOnFoot && player.Velocity.Length() < 0.1f)
            {
                SafePlaySpeech(clerk, "SHOP_BROWSE", "SPEECH_PARAMS_FORCE");
                PlayClerkGreetingAnim(clerk);
                return;
            }

            // 7. Default greeting
            SafePlaySpeech(clerk, "SHOP_GREET", "SPEECH_PARAMS_FORCE");
            PlayClerkGreetingAnim(clerk);
        }

        private void PlayClerkGreetingAnim(Ped clerk)
        {
            if (clerk == null || !clerk.Exists())
                return;

            string dict = "mp_am_hold_up";
            string anim = "shoplift_high";

            Function.Call(Hash.REQUEST_ANIM_DICT, dict);
            clerk.Task.PlayAnimation(dict, anim, 8f, -8f, 2000, AnimationFlags.UpperBodyOnly, 0f);
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

                    // ⭐ NEW: Dummy clerk also counts as "ours" so it is never removed
                    if (s.DummyClerk != null && s.DummyClerk.Exists() && s.DummyClerk.Handle == ped.Handle)
                        return true;
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

                Ped clerk = World.CreatePed(PedHash.ShopKeep01, store.ClerkPos, store.ClerkHeading);
                if (clerk == null || !clerk.Exists())
                    return;

                store.IsOurClerk = true;
                store.Clerk = clerk;

                clerk.IsPersistent = true;

                // Allow ambient / speech / gestures
                clerk.BlockPermanentEvents = false;
                Function.Call(Hash.SET_PED_CAN_PLAY_AMBIENT_ANIMS, clerk, true);
                Function.Call(Hash.SET_PED_CAN_PLAY_AMBIENT_BASE_ANIMS, clerk, true);
                Function.Call(Hash.SET_PED_CAN_PLAY_GESTURE_ANIMS, clerk, true);
                Function.Call(Hash.SET_PED_CAN_USE_AUTO_CONVERSATION_LOOKAT, clerk, true);

                clerk.Task.ClearAllImmediately();

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

                Ped clerk = World.CreatePed(PedHash.ShopKeep01, store.ClerkPos, store.ClerkHeading);
                if (clerk == null || !clerk.Exists())
                    return;

                store.Clerk = clerk;
                store.IsOurClerk = true;

                clerk.IsPersistent = true;

                // Same flags as SpawnClerk
                clerk.BlockPermanentEvents = false;
                Function.Call(Hash.SET_PED_CAN_PLAY_AMBIENT_ANIMS, clerk, true);
                Function.Call(Hash.SET_PED_CAN_PLAY_AMBIENT_BASE_ANIMS, clerk, true);
                Function.Call(Hash.SET_PED_CAN_PLAY_GESTURE_ANIMS, clerk, true);
                Function.Call(Hash.SET_PED_CAN_USE_AUTO_CONVERSATION_LOOKAT, clerk, true);

                clerk.Task.ClearAllImmediately();

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

        private void PlayAnimNative(Ped ped, string dict, string anim, AnimationFlags flags)
        {
            if (ped == null || !ped.Exists())
                return;

            try
            {
                Function.Call(Hash.REQUEST_ANIM_DICT, dict);
                // No Script.Wait here; just fire the task
                Function.Call(
                    Hash.TASK_PLAY_ANIM,
                    ped,
                    dict,
                    anim,
                    8.0f,   // speed
                    -8.0f,  // speedMult
                    -1,     // duration
                    (int)flags,
                    0.0f,   // playbackRate
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

                if (!store.ClerkIdle)
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
                    PlayAnimNative(clerk, dict, anim, AnimationFlags.Loop);
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

                Model bagModel = new Model("prop_poly_bag_01");
                if (bagModel.IsInCdImage && bagModel.IsValid)
                {
                    Vector3 spawnPos = clerk.Position + clerk.ForwardVector * 0.3f + new Vector3(0, 0, 0.8f);
                    Prop bag = World.CreateProp(bagModel, spawnPos, true, false);

                    if (bag != null && bag.Exists())
                    {
                        bag.ApplyForce(clerk.ForwardVector * 2.0f + new Vector3(0, 0, 1.0f));
                    }
                }

                Ped player = Game.Player.Character;

                switch (store.ReactionType)
                {
                    case ClerkReactionType.NormalPanic:
                        store.ClerkPanicking = true;
                        clerk.Task.Cower(-1);
                        break;

                    case ClerkReactionType.Flee:
                        store.ClerkFleeing = true;
                        if (player != null && player.Exists())
                            clerk.Task.ReactAndFlee(player);
                        break;

                    case ClerkReactionType.FightPistol:
                        clerk.Weapons.Give(WeaponHash.Pistol, 60, true, true);
                        if (player != null && player.Exists())
                            clerk.Task.FightAgainst(player);
                        break;

                    case ClerkReactionType.FightShotgun:
                        clerk.Weapons.Give(WeaponHash.PumpShotgun, 20, true, true);
                        if (player != null && player.Exists())
                            clerk.Task.FightAgainst(player);
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

                clerk.Task.Cower(-1);
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
                if (player != null && player.Exists())
                    clerk.Task.ReactAndFlee(player);
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

                int chance = store.ClerkRecognizedPlayer ? 40 : 20;

                if (_rng.Next(0, 100) < chance)
                {
                    store.SilentAlarmPressed = true;
                    store.SilentAlarmUtc = DateTime.UtcNow;

                    SafePlaySpeech(clerk, "GENERIC_SHOCKED_HIGH", "SPEECH_PARAMS_FORCE");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.TryTriggerSilentAlarm", ex);
            }
        }

        // ------------------------------------------------------------
        // CLERK CALLING POLICE
        // ------------------------------------------------------------
        private void TryTriggerPoliceCall(TrackedStore store, Ped clerk, Ped player)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                if (store.ClerkCallingPolice)
                    return;

                if (!store.ClerkReacted)
                    return;

                if (store.ClerkFleeing)
                    return;

                if (_ctx.Player.IsThreatening(clerk))
                    return;

                int chance = store.ClerkRecognizedPlayer ? 50 : 25;

                if (_rng.Next(0, 100) < chance)
                {
                    store.ClerkCallingPolice = true;
                    store.ClerkCallStartUtc = DateTime.UtcNow;

                    SafePlaySpeech(clerk, "GENERIC_SHOCKED_MED", "SPEECH_PARAMS_FORCE");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.TryTriggerPoliceCall", ex);
            }
        }

        // ------------------------------------------------------------
        // DEATH HANDLING
        // ------------------------------------------------------------
        private void HandleClerkDeath(TrackedStore store)
        {
            try
            {
                if (store == null)
                    return;

                if (store.ClerkDeathHandled)
                    return;

                store.ClerkDeathHandled = true;

                Ped player = Game.Player.Character;
                WeaponHash weapon = WeaponHash.Unarmed;

                if (player != null && player.Exists())
                    weapon = player.Weapons.Current.Hash;

                if (weapon == WeaponHash.Unarmed)
                {
                    store.ClerkKilledWithGun = false;
                    _ctx.Ui.TextNotification("DIA_SILENT", "Silent Takedown", "LOS ANGELES PD", "Clerk knocked out silently.");
                    _ctx.Stalker.QueueKnockoutMessage();
                }
                else if (_ctx.Player.IsMeleeWeapon(weapon))
                {
                    store.ClerkKilledWithGun = false;
                    _ctx.Ui.TextNotification("DIA_POLICE", "Robbery Reported", "LOS ANGELES PD", "Clerk found injured at " + store.Name);
                    _ctx.Stalker.QueueMeleeKillMessage();
                }
                else
                {
                    store.ClerkKilledWithGun = true;
                    _ctx.Ui.TextNotification("DIA_POLICE", "All Units Responding", "LOS ANGELES PD", "Reported armed robbery in progress, shots fired at " + store.Name);
                    _ctx.Stalker.QueueGunKillMessage();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.HandleClerkDeath", ex);
            }
        }

        // ------------------------------------------------------------
        // SAFE SPEECH HELPER
        // ------------------------------------------------------------
        private void SafePlaySpeech(Ped ped, string speechName, string speechParam)
        {
            try
            {
                if (ped == null || !ped.Exists())
                    return;

                if (string.IsNullOrEmpty(speechName) || string.IsNullOrEmpty(speechParam))
                    return;

                Function.Call(Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE, ped, speechName, speechParam, 0);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.SafePlaySpeech", ex);
            }
        }
    }
}
