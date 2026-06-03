using System;
using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyTrackerMod.Data;
using StoreRobberyTrackerMod.Debug;

namespace StoreRobberyTrackerMod.Systems
{
    internal class ClerkReplacementSystem
    {
        private readonly StoreContext _ctx;
        private readonly TimeSpan _sweepInterval = TimeSpan.FromSeconds(3);

        // All Rockstar clerk models
        private readonly int[] _defaultClerkModels =
        {
            (int)PedHash.ShopKeep01,
            (int)PedHash.ShopMaskSMY,
            Function.Call<int>(Hash.GET_HASH_KEY, "mp_m_shopkeep_01"),
            Function.Call<int>(Hash.GET_HASH_KEY, "s_m_m_shopkeep_01")
        };

        public ClerkReplacementSystem(StoreContext ctx)
        {
            try
            {
                _ctx = ctx;
                DebugLogger.Info("ClerkReplacementSystem initialized");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkReplacementSystem.ctor", ex);
            }
        }

        // ------------------------------------------------------------
        // MAIN ENTRY
        // ------------------------------------------------------------
        public void UpdateForStore(TrackedStore store, Ped player)
        {
            if (store == null)
                return;

            // ⭐ Increase distance so replacement happens BEFORE interior loads
            float dist = player.Position.DistanceTo(store.StorePos);
            if (dist > store.Radius + 60f)
                return;

            // Ensure replacement when near
            EnsureDefaultClerkRemoved(store);

            // Periodic sweep to prevent respawns
            if (DateTime.UtcNow - store.LastClerkSweepUtc >= _sweepInterval)
            {
                store.LastClerkSweepUtc = DateTime.UtcNow;
                EnsureDefaultClerkRemoved(store);
            }
        }

        // ------------------------------------------------------------
        // ENSURE DEFAULT CLERK REMOVED
        // ------------------------------------------------------------
        private void EnsureDefaultClerkRemoved(TrackedStore store)
        {
            // Temporarily suppress wanted level while replacement runs
            Function.Call(Hash.SET_MAX_WANTED_LEVEL, 0);
            Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, true);
            Game.Player.WantedLevel = 0;

            try
            {
                // If our clerk already exists, just keep the area clean
                if (store.Clerk != null && store.Clerk.Exists())
                {
                    RemoveNearbyDefaultClerks(store, store.Clerk);
                    store.DefaultClerkRemoved = true;
                }
                else
                {
                    // First time: remove default clerk, then spawn ours
                    RemoveNearbyDefaultClerks(store, null);
                    _ctx.Clerks?.ForceSpawnClerk(store);

                    if (store.Clerk != null && store.Clerk.Exists())
                        store.DefaultClerkRemoved = true;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkReplacementSystem.EnsureDefaultClerkRemoved", ex);
            }
            finally
            {
                // Restore normal police behavior
                Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, false);
                Function.Call(Hash.SET_MAX_WANTED_LEVEL, 5);
            }
        }

        // ------------------------------------------------------------
        // REMOVE DEFAULT CLERKS (SAFE VERSION)
        // ------------------------------------------------------------
        private void RemoveNearbyDefaultClerks(TrackedStore store, Ped skip)
        {
            Vector3 pos = store.ClerkPos;
            float radius = 3.0f;

            Ped[] nearby = World.GetNearbyPeds(pos, radius);
            if (nearby == null || nearby.Length == 0)
                return;

            foreach (Ped ped in nearby)
            {
                if (ped == null || !ped.Exists())
                    continue;

                if (skip != null && ped.Handle == skip.Handle)
                    continue;

                int hash = ped.Model.Hash;

                if (Array.IndexOf(_defaultClerkModels, hash) != -1)
                {
                    // ⭐ DO NOT DELETE — deleting triggers wanted level
                    // ⭐ DO NOT MOVE UNDERGROUND — GTA interprets as death

                    ped.Task.ClearAllImmediately();
                    ped.BlockPermanentEvents = true;
                    ped.CanBeTargetted = false;
                    ped.IsInvincible = true;
                    ped.IsVisible = false;
                    ped.IsCollisionEnabled = false;

                    // ⭐ Move clerk ABOVE the store so GTA considers them alive
                    ped.Position = store.ClerkPos + new Vector3(0f, 0f, 50f);

                    ped.IsPersistent = true;
                    ped.IsPositionFrozen = true;

                    // ⭐ Spawn dummy clerk to satisfy interior script
                    _ctx.Clerks.SpawnDummyClerk(store);

                    DebugLogger.Info($"Default clerk safely neutralized for store {store.Id}");
                }
            }
        }
    }
}
