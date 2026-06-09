using StoreRobberyEnhanced.Config;

namespace StoreRobberyEnhanced.Minigame
{
    internal interface ISafeCrackInput
    {
        void Process(SafeCrackState state, SafeCrackSettings settings);
    }
}
