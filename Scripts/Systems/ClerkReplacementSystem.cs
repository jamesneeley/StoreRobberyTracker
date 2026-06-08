using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyTrackerMod.Data;
using StoreRobberyTrackerMod.Debug;
using System;

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

        public void UpdateForStore(TrackedStore store, Ped player)
        {
            if (store == null)
                return;

            // Only care if player is near this store
            float dist = player.Position.DistanceTo(store.StorePos);
            if (dist > store.Radius + 10f)
                return;

            // Ensure replacement when near (Option 1)
            EnsureDefaultClerkRemoved(store);

            // Periodic sweep to prevent respawns (Option 3)
            if (DateTime.UtcNow - store.LastClerkSweepUtc >= _sweepInterval)
            {
                store.LastClerkSweepUtc = DateTime.UtcNow;
                EnsureDefaultClerkRemoved(store);
            }
        }

        private void EnsureDefaultClerkRemoved(TrackedStore store)
        {
            try 
            {
                // ⭐ Suppress wanted level only during replacement
                Function.Call(Hash.SET_MAX_WANTED_LEVEL, 0);
                Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, true);
                Game.Player.WantedLevel = 0;
                _ctx.Police.SuppressPoliceForDebug = true;

                // If our clerk already exists, just keep the area clean
                if (store.Clerk != null && store.Clerk.Exists())
            {
                RemoveNearbyDefaultClerks(store, store.Clerk);
                store.DefaultClerkRemoved = true;
                return;
            }

            // First time: remove default clerk, then spawn ours
            RemoveNearbyDefaultClerks(store, null);

            _ctx.Clerks?.ForceSpawnClerk(store);

            if (store.Clerk != null && store.Clerk.Exists())
                store.DefaultClerkRemoved = true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkReplacementSystem.EnsureDefaultClerkRemoved", ex);
            }
            finally
            {
                Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, false);
                Function.Call(Hash.SET_MAX_WANTED_LEVEL, 5);
                _ctx.Police.SuppressPoliceForDebug = false;
            }
        }

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

                // mark that this was NOT our clerk
                store.IsOurClerk = false;

                // SHVDN 3.9.0: valid store clerk models
                if (Array.IndexOf(_defaultClerkModels, ped.Model.Hash) != -1)
                {
                    // Neutralize default clerk safely (NO wanted level)
                    ped.Task.ClearAllImmediately();
                    ped.BlockPermanentEvents = true;
                    ped.IsInvincible = true;
                    ped.CanBeTargetted = false;

                    // Move him far away so Rockstar never sees him "die"
                    ped.Position = new Vector3(0f, 0f, 50f);

                    // Release him cleanly
                    ped.IsPersistent = false;
                    ped.MarkAsNoLongerNeeded();

                    // Mark that we just removed a native clerk for this store
                    store.NativeClerkRemovedRecently = true;
                    store.NativeClerkRemovedUtc = DateTime.UtcNow;
                }
            }
        }
    }
}
