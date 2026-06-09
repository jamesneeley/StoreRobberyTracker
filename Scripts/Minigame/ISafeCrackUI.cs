using StoreRobberyEnhanced.Config;

namespace StoreRobberyEnhanced.Minigame
{
    internal interface ISafeCrackUI
    {
        void Draw(SafeCrackState state, SafeCrackSettings settings);
        void Clear();

        // ⭐ Add this:
        void Enable();
    }
}
