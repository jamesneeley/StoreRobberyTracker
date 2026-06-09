using GTA;
using GTA.Math;
using StoreRobberyTrackerMod.Systems;
using StoreRobberyTrackerMod.UI;
using System;

namespace StoreRobberyTrackerMod.Debug
{
    internal static class DebugActions
    {
        private static UiHelpers _ui;
        private static StoreContext _ctx;
        public static bool IsReady => _ui != null && _ctx != null;

        public static void Init(UiHelpers ui, StoreContext ctx)
        {
            _ui = ui;
            _ctx = ctx;
        }

        // ------------------------------------------------------------
        // ROBBERY START
        // ------------------------------------------------------------
        public static void TriggerRobberyStart()
        {
            if (_ctx.Robberies.TryStartDebugRobbery(out string msg))
                _ui.ShowNotification("~g~RobberyStart OK~s~: " + msg);
            else
                _ui.ShowNotification("~r~RobberyStart FAILED~s~: " + msg);
        }

        // ------------------------------------------------------------
        // SAFE CRACK (INSTANT)
        // ------------------------------------------------------------
        public static void TriggerSafeCrack()
        {
            if (_ctx.Safes.DebugForceSafeCrack(out string msg))
                _ui.ShowNotification("~g~SafeCrack OK~s~: " + msg);
            else
                _ui.ShowNotification("~r~SafeCrack FAILED~s~: " + msg);
        }

        // ------------------------------------------------------------
        // SAFE CRACK MINIGAME
        // ------------------------------------------------------------
        public static void TriggerSafeCrackMini()
        {
            if (_ctx.SafeCrack == null || _ctx.SafeState == null)
            {
                _ui.ShowNotification("~r~SafeCrack system not initialized yet");
                return;
            }

            if (_ctx.Safes.DebugStartSafeCrack(out string msg))
                _ui.ShowNotification("~g~SafeCrack Minigame OK~s~: " + msg);
            else
                _ui.ShowNotification("~r~SafeCrack Minigame FAILED~s~: " + msg);
        }

        // ------------------------------------------------------------
        // CAMERA ALARM
        // ------------------------------------------------------------
        public static void TriggerCameraAlarm()
        {
            _ctx.Cameras.DebugTriggerAlarm();
            _ui.ShowNotification("~y~Camera Alarm Triggered");
        }

        // ------------------------------------------------------------
        // ESCAPE
        // ------------------------------------------------------------
        public static void TriggerEscape()
        {
            _ctx.Robberies.DebugForceEscape();
            _ui.ShowNotification("~b~Escape Triggered");
        }

        // ------------------------------------------------------------
        // PAYOUT
        // ------------------------------------------------------------
        public static void TriggerPayout()
        {
            int amount = _ctx.Robberies.DebugForcePayout();
            _ui.ShowNotification($"~g~Payout Triggered: ~w~${amount}");
        }

        // ------------------------------------------------------------
        // COOLDOWN
        // ------------------------------------------------------------
        public static void TriggerCooldown()
        {
            var s = _ctx.GetNearestStore();

            if (s == null)
            {
                _ui.ShowNotification("~r~No store found near player");
                return;
            }

            _ui.ShowNotification($"DoorPos: {s.DoorPos}");

            _ctx.Cooldowns.DebugForceCooldown();
            _ui.ShowNotification("~c~Cooldown Triggered");
        }

        // ------------------------------------------------------------
        // STALKER
        // ------------------------------------------------------------
        public static void TriggerStalker()
        {
            _ctx.Stalker.DebugForceStalker();
            _ui.ShowNotification("~r~Stalker Triggered");
        }

        // ------------------------------------------------------------
        // UI TEST
        // ------------------------------------------------------------
        public static void TriggerUI()
        {
            _ui.ShowNotification("~b~UI Test Triggered");
            _ui.ShowHelpText("This is a debug help text.");
        }

        // ------------------------------------------------------------
        // BANNER TEST
        // ------------------------------------------------------------
        public static void TriggerBanner()
        {
            _ui.ShowHeistPassedBanner("DEBUG BANNER", "This is a test banner.");
        }

        // ------------------------------------------------------------
        // TIMER TEST
        // ------------------------------------------------------------
        public static void TriggerTimer()
        {
            // 10 seconds remaining, simple label
            _ui.SetTimerText("Debug Timer: 10", 10);
        }

        // ------------------------------------------------------------
        // MULTIPOS TEST
        // ------------------------------------------------------------
        public static void TriggerMultiPos()
        {
            try
            {
                Ped p = Game.Player.Character;
                if (p == null || !p.Exists())
                {
                    _ui.ShowNotification("~r~Player not found.");
                    return;
                }

                Vector3 pos = p.Position;
                float heading = p.Heading;

                string formatted =
                    $"new Vector3({pos.X:0.000}f, {pos.Y:0.000}f, {pos.Z:0.000}f), Heading={heading:0.00}f";

                // Show on-screen
                _ui.ShowNotification($"~b~POS:~w~ {pos.X:0.000}, {pos.Y:0.000}, {pos.Z:0.000}\n~b~Heading:~w~ {heading:0.00}");

                // Also show in your mod’s UI
                _ui.ShowNotification("~g~MultiPos captured");

                // Log it for later use
                DebugLogger.Info($"[MultiPos] {formatted}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("DebugActions.TriggerMultiPos", ex);
            }
        }

        // ------------------------------------------------------------
        // MISC ACTIONS TEST
        // ------------------------------------------------------------
        public static void TriggerMiscActions()
        {
            try
            {
                // 1. Run store checker (existing)
                StoreChecker();

                // 2. Dump profiler report (if enabled)
                if (DebugState.EnableProfiler)
                    DebugProfiler.DumpToFile();

                // 3. Dump store snapshot (if file manager enabled)
                if (DebugState.EnableFileManager)
                    DebugFileManager.WriteSnapshot("StoreSnapshot", _ctx.Stores);

                // 4. Log global heat
                DebugLogger.Info($"[MiscActions] GlobalHeat={_ctx.GlobalHeatLevel}");

                _ui.ShowNotification("~b~Misc Actions Executed");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("DebugActions.TriggerMiscActions", ex);
            }
        }

        // ------------------------------------------------------------
        // SCENARIO FULL ROBBERY TEST
        // ------------------------------------------------------------
        public static void TriggerScenarioFullRobbery()
        {
            StoreChecker();
            _ui.ShowNotification("~b~Scenario  Full Robbery");
            _ctx.Scenarios.RunFullRobberyScenario();
        }

        // ------------------------------------------------------------
        // SCENARIO QUICK LOOT TEST
        // ------------------------------------------------------------
        public static void TriggeScenarioQuickLoot()
        {
            StoreChecker();
            _ui.ShowNotification("~b~Scenario Quick Loot");
            _ctx.Scenarios.RunQuickLootScenario();
        }

        // ------------------------------------------------------------
        // CAMERA DEBUG OVERLAY
        // ------------------------------------------------------------
        public static bool CameraDebugEnabled = false;

        public static void ToggleCameraDebug()
        {
            CameraDebugEnabled = !CameraDebugEnabled;
            string state = CameraDebugEnabled ? "~g~ENABLED" : "~r~DISABLED";
            _ui.ShowNotification($"~b~Camera Debug Overlay {state}");
        }

        // ------------------------------------------------------------
        // STORE DIAGNOSTICS
        // ------------------------------------------------------------
        public static void TriggerStoreDiagnostics()
        {
            var s = _ctx.GetNearestStore();
            if (s == null)
            {
                _ui.ShowNotification("~r~No store found near player");
                return;
            }

            string msg =
                $"~b~STORE DIAGNOSTICS~w~\n" +
                $"ID: ~y~{s.Id}~w~\n" +
                $"Name: {s.Name}\n" +
                $"~g~Clerk Position~w~\n" +
                $"Pos: {s.ClerkPos}\n" +
                $"Heading: {s.ClerkHeading}\n" +
                $"~g~Register~w~\n" +
                $"Pos: {s.RegisterPos}\n" +
                $"Heading: {s.RegisterHeading}\n" +
                $"~g~Safe~w~\n" +
                $"Pos: {s.SafePos}\n" +
                $"Heading: {s.SafeHeading}\n" +
                $"~g~Door~w~\n" +
                $"Pos: {s.DoorPos}\n" +
                $"Radius: {s.Radius}\n" +
                $"~g~Cameras:~w~ {s.Cameras.Count}\n" +
                $"Robbed: {s.IsRobbed}   Cooldown: {s.CooldownActive}";

            _ui.ShowNotification(msg);
        }

        public static void StoreChecker()
        {
            var s = _ctx.GetNearestStore();
            if (s == null)
            {
                _ui.ShowNotification("~r~No store found near player");
                return;
            }
        }
    }
}
