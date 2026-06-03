using GTA;
using GTA.Native;
using StoreRobberyTrackerMod.Config;
using StoreRobberyTrackerMod.Debug;

namespace StoreRobberyTrackerMod.Minigame
{
    internal class SafeCrackUI : ISafeCrackUI
    {
        // Rockstar texture dictionaries
        private const string DICT_SAFE = "MPSafeCracking";
        private const string DICT_ARROW = "GolfPutting";

        // Sprite names
        private const string SPRITE_DIAL_BG = "Dial_BG";
        private const string SPRITE_DIAL = "Dial";
        private const string SPRITE_LOCK_CLOSED = "lock_closed";
        private const string SPRITE_LOCK_OPEN = "lock_open";
        private const string SPRITE_ARROW = "PuttingMarker";

        // ------------------------------------------------------------
        // MAIN DRAW ENTRY
        // ------------------------------------------------------------
        public void Draw(SafeCrackState state, SafeCrackSettings settings)
        {
            try
            {
                if (!state.Active)
                    return;

                // 🔊 Hard proof this is running
                DebugLogger.Trace("[SafeCrackUI] Draw() called — state active");

                LoadTextures();

                DrawDial(state);
                DrawLocks(state);
                DrawDirectionArrow(state);
                DrawSweetSpotShake(state);
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException("SafeCrackUI.Draw", ex);
            }
        }

        public void Clear()
        {
            // UI is drawn per-frame; nothing persistent to clear.
        }

        // ------------------------------------------------------------
        // LOAD TEXTURES
        // ------------------------------------------------------------
        private void LoadTextures()
        {
            if (!Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, DICT_SAFE))
                Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, DICT_SAFE, true);

            if (!Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, DICT_ARROW))
                Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, DICT_ARROW, true);
        }

        // ------------------------------------------------------------
        // DRAW DIAL BACKGROUND + ROTATING DIAL
        // ------------------------------------------------------------
        private void DrawDial(SafeCrackState state)
        {
            float x = 0.50f;
            float y = 0.50f;

            float scaleX = 0.30f;
            float scaleY = scaleX * GetAspectRatio();

            // Background
            DrawSprite(DICT_SAFE, SPRITE_DIAL_BG, x, y, scaleX, scaleY, 0f, 255, 255, 255, 255);

            // Rotating dial
            DrawSprite(
                DICT_SAFE,
                SPRITE_DIAL,
                x,
                y,
                scaleX * 0.50f,
                scaleY * 0.50f,
                -state.CurrentDialRotation,
                255, 255, 255, 255
            );
        }

        // ------------------------------------------------------------
        // DRAW LOCK INDICATORS (3 STAGES)
        // ------------------------------------------------------------
        private void DrawLocks(SafeCrackState state)
        {
            float baseX = 0.40f;
            float y = 0.80f;

            float scaleX = 0.06f;
            float scaleY = scaleX * GetAspectRatio();

            bool p1 = state.Stage >= 1;
            bool p2 = state.Stage >= 2;
            bool p3 = state.Stage >= 3;

            DrawSprite(DICT_SAFE, p1 ? SPRITE_LOCK_OPEN : SPRITE_LOCK_CLOSED, baseX, y, scaleX, scaleY, 0f, 255, 255, 255, 255);
            DrawSprite(DICT_SAFE, p2 ? SPRITE_LOCK_OPEN : SPRITE_LOCK_CLOSED, baseX + 0.10f, y, scaleX, scaleY, 0f, 255, 255, 255, 255);
            DrawSprite(DICT_SAFE, p3 ? SPRITE_LOCK_OPEN : SPRITE_LOCK_CLOSED, baseX + 0.20f, y, scaleX, scaleY, 0f, 255, 255, 255, 255);
        }

        // ------------------------------------------------------------
        // DIRECTION ARROW (LEFT OR RIGHT)
        // ------------------------------------------------------------
        private void DrawDirectionArrow(SafeCrackState state)
        {
            float x = state.DirectionRight ? 0.52f : 0.48f;
            float y = 0.30f;

            float scaleX = 0.06f;
            float scaleY = scaleX * GetAspectRatio();

            float rotation = state.DirectionRight ? 270f : 90f;

            DrawSprite(DICT_ARROW, SPRITE_ARROW, x, y, scaleX, scaleY, rotation, 0, 255, 0, 255);
        }

        // ------------------------------------------------------------
        // SHAKE EFFECT WHEN NEAR SWEET SPOT
        // ------------------------------------------------------------
        private void DrawSweetSpotShake(SafeCrackState state)
        {
            if (!state.IsInSweetSpot)
                return;

            // Shake intensity based on closeness
            float intensity = 0.0015f * state.SweetSpotCloseness;

            float shakeX = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, -intensity, intensity);
            float shakeY = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, -intensity, intensity);

            // Draw a subtle green highlight text
            DrawText("Sweet Spot!", 0.50f + shakeX, 0.65f + shakeY, 0.55f, 0, 255, 0);
        }

        // ------------------------------------------------------------
        // DRAW SPRITE HELPER
        // ------------------------------------------------------------
        private void DrawSprite(string dict, string name, float x, float y, float w, float h, float rot, int r, int g, int b, int a)
        {
            Function.Call(Hash.DRAW_SPRITE, dict, name, x, y, w, h, rot, r, g, b, a);
        }

        // ------------------------------------------------------------
        // TEXT DRAW HELPER
        // ------------------------------------------------------------
        private void DrawText(string text, float x, float y, float scale, int r, int g, int b)
        {
            Function.Call(Hash.SET_TEXT_FONT, 0);
            Function.Call(Hash.SET_TEXT_SCALE, scale, scale);
            Function.Call(Hash.SET_TEXT_COLOUR, r, g, b, 255);
            Function.Call(Hash.SET_TEXT_OUTLINE);

            Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT, "STRING");
            Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
            Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_TEXT, x, y);
        }

        // ------------------------------------------------------------
        // ASPECT RATIO HELPER
        // ------------------------------------------------------------
        private float GetAspectRatio()
        {
            return Function.Call<float>(Hash.GET_SCREEN_ASPECT_RATIO, true);
        }
    }
}
