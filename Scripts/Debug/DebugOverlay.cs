using GTA;
using GTA.Native;
using System;
using System.Text;

namespace StoreRobberyEnhanced.Debug
{
    internal static class DebugOverlay
    {
        /// <summary>
        /// Draws the debug overlay if enabled.
        /// Called every tick from DebugController.Update().
        /// </summary>
        internal static void Draw(IniConfig config)
        {
            if (!DebugState.OverlayVisible)
                return;

            StringBuilder sb = new StringBuilder();

            // Header
            sb.AppendLine("~y~STORE ROBBERY ENHANCED DEBUG~s~");

            // Session info
            TimeSpan uptime = DateTime.UtcNow - DebugState.SessionStart;
            sb.AppendLine($"Uptime: ~c~{uptime:hh\\:mm\\:ss}");

            // Last action
            if (DebugState.LastActionTime != DateTime.MinValue)
            {
                TimeSpan since = DateTime.UtcNow - DebugState.LastActionTime;
                sb.AppendLine($"Last: ~g~{DebugState.LastActionName}~s~ ({since.Seconds}s ago)");
            }
            else
            {
                sb.AppendLine("Last: ~c~None");
            }

            // Debug level
            sb.AppendLine($"Level: ~c~{DebugState.DebugLevel}");

            sb.AppendLine(""); // spacing

            // Flags from IniConfig
            sb.AppendLine($"F9 DebugTimer: ~{(config.EnableDebugTimer ? "g" : "r")}~{config.EnableDebugTimer}");
            sb.AppendLine($"1 RobberyStart: ~{(config.Debug_RobberyStart ? "g" : "r")}~{config.Debug_RobberyStart}");
            sb.AppendLine($"2 SafeCrack:    ~{(config.Debug_SafeCrack ? "g" : "r")}~{config.Debug_SafeCrack}");
            sb.AppendLine($"3 CameraAlarm:  ~{(config.Debug_CameraAlarm ? "g" : "r")}~{config.Debug_CameraAlarm}");
            sb.AppendLine($"4 Escape:       ~{(config.Debug_Escape ? "g" : "r")}~{config.Debug_Escape}");
            sb.AppendLine($"5 Payout:       ~{(config.Debug_Payout ? "g" : "r")}~{config.Debug_Payout}");
            sb.AppendLine($"6 Cooldown:     ~{(config.Debug_Cooldown ? "g" : "r")}~{config.Debug_Cooldown}");
            sb.AppendLine($"7 Stalker:      ~{(config.Debug_Stalker ? "g" : "r")}~{config.Debug_Stalker}");
            sb.AppendLine($"8 UI:           ~{(config.Debug_UI ? "g" : "r")}~{config.Debug_UI}");
            sb.AppendLine($"9 Banner:       ~{(config.Debug_Banner ? "g" : "r")}~{config.Debug_Banner}");
            sb.AppendLine($"0 Timer:        ~{(config.Debug_Timer ? "g" : "r")}~{config.Debug_Timer}");
            sb.AppendLine($"F10 StoreDiag:  ~{(config.Debug_StoreDiag ? "g" : "r")}~{config.Debug_StoreDiag}");
            sb.AppendLine($"/ MultiPos:     ~{(config.Debug_MultiPos ? "g" : "r")}~{config.Debug_MultiPos}");
            sb.AppendLine($"* MultiActions: ~{(config.Debug_MultiActions ? "g" : "r")}~{config.Debug_MultiActions}");
            sb.AppendLine($"F3 CameraDebug: ~{(config.Debug_CameraDebug ? "g" : "r")}~{config.Debug_CameraDebug}");

            // ------------------------------------------------------------
            // PATCH 12 — GLOBAL ROBBERY / CLERK STATE MACHINE OVERLAY
            // ------------------------------------------------------------

            var store = DebugState.LastStore; // optional: set by your DebugController
            if (store != null)
            {
                sb.AppendLine("");
                sb.AppendLine("~y~--- ACTIVE STORE ---~s~");
                sb.AppendLine($"Store: ~c~{store.Name}");

                sb.AppendLine($"RobberyActive: ~c~{store.IsRobberyActive}");
                sb.AppendLine($"RobberyEnded:  ~c~{store.RobberyEnded}");
                sb.AppendLine($"Cooldown:      ~c~{store.CooldownActive}");

                if (store.CooldownActive)
                {
                    TimeSpan cd = DateTime.UtcNow - store.CooldownStartUtc;
                    sb.AppendLine($"CooldownTime:  ~c~{cd.TotalSeconds:0}s");
                }

                sb.AppendLine($"SilentRobbery: ~c~{store.SilentRobbery}");
                sb.AppendLine($"SafeCrackRun:  ~c~{(DebugState.SafeCrackRunning ? "Yes" : "No")}");

                sb.AppendLine("");
                sb.AppendLine("~y~--- CLERK STATE ---~s~");

                sb.AppendLine($"SurrenderStage: ~c~{store.ClerkSurrenderStage}");

                string phase =
                    (store.ClerkStalling ? "STALL " : "") +
                    (store.ClerkOpeningRegister ? "OPEN " : "") +
                    (store.ClerkGrabbingCash ? "CASH " : "") +
                    (store.ClerkThrowingBag ? "BAG " : "") +
                    (store.ClerkPanicking ? "PANIC " : "") +
                    (store.ClerkFleeing ? "FLEE " : "");

                sb.AppendLine($"Phase: ~c~{(phase.Length == 0 ? "IDLE" : phase)}");

                sb.AppendLine($"Heat: ~c~{store.HeatLevel}");
                sb.AppendLine($"Alarm: ~c~{store.AlarmTriggered}");

                sb.AppendLine($"PendingPayout:  ~c~{store.PendingPayout}");
                sb.AppendLine($"Collected:      ~c~{store.CollectedPayout}");

                sb.AppendLine($"ReactionType:   ~c~{store.ReactionType}");

                // Patch 10 corruption detection
                int active =
                    (store.ClerkStalling ? 1 : 0) +
                    (store.ClerkOpeningRegister ? 1 : 0) +
                    (store.ClerkGrabbingCash ? 1 : 0) +
                    (store.ClerkThrowingBag ? 1 : 0) +
                    (store.ClerkPanicking ? 1 : 0) +
                    (store.ClerkFleeing ? 1 : 0);

                if (active > 1)
                    sb.AppendLine("~r~STATE CORRUPTION DETECTED (PATCH10)~s~");
            }

            DrawTextTopRight(sb.ToString(), 0.985f, 0.015f, 0.35f);
        }

        /// <summary>
        /// Draws text aligned to the top-right corner.
        /// </summary>
        private static void DrawTextTopRight(string text, float x, float y, float scale)
        {
            Function.Call(Hash.SET_TEXT_FONT, 0);
            Function.Call(Hash.SET_TEXT_SCALE, 0.0f, scale);
            Function.Call(Hash.SET_TEXT_COLOUR, 255, 255, 255, 220);
            Function.Call(Hash.SET_TEXT_WRAP, 0.0f, x);
            Function.Call(Hash.SET_TEXT_RIGHT_JUSTIFY, true);
            Function.Call(Hash.SET_TEXT_OUTLINE);

            Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT, "STRING");
            Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
            Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_TEXT, x, y);
        }
    }
}
