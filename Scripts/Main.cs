using System;
using System.Reflection;
using GTA;
using StoreRobberyEnhanced.Debug;
using StoreRobberyEnhanced.UI;

namespace StoreRobberyEnhanced
{
    public class Main : Script
    {
        private StoreContext _ctx;
        private bool _initialized;

        private DebugController _debug;

        private static string ScriptVersion =>
            Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public void ShowLoadedNotification()
        {
            try
            {
                if (_ctx.Config.EnableMessages)
                    GTA.UI.Notification.PostTicker($"~b~Store Robbery Enhanced v{ScriptVersion}~w~ is now active.", true);

                DebugLogger.Info($"Loaded notification shown (v{ScriptVersion})");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("Main.ShowLoadedNotification", ex);
            }
        }

        public Main()
        {
            try
            {
                DebugLogger.Info("Main constructor called");

                Tick += WaitForGameLoad;
                Aborted += OnAborted;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("Main.ctor", ex);
            }
        }

        // ------------------------------------------------------------
        // WAIT FOR GAME LOAD
        // ------------------------------------------------------------
        private void WaitForGameLoad(object sender, EventArgs e)
        {
            try
            {
                Ped player = Game.Player.Character;

                if (player == null || !player.Exists())
                    return;

                if (_initialized)
                    return;

                DebugLogger.Info("Game loaded — initializing mod");

                _initialized = true;
                Tick -= WaitForGameLoad;

                RemoveNativeHoldupBlipsGlobal();

                _ctx = new StoreContext(this);
                _ctx.Initialize();

                // ⭐ Initialize DebugLogger using INI setting
                DebugLogger.Initialize(_ctx.Config.EnableLogging);
                DebugEvents.Initialize(_ctx.Config.EnableEvents);
                DebugFileManager.Initialize(_ctx.Config.EnableFileManager);
                DebugProfiler.Initialize(_ctx.Config.EnableProfiler);

                // ⭐ DebugActions + DebugController now use the unified UI instance
                DebugActions.Init(StoreContext.GlobalUi, _ctx);
                _debug = new DebugController(this, StoreContext.GlobalUi, _ctx);

                _debug.ApplyKeybindConfig(
                    _ctx.Config.ModifierKey,
                    _ctx.Config.ToggleKey,
                    new System.Collections.Generic.Dictionary<int, string>
                    {
                        { _ctx.Config.Action_RobberyStart, "RobberyStart" },
                        { _ctx.Config.Action_SafeCrack, "SafeCrack" },
                        { _ctx.Config.Action_SafeCrackMini, "SafeCrackMini" },
                        { _ctx.Config.Action_CameraAlarm, "CameraAlarm" },
                        { _ctx.Config.Action_Escape, "Escape" },
                        { _ctx.Config.Action_Payout, "Payout" },
                        { _ctx.Config.Action_Cooldown, "Cooldown" },
                        { _ctx.Config.Action_Stalker, "Stalker" },
                        { _ctx.Config.Action_StalkerCall, "StalkerCall" },
                        { _ctx.Config.Action_UI, "UI" },
                        { _ctx.Config.Action_Banner, "Banner" },
                        { _ctx.Config.Action_Timer, "Timer" },
                        { _ctx.Config.Action_StoreDiag, "StoreDiag" },
                        { _ctx.Config.Action_MultiPos, "MultiPos" },
                        { _ctx.Config.Action_MiscActions, "MiscActions" },
                        { _ctx.Config.Scenario_FullRobbery, "ScenarioFullRobbery" },
                        { _ctx.Config.Scenario_QuickLoot, "ScnearioQuickLoor" },
                        { _ctx.Config.Action_CameraDebug, "CameraDebug" }
                    }
                );

                Tick += OnTick;

                DebugLogger.Info("Main initialization complete");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("Main.WaitForGameLoad", ex);
            }
        }

        // ------------------------------------------------------------
        // MAIN TICK LOOP (BANNER-SAFE)
        // ------------------------------------------------------------
        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                if (_ctx != null)
                {
                    _ctx.Update();
                }

                // SafeCrack UI
                if (_ctx != null &&
                    _ctx.SafeState != null &&
                    _ctx.SafeState.Active)
                {
                    _ctx.SafeCrackUI.Draw(_ctx.SafeState, _ctx.SafeCrackSettings);
                }

                // -------------------------------
                // STALKER SYSTEM TICK INTEGRATION
                // -------------------------------
                if (_ctx.Stalker != null)
                {
                    _ctx.Stalker.ProcessEvents();       // queued messages
                    _ctx.Stalker.UpdatePhone(); 
                }

                // Debug overlays
                if (DebugState.OverlayVisible)
                    DebugOverlay.Draw(_ctx.Config);

                if (DebugState.OverlayVisible)
                    DebugStoreOverlay.Draw(_ctx);

                // Banner LAST
                StoreContext.GlobalUi.Draw();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("Main.OnTick", ex);
            }
        }

        // ------------------------------------------------------------
        // CLEANUP ON ABORT
        // ------------------------------------------------------------
        private void OnAborted(object sender, EventArgs e)
        {
            try
            {
                DebugLogger.Info("Main.OnAborted called");

                if (_ctx != null)
                    _ctx.CleanupOnAbort();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("Main.OnAborted", ex);
            }
        }

        // ------------------------------------------------------------
        // GLOBAL BLIP CLEANUP
        // ------------------------------------------------------------
        private void RemoveNativeHoldupBlipsGlobal()
        {
            try
            {
                DebugLogger.Info("Removing native holdup blips (global)");

                Blip[] blips = World.GetAllBlips();

                foreach (Blip b in blips)
                {
                    try
                    {
                        if (!b.Exists())
                            continue;

                        if (b.Sprite == (BlipSprite)52)
                        {
                            DebugLogger.Trace("Deleting Rockstar store blip");
                            b.Delete();
                        }
                    }
                    catch (Exception exBlip)
                    {
                        DebugLogger.LogException("Main.RemoveNativeHoldupBlipsGlobal(blip)", exBlip);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("Main.RemoveNativeHoldupBlipsGlobal", ex);
            }
        }
    }
}
