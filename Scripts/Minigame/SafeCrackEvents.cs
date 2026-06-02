using System;
using GTA.Math;

namespace StoreRobberyTrackerMod.Minigame
{
    /// <summary>
    /// Provides event hooks for the SafeCrack minigame.
    /// Allows other systems to subscribe to safe-cracked notifications.
    /// </summary>
    internal static class SafeCrackEvents
    {
        // Fired when a safe is successfully cracked.
        public static event Action<Vector3, int> SafeCracked;

        /// <summary>
        /// Invokes the SafeCracked event.
        /// </summary>
        public static void OnSafeCracked(Vector3 position, int payout)
        {
            Action<Vector3, int> handler = SafeCracked;
            if (handler != null)
            {
                handler(position, payout);
            }
        }
    }
}
