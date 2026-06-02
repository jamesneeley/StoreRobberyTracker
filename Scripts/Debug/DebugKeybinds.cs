using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace StoreRobberyTrackerMod.Debug
{
    internal static class DebugKeybinds
    {
        // ------------------------------------------------------------
        // WINDOWS API KEY POLLING
        // ------------------------------------------------------------
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static bool IsKeyDown(int vKey)
        {
            return (GetAsyncKeyState(vKey) & 0x8000) != 0;
        }

        // ------------------------------------------------------------
        // CONFIGURABLE KEYS
        // ------------------------------------------------------------
        public static int ModifierKey { get; private set; } = 162; // Left CTRL default

        private static readonly Dictionary<int, string> _actionMap =
            new Dictionary<int, string>();

        public static void ApplyConfig(int modifierKey, Dictionary<int, string> actionMap)
        {
            ModifierKey = modifierKey;

            _actionMap.Clear();

            if (actionMap != null)
            {
                foreach (var kv in actionMap)
                    _actionMap[kv.Key] = kv.Value;
            }
        }

        // ------------------------------------------------------------
        // MODIFIER
        // ------------------------------------------------------------
        public static bool ModifierHeld()
        {
            return IsKeyDown(ModifierKey);
        }

        // ------------------------------------------------------------
        // ACTION HANDLING (NO MUTATION, NO LATCH)
        // ------------------------------------------------------------
        public static bool TryGetAction(out string actionName)
        {
            actionName = null;

            if (!ModifierHeld())
                return false;

            foreach (var kv in _actionMap)
            {
                int key = kv.Key;

                if (IsKeyDown(key))
                {
                    actionName = kv.Value;
                    return true;
                }
            }

            return false;
        }

        // ------------------------------------------------------------
        // TOGGLE HANDLING
        // ------------------------------------------------------------
        public static bool IsTogglePressed(int toggleKey)
        {
            return ModifierHeld() && IsKeyDown(toggleKey);
        }

        // Optional test hook
        public static bool IsKeyDownPublic(int vKey)
        {
            return IsKeyDown(vKey);
        }
    }
}
