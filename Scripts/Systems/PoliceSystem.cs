using System;
using GTA;
using GTA.Native;
using StoreRobberyTrackerMod.Data;
using StoreRobberyTrackerMod.Debug;

namespace StoreRobberyTrackerMod.Systems
{
    internal class PoliceSystem
    {
        private readonly StoreContext _ctx;
        public bool SuppressPoliceForDebug = false;


        public PoliceSystem(StoreContext ctx)
        {
            _ctx = ctx;
        }

        // ------------------------------------------------------------
        // MAIN ENTRY
        // ------------------------------------------------------------
        public void UpdatePoliceLogic(TrackedStore store, Ped player)
        {
            try
            {
                // ⭐ NEW: Debug override — completely disable police logic
                if (SuppressPoliceForDebug)
                    return;

                // ⭐ If our clerk does not exist yet, do NOT treat anything as a robbery
                if (store.Clerk == null || !store.Clerk.Exists())
                {
                    store.IsRobberyActive = false;
                    store.ClerkDeathHandled = false;
                    store.ClerkCallingPolice = false;
                    store.SilentAlarmPressed = false;
                    store.AlarmTriggered = false;
                    store.ClerkReacted = false;
                    return;
                }

                // ⭐ If dummy clerk exists and no robbery is active, never escalate or trigger police
                if (!store.IsRobberyActive)
                    return;

                if (!_ctx.AnyRobberyActive)
                    return;

                if (!_ctx.Config.EnablePolice)
                    return;

                HandleCameraTriggeredAlarm(store);
                HandleClerkDeathAlarm(store);
                HandleSilentAlarm(store);
                HandleClerkCallingPolice(store);
                HandleRepeatRobberyEscalation(store);
                HandleMaskEscalation(store);
                HandleFightEscalation(store);
                HandleTimeEscalation(store);
                HandleRecognitionEscalation(store);

                ApplyHeatEffects(store);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.UpdatePoliceLogic", ex);
            }
        }

        // ------------------------------------------------------------
        // CAMERA DETECTION
        // ------------------------------------------------------------
        private void HandleCameraTriggeredAlarm(TrackedStore store)
        {
            try
            {
                if (!_ctx.Config.EnablePolice)
                    return;

                if (!store.IsRobberyActive || store.AlarmTriggered)
                    return;

                foreach (CameraData cam in store.Cameras)
                {
                    if (cam.Destroyed)
                        continue;

                    // ⭐ Require both camera grace AND clerk reaction to avoid false alarms
                    if (cam.GraceActive && store.ClerkReacted)
                    {
                        DebugLogger.Info($"Camera alarm triggered at {store.Name}");
                        TriggerPolice(store, 2, "~r~Camera detected suspicious activity!");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.HandleCameraTriggeredAlarm", ex);
            }
        }

        // ------------------------------------------------------------
        // CLERK DEATH
        // ------------------------------------------------------------
        private void HandleClerkDeathAlarm(TrackedStore store)
        {
            try
            {
                if (!_ctx.Config.EnablePolice)
                    return;

                if (!store.IsRobberyActive || store.AlarmTriggered)
                    return;

                // ⭐ Only our clerk’s tracked death should matter
                if (!store.ClerkDeathHandled)
                    return;

                DebugLogger.Info($"Clerk death alarm at {store.Name}, killedWithGun={store.ClerkKilledWithGun}");

                if (store.ClerkKilledWithGun)
                    TriggerPolice(store, 3, "~r~Clerk shot! Police alerted!");
                else
                    TriggerPolice(store, 2, "~r~Clerk found dead! Police alerted!");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.HandleClerkDeathAlarm", ex);
            }
        }

        // ------------------------------------------------------------
        // SILENT ALARM
        // ------------------------------------------------------------
        private void HandleSilentAlarm(TrackedStore store)
        {
            try
            {
                if (!_ctx.Config.EnablePolice)
                    return;

                if (!store.IsRobberyActive || store.AlarmTriggered)
                    return;

                if (!store.SilentAlarmPressed)
                    return;

                double elapsed = (DateTime.UtcNow - store.SilentAlarmUtc).TotalSeconds;
                if (elapsed >= _ctx.Config.SilentAlarmDelaySeconds)
                {
                    DebugLogger.Info($"Silent alarm triggered at {store.Name}");
                    TriggerPolice(store, 1, "~y~Silent alarm triggered!");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.HandleSilentAlarm", ex);
            }
        }

        // ------------------------------------------------------------
        // CLERK CALLING POLICE
        // ------------------------------------------------------------
        private void HandleClerkCallingPolice(TrackedStore store)
        {
            try
            {
                if (!_ctx.Config.EnablePolice)
                    return;

                if (!store.IsRobberyActive || store.AlarmTriggered)
                    return;

                if (!store.ClerkCallingPolice)
                    return;

                double elapsed = (DateTime.UtcNow - store.ClerkCallStartUtc).TotalSeconds;
                if (elapsed >= _ctx.Config.ClerkCallDelaySeconds)
                {
                    DebugLogger.Info($"Clerk called police at {store.Name}");
                    TriggerPolice(store, 2, "~r~Clerk called the police!");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.HandleClerkCallingPolice", ex);
            }
        }

        // ------------------------------------------------------------
        // REPEAT ROBBERY ESCALATION
        // ------------------------------------------------------------
        private void HandleRepeatRobberyEscalation(TrackedStore store)
        {
            try
            {
                if (!_ctx.Config.EnablePolice)
                    return;

                if (store.RepeatRobberyEscalationApplied)
                    return;

                if (store.TimesRobbed >= 4)
                {
                    store.HeatLevel += 2;
                    store.RepeatRobberyEscalationApplied = true;
                    DebugLogger.Info($"Repeat robbery escalation (+2 heat) at {store.Name}");
                }
                else if (store.TimesRobbed >= 2)
                {
                    store.HeatLevel += 1;
                    store.RepeatRobberyEscalationApplied = true;
                    DebugLogger.Info($"Repeat robbery escalation (+1 heat) at {store.Name}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.HandleRepeatRobberyEscalation", ex);
            }
        }

        // ------------------------------------------------------------
        // MASK ESCALATION
        // ------------------------------------------------------------
        private void HandleMaskEscalation(TrackedStore store)
        {
            try
            {
                if (!_ctx.Config.EnablePolice)
                    return;

                if (store.MaskEscalationApplied)
                    return;

                if (store.PlayerMaskedAtStart)
                {
                    store.HeatLevel += 1;
                    store.MaskEscalationApplied = true;
                    DebugLogger.Info($"Mask escalation (+1 heat) at {store.Name}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.HandleMaskEscalation", ex);
            }
        }

        // ------------------------------------------------------------
        // CLERK FIGHT-BACK ESCALATION
        // ------------------------------------------------------------
        private void HandleFightEscalation(TrackedStore store)
        {
            try
            {
                if (store.FightEscalationApplied)
                    return;

                if (store.ReactionType == ClerkReactionType.FightPistol ||
                    store.ReactionType == ClerkReactionType.FightShotgun)
                {
                    store.HeatLevel += 1;
                    store.FightEscalationApplied = true;
                    DebugLogger.Info($"Fight escalation (+1 heat) at {store.Name}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.HandleFightEscalation", ex);
            }
        }

        // ------------------------------------------------------------
        // TIME ESCALATION
        // ------------------------------------------------------------
        private void HandleTimeEscalation(TrackedStore store)
        {
            try
            {
                if (store.TimeEscalationApplied)
                    return;

                double elapsed = (DateTime.UtcNow - store.RobberyStartUtc).TotalSeconds;
                if (elapsed >= _ctx.Config.TimeEscalationSeconds)
                {
                    store.HeatLevel += 1;
                    store.TimeEscalationApplied = true;
                    DebugLogger.Info($"Time escalation (+1 heat) at {store.Name}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.HandleTimeEscalation", ex);
            }
        }

        // ------------------------------------------------------------
        // CLERK RECOGNITION ESCALATION
        // ------------------------------------------------------------
        private void HandleRecognitionEscalation(TrackedStore store)
        {
            try
            {
                if (store.ClerkRecognizedPlayer)
                {
                    store.HeatLevel += 1;
                    store.ClerkRecognizedPlayer = false; // consume once
                    DebugLogger.Info($"Recognition escalation (+1 heat) at {store.Name}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.HandleRecognitionEscalation", ex);
            }
        }

        // ------------------------------------------------------------
        // APPLY HEAT EFFECTS
        // ------------------------------------------------------------
        private void ApplyHeatEffects(TrackedStore store)
        {
            try
            {
                if (!_ctx.Config.EnablePolice)
                    return;

                if (!store.IsRobberyActive)
                    return;

                int wanted = Game.Player.WantedLevel;

                if (store.HeatLevel == 1 && wanted < 1)
                    Game.Player.WantedLevel = 1;

                if (store.HeatLevel == 2 && wanted < 2)
                    Game.Player.WantedLevel = 2;

                if (store.HeatLevel >= 3 && wanted < 3)
                    Game.Player.WantedLevel = 3;

                DebugLogger.Trace($"ApplyHeatEffects: store={store.Name}, heat={store.HeatLevel}, wanted={Game.Player.WantedLevel}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.ApplyHeatEffects", ex);
            }
        }

        // ------------------------------------------------------------
        // TRIGGER POLICE
        // ------------------------------------------------------------
        private void TriggerPolice(TrackedStore store, int wantedLevel, string message)
        {
            try
            {
                if (!_ctx.Config.EnablePolice)
                    return;

                if (!store.IsRobberyActive)
                    return;

                if (store.AlarmTriggered)
                    return;

                store.AlarmTriggered = true;

                if (wantedLevel > Game.Player.WantedLevel)
                    Game.Player.WantedLevel = wantedLevel;

                _ctx.Ui.ShowNotification(message);

                store.HeatLevel += 1;
                _ctx.GlobalHeatLevel += 1;

                DebugLogger.Info($"TriggerPolice: store={store.Name}, wanted={wantedLevel}, heat={store.HeatLevel}, globalHeat={_ctx.GlobalHeatLevel}");

                _ctx.SaveStoreState(store);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.TriggerPolice", ex);
            }
        }
    }
}
