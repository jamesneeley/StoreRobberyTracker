using GTA;
using System;
using System.Threading.Tasks;
using StoreRobberyTrackerMod.UI;

namespace StoreRobberyTrackerMod.Debug
{
    internal class DebugScenarios
    {
        private static bool _runningScenario = false;
        private static UiHelpers _ui;

        public static void Init(UiHelpers ui)
        {
            _ui = ui;
        }

        internal async void RunFullRobberyScenario()
        {
            if (_runningScenario)
            {
                _ui.ShowNotification("~r~Scenario already running.");
                return;
            }

            _runningScenario = true;
            DebugLogger.Info("Starting Full Robbery Scenario");
            DebugEvents.Emit(DebugEvents.EventType.Custom, "Scenario", "FullRobbery");

            try
            {
                DebugLogger.Info("Scenario: RobberyStart");
                DebugActions.TriggerRobberyStart();
                await Delay(1500);

                DebugLogger.Info("Scenario: CameraAlarm");
                DebugActions.TriggerCameraAlarm();
                await Delay(1500);

                DebugLogger.Info("Scenario: SafeCrack");
                DebugActions.TriggerSafeCrack();
                await Delay(2000);

                DebugLogger.Info("Scenario: Escape");
                DebugActions.TriggerEscape();
                await Delay(1500);

                DebugLogger.Info("Scenario: Payout");
                DebugActions.TriggerPayout();
                await Delay(1500);

                DebugLogger.Info("Scenario: Cooldown");
                DebugActions.TriggerCooldown();

                _ui.ShowNotification("~g~Full Robbery Scenario Complete");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("FullRobberyScenario", ex);
            }
            finally
            {
                _runningScenario = false;
            }
        }

        internal async void RunQuickLootScenario()
        {
            if (_runningScenario)
            {
                _ui.ShowNotification("~r~Scenario already running.");
                return;
            }

            _runningScenario = true;
            DebugLogger.Info("Starting Quick Loot Scenario");
            DebugEvents.Emit(DebugEvents.EventType.Custom, "Scenario", "QuickLoot");

            try
            {
                DebugActions.TriggerRobberyStart();
                await Delay(1000);

                DebugActions.TriggerSafeCrack();
                await Delay(1500);

                DebugActions.TriggerPayout();

                _ui.ShowNotification("~g~Quick Loot Scenario Complete");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("QuickLootScenario", ex);
            }
            finally
            {
                _runningScenario = false;
            }
        }

        private static Task Delay(int ms)
        {
            return Task.Run(() => Script.Wait(ms));
        }
    }
}
