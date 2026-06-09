using System;
using System.Drawing;   
using GTA;
using GTA.Native;
using GTA.UI;
using GTA.Math;
using StoreRobberyEnhanced.Data;

namespace StoreRobberyEnhanced.Debug
{
    internal static class DebugCameraRender
    {
        public static void Draw(StoreContext ctx)
        {
            var store = ctx.GetNearestStore();
            if (store == null)
                return;

            foreach (var cam in store.Cameras)
            {
                DrawCamera(cam, store);
            }
        }

        private static void DrawCamera(CameraData cam, TrackedStore store)
        {
            Vector3 pos = cam.Position;

            // Draw camera sphere
            World.DrawMarker(
                MarkerType.DebugSphere,
                pos,
                Vector3.Zero,
                Vector3.Zero,
                new Vector3(0.10f, 0.10f, 0.10f),
                cam.Destroyed ? Color.Red : Color.Cyan
            );

            // Draw forward direction (using ClerkPos as fallback)
            Vector3 dir = (store.ClerkPos - pos);
            dir.Normalize();

            Vector3 end = pos + dir * 2.0f;

            World.DrawLine(pos, end, cam.Destroyed ? Color.Red : Color.Yellow);

            // Grace period indicator
            if (cam.GraceActive)
            {
                World.DrawMarker(
                    MarkerType.DebugSphere,
                    pos + new Vector3(0, 0, 0.25f),
                    Vector3.Zero,
                    Vector3.Zero,
                    new Vector3(0.15f, 0.15f, 0.15f),
                    Color.Orange
                );
            }
        }
    }
}
