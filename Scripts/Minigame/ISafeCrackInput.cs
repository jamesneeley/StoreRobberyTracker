using StoreRobberyTrackerMod.Config;

namespace StoreRobberyTrackerMod.Minigame
{
    internal interface ISafeCrackInput
    {
        void Process(SafeCrackState state, SafeCrackSettings settings);
    }
}
