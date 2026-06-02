using StoreRobberyTrackerMod.Config;

namespace StoreRobberyTrackerMod.Minigame
{
    internal interface ISafeCrackLogic
    {
        void Initialize(SafeCrackState state, SafeCrackSettings settings);
        void Update(SafeCrackState state, SafeCrackSettings settings);
        bool ValidatePlayerStillEligible(SafeCrackState state);
        int CalculatePayout(SafeCrackSettings settings);
    }
}
