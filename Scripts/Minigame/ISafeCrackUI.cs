using StoreRobberyTrackerMod.Config;

namespace StoreRobberyTrackerMod.Minigame
{
    internal interface ISafeCrackUI
    {
        void Draw(SafeCrackState state, SafeCrackSettings settings);
        void Clear();
    }
}
