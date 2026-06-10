using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System.Drawing;

namespace StoreRobberyEnhanced.Debug
{
    internal static class DebugStoreOverlay
    {
        public static void Draw(StoreContext ctx)
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
                return;

            Vector3 p = player.Position;

            int y = 40;
            int line = 18;

            foreach (var s in ctx.Stores)
            {
                if (s == null)
                    continue;

                float dist = p.DistanceTo(s.StorePos);
                bool inside = dist <= s.Radius;

                // ------------------------------------------------------------
                // PATCH 12 — EXTENDED STATE MACHINE DEBUG LINE
                // ------------------------------------------------------------
                string text =
                    $"{s.Name}  " +
                    $"Dist:{dist:0.0}  " +
                    $"Inside:{inside}  " +
                    $"RobberyActive:{s.IsRobberyActive}  " +
                    $"RobberyEnded:{s.RobberyEnded}  " +
                    $"Cooldown:{s.CooldownActive}  " +
                    $"SurrenderStage:{s.ClerkSurrenderStage}  " +
                    $"Phase:" +
                        $"{(s.ClerkStalling ? "STALL " : "")}" +
                        $"{(s.ClerkOpeningRegister ? "OPEN " : "")}" +
                        $"{(s.ClerkGrabbingCash ? "CASH " : "")}" +
                        $"{(s.ClerkThrowingBag ? "BAG " : "")}" +
                        $"{(s.ClerkPanicking ? "PANIC " : "")}" +
                        $"{(s.ClerkFleeing ? "FLEE " : "")}" +
                    $"Heat:{s.HeatLevel}  " +
                    $"Alarm:{s.AlarmTriggered}  " +
                    $"Pending:{s.PendingPayout}  " +
                    $"Collected:{s.CollectedPayout}  " +
                    $"Silent:{s.SilentRobbery}  " +
                    $"SafeCrack:{(ctx.SafeCrack != null && ctx.SafeCrack.IsRunning)}  " +
                    $"Reaction:{s.ReactionType}  " +
                    $"Door:{FormatVec(s.DoorPos)}  " +
                    $"Safe:{FormatVec(s.SafePos)}  " +
                    $"Clerk:{FormatVec(s.ClerkPos)}  " +
                    $"Blocker:{(s.CooldownBlocker != null ? "Yes" : "No")}  " +
                    $"DefClerkRemoved:{s.DefaultClerkRemoved}";

                DrawText(text, 0.01f, y / 1080f, HudColor.White);
                y += line;
            }
        }

        private static string FormatVec(Vector3 v)
        {
            return $"({v.X:0.0},{v.Y:0.0},{v.Z:0.0})";
        }

        private static void DrawText(string text, float x, float y, HudColor color)
        {
            Function.Call(Hash.SET_TEXT_FONT, 0);
            Function.Call(Hash.SET_TEXT_SCALE, 0.3f, 0.3f);
            Function.Call(Hash.SET_TEXT_COLOUR, color, 255);
            Function.Call(Hash.SET_TEXT_OUTLINE);
            Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT, "STRING");
            Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
            Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_TEXT, x, y);
        }
    }
}
