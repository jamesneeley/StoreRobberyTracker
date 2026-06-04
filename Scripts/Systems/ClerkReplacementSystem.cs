using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyTrackerMod.Data;
using StoreRobberyTrackerMod.Debug;
using System;
using System.Collections.Generic;

namespace StoreRobberyTrackerMod.Systems
{
    internal class ClerkReplacementSystem
    {
        private readonly StoreContext _ctx;
        private readonly TimeSpan _sweepInterval = TimeSpan.FromSeconds(1);

        // Rockstar interior clerk models
        private readonly int[] _interiorClerkModels =
        {
            Function.Call<int>(Hash.GET_HASH_KEY, "s_m_m_shopkeep_01"),
            Function.Call<int>(Hash.GET_HASH_KEY, "mp_m_shopkeep_01")
        };

        // Ambient clerk models (rarely used but included)
        private readonly int[] _ambientClerkModels =
        {
            (int)PedHash.ShopKeep01,
            (int)PedHash.ShopMaskSMY
        };

        public ClerkReplacementSystem(StoreContext ctx)
        {
            _ctx = ctx;
            DebugLogger.Info("ClerkReplacementSystem initialized");
        }

        // ------------------------------------------------------------
        // MAIN ENTRY
        // ------------------------------------------------------------
        public void UpdateForStore(TrackedStore store, Ped player)
        {
            if (store == null)
                return;

            float dist = player.Position.DistanceTo(store.StorePos);

            // Replace BEFORE interior loads
            if (dist > store.Radius + 60f)
                return;

            // ⭐ Temporarily suppress wanted level while replacement runs
            Function.Call(Hash.SET_MAX_WANTED_LEVEL, 0);
            Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, true);
            Game.Player.WantedLevel = 0;

            try
            {
                // ⭐ Sweep interior clerks every tick (safe)
                RemoveInteriorClerks(store);

                // ⭐ Ensure our clerk exists (spawns only once)
                EnsureCustomClerk(store);

                // ⭐ Periodic sweep to prevent respawns
                if (DateTime.UtcNow - store.LastClerkSweepUtc >= _sweepInterval)
                {
                    store.LastClerkSweepUtc = DateTime.UtcNow;
                    RemoveInteriorClerks(store);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkReplacementSystem.UpdateForStore", ex);
            }
            finally
            {
                // ⭐ Restore normal police behavior
                Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, false);
                Function.Call(Hash.SET_MAX_WANTED_LEVEL, 5);
            }
        }

        // ------------------------------------------------------------
        // REMOVE INTERIOR SCRIPT CLERKS (THE REAL FIX)
        // ------------------------------------------------------------
        private readonly HashSet<int> _neutralizedHandles = new HashSet<int>();

        private void RemoveInteriorClerks(TrackedStore store)
        {
            try
            {
                Ped[] all = World.GetAllPeds();

                foreach (Ped ped in all)
                {
                    if (ped == null || !ped.Exists())
                        continue;

                    int handle = ped.Handle;
                    int hash = ped.Model.Hash;

                    // Skip already neutralized peds
                    if (_neutralizedHandles.Contains(handle))
                        continue;

                    // Skip our clerk and dummy
                    if ((store.Clerk != null && handle == store.Clerk.Handle) ||
                        (store.DummyClerk != null && handle == store.DummyClerk.Handle))
                        continue;

                    if (Array.IndexOf(_interiorClerkModels, hash) != -1 ||
                        Array.IndexOf(_ambientClerkModels, hash) != -1)
                    {
                        NeutralizeInteriorClerk(ped, store);
                        _neutralizedHandles.Add(handle);
                    }
                }

                // Clean up stale handles
                _neutralizedHandles.RemoveWhere(h => !Function.Call<bool>(Hash.DOES_ENTITY_EXIST, h));
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkReplacementSystem.RemoveInteriorClerks", ex);
            }
        }

        // ------------------------------------------------------------
        // NEUTRALIZE INTERIOR CLERK
        // ------------------------------------------------------------
        private void NeutralizeInteriorClerk(Ped ped, TrackedStore store)
        {
            try
            {
                ped.Task.ClearAllImmediately();
                ped.BlockPermanentEvents = true;
                ped.CanBeTargetted = false;
                ped.IsInvincible = true;
                ped.IsVisible = false;
                ped.IsCollisionEnabled = false;

                // Move far above store so GTA considers them alive
                ped.Position = store.ClerkPos + new Vector3(0f, 0f, 50f);

                ped.IsPersistent = true;
                ped.IsPositionFrozen = true;

                // Spawn dummy if needed
                if (store.Clerk == null || !store.Clerk.Exists())
                {
                    _ctx.Clerks.SpawnDummyClerk(store);
                }

                DebugLogger.Info($"Interior clerk neutralized for store {store.Id}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkReplacementSystem.NeutralizeInteriorClerk", ex);
            }
        }

        // ------------------------------------------------------------
        // ENSURE CUSTOM CLERK EXISTS
        // ------------------------------------------------------------
        private void EnsureCustomClerk(TrackedStore store)
        {
            try
            {
                if (store.Clerk != null && store.Clerk.Exists())
                {
                    store.DefaultClerkRemoved = true;
                    return;
                }

                // Remove interior clerks again before spawning
                RemoveInteriorClerks(store);

                // Remove dummy if present
                if (store.DummyClerk != null && store.DummyClerk.Exists())
                {
                    store.DummyClerk.Delete();
                    store.DummyClerk = null;
                }

                // Spawn our clerk
                _ctx.Clerks.ForceSpawnClerk(store);

                if (store.Clerk != null && store.Clerk.Exists())
                {
                    store.DefaultClerkRemoved = true;
                    DebugLogger.Info($"Custom clerk spawned for store {store.Id}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkReplacementSystem.EnsureCustomClerk", ex);
            }
        }
    }
}
