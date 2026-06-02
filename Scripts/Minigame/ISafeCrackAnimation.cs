using GTA;
using GTA.Math;

namespace StoreRobberyTrackerMod.Minigame
{
    internal interface ISafeCrackAnimation
    {
        void Begin(Ped player, Vector3 safePos, float safeHeading);
        void UpdateLoop(Ped player);
        void EndSuccess(Ped player);
        void End(Ped player, bool success);
    }
}
