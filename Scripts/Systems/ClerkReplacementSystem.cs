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

        // ------------------------------------------------------------
        // ENSURE DEFAULT CLERK REMOVED
        // ------------------------------------------------------------
        private void EnsureDefaultClerkRemoved(TrackedStore store)
        {
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

        // ------------------------------------------------------------
        // REMOVE DEFAULT CLERKS
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

                // mark that this was NOT our clerk
                store.IsOurClerk = false;

                // SHVDN 3.9.0: valid store clerk models
                //if (ped.Model.Hash == (int)PedHash.ShopKeep01 ||
                //    ped.Model.Hash == (int)PedHash.ShopMaskSMY)
                //{
                //    ped.Delete();
                //}
                int hash = ped.Model.Hash;
                //if (ped.Model.Hash == (int)PedHash.ShopKeep01 || ped.Model.Hash == (int)PedHash.ShopMaskSMY)
                if (Array.IndexOf(_defaultClerkModels, hash) != -1)
                {
                    // Make sure he doesn't react or cause wanted level
                    ped.Task.ClearAllImmediately();
                    ped.BlockPermanentEvents = true;
                    ped.CanBeTargetted = false;
                    ped.IsInvincible = true;

                    ped.IsVisible = false;
                    // Move him far away / underground so he’s effectively gone
                    ped.Position = new Vector3(0f, 0f, -100f);
                    ped.IsPersistent = false;
                    ped.MarkAsNoLongerNeeded();
                }
            }
        }
    }
}
