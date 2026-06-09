using System;
using System.Drawing;
using GTA;
using GTA.Native;
using StoreRobberyTrackerMod.Debug;

namespace StoreRobberyTrackerMod.UI
{
    internal class UiHelpers
    {
        private Scaleform _heistScaleform;
        private IniConfig _config;

        public UiHelpers(IniConfig config)
        {
            try
            {
                _config = config;
                DebugLogger.Info("UiHelpers initialized");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.ctor", ex);
            }
        }

        // Timer system
        private string _activeTimerText = null;
        private int _activeTimerSeconds = 0;

        private float _timerAlpha = 0f;
        private bool _timerVisible = false;
        private int _timerFadeSpeed = 5;
        private bool _useScaleformTimer = false;

        private Scaleform _timerScaleform = null;
        private int _heistBannerEndTime = 0;

        // ------------------------------------------------------------
        // NOTIFICATION
        // ------------------------------------------------------------
        public void ShowNotification(string msg)
        {
            try
            {
                DebugLogger.Trace($"ShowNotification: {msg}");
                GTA.UI.Notification.PostTicker(msg, true);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.ShowNotification", ex);
            }
        }

        // ------------------------------------------------------------
        // SUBTITLE
        // ------------------------------------------------------------
        public void ShowSubtitle(string msg, int duration = 3000)
        {
            try
            {
                DebugLogger.Trace($"ShowSubtitle: {msg}");
                GTA.UI.Screen.ShowSubtitle(msg, duration);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.ShowSubtitle", ex);
            }
        }

        public void ShowHelpText(string text)
        {
            try
            {
                DebugLogger.Trace($"ShowHelpText: {text}");

                Function.Call((Hash)0x8509B634FBE7DA11, "STRING");
                Function.Call((Hash)0x6C188BE134E074AA, text);
                Function.Call((Hash)0x238FFE5C7B0498A6, 0, false, true, -1);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.ShowHelpText", ex);
            }
        }

        // ------------------------------------------------------------
        // BIG HEIST BANNER
        // ------------------------------------------------------------
        public void ShowHeistPassedBanner(string title, string subtitle)
        {
            try
            {
                DebugLogger.Info($"ShowHeistPassedBanner: {title} / {subtitle}");

                _heistScaleform = new Scaleform("MP_BIG_MESSAGE_FREEMODE");
                _heistScaleform.CallFunction("SHOW_SHARD_CENTERED_MP_MESSAGE", title, subtitle, 21, true, false);

                _heistBannerEndTime = Game.GameTime + 6000;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.ShowHeistPassedBanner", ex);
            }
        }

        // ------------------------------------------------------------
        // TEXT NOTIFICATIONS (STALKER)
        // ------------------------------------------------------------
        public void TextNotification(string avatar, string author, string title, string message)
        {
            try
            {
                DebugLogger.Info($"TextNotification: {title} / {message}");

                while (!Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, avatar))
                {
                    Script.Wait(10);
                    Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, avatar, 0);
                }

                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "CONFIRM_BEEP", "HUD_MINI_GAME_SOUNDSET");
                Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
                Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, message);

                Function.Call<int>(
                    Hash.END_TEXT_COMMAND_THEFEED_POST_MESSAGETEXT,
                    avatar, avatar, true, 0, title, author
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.TextNotification", ex);
            }
        }

        // ------------------------------------------------------------
        // DRAW LOOP
        // ------------------------------------------------------------
        public void Draw()
        {
            try
            {
                if (_heistScaleform != null)
                {
                    _heistScaleform.Render2D();

                    if (Game.GameTime > _heistBannerEndTime)
                    {
                        DebugLogger.Trace("Heist banner expired");
                        _heistScaleform = null;
                    }
                }

                if (_activeTimerText != null)
                    DrawTimer(_activeTimerText, _activeTimerSeconds);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.Draw", ex);
            }
        }

        // ------------------------------------------------------------
        // TIMER CONTROL
        // ------------------------------------------------------------
        public void SetTimerText(string text, int secondsRemaining = 0)
        {
            try
            {
                DebugLogger.Trace($"SetTimerText: {text} ({secondsRemaining}s)");
                _activeTimerText = text;
                _activeTimerSeconds = secondsRemaining;
                _timerVisible = true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.SetTimerText", ex);
            }
        }

        public void ClearTimer()
        {
            try
            {
                DebugLogger.Trace("ClearTimer()");
                _activeTimerText = null;
                _timerVisible = false;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.ClearTimer", ex);
            }
        }

        // ------------------------------------------------------------
        // TIMER RENDERING
        // ------------------------------------------------------------
        private void DrawTimer(string text, int secondsRemaining)
        {
            try
            {
                if (_timerVisible)
                {
                    if (_timerAlpha < 1f)
                        _timerAlpha += 0.01f * _timerFadeSpeed;

                    if (_timerAlpha > 1f)
                        _timerAlpha = 1f;
                }
                else
                {
                    if (_timerAlpha > 0f)
                        _timerAlpha -= 0.01f * _timerFadeSpeed;

                    if (_timerAlpha <= 0f)
                        return;
                }

                if (_useScaleformTimer)
                {
                    DrawScaleformTimer(text);
                    return;
                }

                DrawTimerInternal(text, secondsRemaining);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.DrawTimer", ex);
            }
        }

        // ------------------------------------------------------------
        // SCALEFORM TIMER
        // ------------------------------------------------------------
        private void DrawScaleformTimer(string text)
        {
            try
            {
                if (_timerScaleform == null)
                {
                    DebugLogger.Trace("Creating scaleform timer");
                    _timerScaleform = new Scaleform("MP_BIG_MESSAGE_FREEMODE");
                    _timerScaleform.CallFunction("SHOW_SHARD_WASTED_MP_MESSAGE", text, "", 5);
                }

                _timerScaleform.Render2D();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.DrawScaleformTimer", ex);
            }
        }

        // ------------------------------------------------------------
        // NORMAL TIMER (NATIVES)
        // ------------------------------------------------------------
        private void DrawTimerInternal(string text, int secondsRemaining)
        {
            try
            {
                var cfg = _config;

                bool flash = secondsRemaining <= 10 && (Game.GameTime % 500 < 250);

                int r = flash ? 255 : 255;
                int g = flash ? 50 : 255;
                int b = flash ? 50 : 255;
                int a = (int)(_timerAlpha * 255);

                float x = cfg.TimerPosX;
                float y = cfg.TimerPosY;

                float boxWidth = cfg.TimerBgWidth;
                float boxHeight = cfg.TimerBgHeight;

                if (cfg.TimerBackground)
                {
                    DrawTimerBackground(x, y, boxWidth, boxHeight, a, cfg.TimerBgOpacity, cfg.TimerBgR, cfg.TimerBgG, cfg.TimerBgB);
                }

                Function.Call(Hash.SET_TEXT_FONT, 0);
                Function.Call(Hash.SET_TEXT_SCALE, 0.0f, cfg.TimerScale);
                Function.Call(Hash.SET_TEXT_COLOUR, r, g, b, a);
                Function.Call(Hash.SET_TEXT_CENTRE, false);

                if (cfg.TimerDropShadow)
                    Function.Call(Hash.SET_TEXT_DROPSHADOW, 2, 0, 0, 0, 255);

                Function.Call(Hash.SET_TEXT_OUTLINE);

                Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT, "STRING");
                Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
                Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_TEXT, x, y);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.DrawTimerInternal", ex);
            }
        }

        // ------------------------------------------------------------
        // BACKGROUND BOX
        // ------------------------------------------------------------
        private void DrawTimerBackground(float x, float y, float width, float height, int alpha, float opacity, int r, int g, int b)
        {
            try
            {
                Function.Call(Hash.DRAW_RECT, x + width / 2f, y + height / 2f, width, height, r, g, b, (int)(alpha * opacity));
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.DrawTimerBackground", ex);
            }
        }
    }
}
