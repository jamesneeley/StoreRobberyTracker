using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;
using System;
using static System.Windows.Forms.AxHost;

namespace StoreRobberyEnhanced.Systems
{
    internal class CameraSystem
    {
        private readonly StoreContext _ctx;

        public CameraSystem(StoreContext ctx)
        {
            _ctx = ctx;
        }

        // ------------------------------------------------------------
        // DEBUG: FORCE CAMERA ALARM
        // ------------------------------------------------------------
        public void DebugTriggerAlarm()
        {
            try
            {
                // ⭐ Suppress debug camera alarm during SafeCrack
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                {
                    DebugLogger.Info("DebugTriggerAlarm() suppressed — SafeCrack active");
                    _ctx.Ui.ShowNotification("~y~Camera alarm suppressed (SafeCrack active)");
                    return;
                }

                DebugLogger.Info("DebugTriggerAlarm() called");

                var store = _ctx.GetNearestStore();
                if (store == null)
                {
                    DebugLogger.Info("No store nearby for DebugTriggerAlarm");
                    _ctx.Ui.ShowNotification("~r~No store nearby");
                    return;
                }

                if (store.AlarmTriggered)
                {
                    DebugLogger.Info($"Store {store.Id} alarm already triggered");
                    _ctx.Ui.ShowNotification("~y~Alarm already triggered");
                    return;
                }

                DebugLogger.Info($"Triggering camera alarm for store {store.Id}");
                TriggerCameraFlag(store);

                _ctx.Ui.ShowNotification("~r~Camera alarm triggered (debug)");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CameraSystem.DebugTriggerAlarm", ex);
            }
        }

        // ------------------------------------------------------------
        // MAIN UPDATE
        // ------------------------------------------------------------
        public void UpdateStoreCameras(StoreContext ctx, TrackedStore store)
        {
            try
            {
                DebugLogger.Trace($"UpdateStoreCameras({store.Id})");

                if (!_ctx.Config.EnableCameras)
                {
                    DebugLogger.Trace("Cameras disabled via config");
                    return;
                }

                // ⭐ Suppress all camera logic during SafeCrack
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                {
                    DebugLogger.Trace($"CameraSystem suppressed — SafeCrack active for store {store.Id}");
                    return;
                }

                // ⭐ Suppress cameras during SilentRobbery mode
                if (store.SilentRobbery)
                {
                    DebugLogger.Trace($"CameraSystem suppressed — SilentRobbery flag for store {store.Id}");
                    return;
                }

                var player = Game.Player.Character;

                if (!store.IsRobberyActive)
                {
                    DebugLogger.Trace($"Store {store.Id} robbery not active → skipping camera logic");
                    return;
                }

                float dist = player.Position.DistanceTo(store.StorePos);
                if (dist > 30f)
                {
                    DebugLogger.Trace($"Player too far from store {store.Id} (dist={dist})");
                    return;
                }

                if (!store.IsPlayerInsideStore)
                {
                    DebugLogger.Trace($"Player not inside store {store.Id}");
                    return;
                }

                bool interiorFound = ProcessInteriorCameras(store, player);

                if (!interiorFound)
                {
                    DebugLogger.Trace($"No interior cameras found for store {store.Id}, using fallback cameras");
                    ProcessFallbackCameras(store, player);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CameraSystem.UpdateStoreCameras", ex);
            }
        }

        // ------------------------------------------------------------
        // INTERIOR CAMERA DETECTION (FULLY PATCHED)
        // ------------------------------------------------------------
        private bool ProcessInteriorCameras(TrackedStore store, Ped player)
        {
            try
            {
                DebugLogger.Trace($"ProcessInteriorCameras({store.Id})");

                if (!_ctx.Config.EnableCameras)
                    return false;

                // ⭐ Suppress interior cameras during SafeCrack
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return false;

                // ⭐ Suppress cameras during SilentRobbery
                if (store.SilentRobbery)
                    return false;

                if (!store.IsRobberyActive)
                    return false;

                var cams = World.GetNearbyProps(store.StorePos, 30f);
                bool foundAny = false;

                for (int i = 0; i < cams.Length; i++)
                {
                    Prop cam = cams[i];
                    if (cam == null || !cam.Exists())
                        continue;

                    if (!IsCameraModel(cam.Model.Hash))
                        continue;

                    foundAny = true;

                    // ------------------------------------------------------------
                    // ⭐ INTERIOR CAMERA DESTRUCTION → SYNC WITH FALLBACK CAMERAS
                    // ------------------------------------------------------------
                    if (PlayerDestroyedCamera(cam, player))
                    {
                        DebugLogger.Info($"Interior camera destroyed at {cam.Position} for store {store.Id}");

                        // Delete the prop
                        cam.Delete();

                        // ⭐ Mark the nearest fallback camera as destroyed
                        CameraData nearest = null;
                        float bestDist = 9999f;

                        foreach (var fcam in store.Cameras)
                        {
                            float d = fcam.Position.DistanceTo(cam.Position);
                            if (d < bestDist)
                            {
                                bestDist = d;
                                nearest = fcam;
                            }
                        }

                        if (nearest != null)
                        {
                            nearest.Destroyed = true;
                            nearest.GraceActive = false;
                            DebugLogger.Info($"Fallback camera synced destroyed for store {store.Id}");
                        }

                        continue;
                    }

                    // ------------------------------------------------------------
                    // ⭐ INTERIOR CAMERA DETECTION (WITH GRACE + CLERK REACTION)
                    // ------------------------------------------------------------
                    if (CameraSeesPlayer(cam.Position, cam.ForwardVector, player))
                    {
                        // ⭐ Clerk must have reacted before cameras can trigger alarms
                        if (!store.ClerkReacted)
                        {
                            DebugLogger.Trace($"Interior camera sees player but clerk not reacted — ignoring for store {store.Id}");
                            continue;
                        }

                        // ⭐ Start grace period on nearest fallback camera
                        CameraData nearest = null;
                        float bestDist = 9999f;

                        foreach (var fcam in store.Cameras)
                        {
                            float d = fcam.Position.DistanceTo(cam.Position);
                            if (d < bestDist)
                            {
                                bestDist = d;
                                nearest = fcam;
                            }
                        }

                        if (nearest != null)
                        {
                            if (!nearest.GraceActive)
                            {
                                nearest.GraceActive = true;
                                nearest.GraceStartUtc = DateTime.UtcNow;
                                nearest.GraceDurationSeconds = _ctx.Config.CameraGraceSeconds;

                                DebugLogger.Trace(
                                    $"Interior camera grace started for store {store.Id} (duration={nearest.GraceDurationSeconds}s)"
                                );
                            }
                            else
                            {
                                double elapsed = (DateTime.UtcNow - nearest.GraceStartUtc).TotalSeconds;

                                if (elapsed >= nearest.GraceDurationSeconds)
                                {
                                    DebugLogger.Info(
                                        $"Interior camera alarm triggered after grace for store {store.Id}"
                                    );

                                    // ⭐ ONE-SHOT
                                    nearest.GraceActive = false;
                                    nearest.Destroyed = true;

                                    TriggerCameraFlag(store);
                                }
                            }
                        }
                    }
                }

                DebugLogger.Trace($"Interior cameras found for store {store.Id}: {foundAny}");
                return foundAny;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CameraSystem.ProcessInteriorCameras", ex);
                return false;
            }
        }

        // ------------------------------------------------------------
        // CAMERA MODEL CHECK (must be above ProcessInteriorCameras)
        // ------------------------------------------------------------
        private bool IsCameraModel(int hash)
        {
            try
            {
                return hash == Function.Call<int>(Hash.GET_HASH_KEY, "prop_cctv_cam_01") ||
                       hash == Function.Call<int>(Hash.GET_HASH_KEY, "prop_cctv_cam_02") ||
                       hash == Function.Call<int>(Hash.GET_HASH_KEY, "prop_cctv_cam_03") ||
                       hash == Function.Call<int>(Hash.GET_HASH_KEY, "prop_cctv_cam_04");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CameraSystem.IsCameraModel", ex);
                return false;
            }
        }

        // ------------------------------------------------------------
        // FALLBACK CAMERAS (FULLY PATCHED)
        // ------------------------------------------------------------
        private void ProcessFallbackCameras(TrackedStore store, Ped player)
        {
            try
            {
                DebugLogger.Trace($"ProcessFallbackCameras({store.Id})");

                if (!_ctx.Config.EnableCameras)
                    return;

                // ⭐ Suppress fallback cameras during SafeCrack
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return;

                // ⭐ Suppress cameras during SilentRobbery
                if (store.SilentRobbery)
                    return;

                if (!store.IsRobberyActive)
                    return;

                int count = store.Cameras.Count;

                for (int i = 0; i < count; i++)
                {
                    CameraData cam = store.Cameras[i];

                    // ------------------------------------------------------------
                    // ⭐ DESTROYED CAMERAS ARE FULLY IGNORED
                    // ------------------------------------------------------------
                    if (cam.Destroyed)
                    {
                        DebugLogger.Trace($"Fallback camera {i} destroyed for store {store.Id}");
                        continue;
                    }

                    // ------------------------------------------------------------
                    // ⭐ ALLOW MELEE DESTRUCTION
                    // ------------------------------------------------------------
                    if (PlayerDestroyedFallbackCamera(cam, player))
                    {
                        cam.Destroyed = true;
                        cam.GraceActive = false;

                        DebugLogger.Info($"Fallback camera {i} destroyed by player for store {store.Id}");
                        continue;
                    }

                    // ------------------------------------------------------------
                    // ⭐ CAMERA DIRECTION (TOWARD CLERK)
                    // ------------------------------------------------------------
                    Vector3 camDir = (store.ClerkPos - cam.Position);
                    if (camDir.LengthSquared() < 0.001f)
                        camDir = new Vector3(0f, 1f, 0f);

                    bool seesPlayer = CameraSeesPlayer(cam.Position, camDir, player);

                    // ------------------------------------------------------------
                    // ⭐ CAMERA SEES PLAYER → START OR CONTINUE GRACE
                    // ------------------------------------------------------------
                    if (seesPlayer)
                    {
                        // ⭐ Clerk must have reacted before cameras can escalate
                        if (!store.ClerkReacted)
                        {
                            DebugLogger.Trace(
                                $"Fallback camera {i} sees player but clerk not reacted — ignoring for store {store.Id}"
                            );
                            continue;
                        }

                        // ⭐ Start grace if not active
                        if (!cam.GraceActive)
                        {
                            cam.GraceActive = true;
                            cam.GraceStartUtc = DateTime.UtcNow;
                            cam.GraceDurationSeconds = _ctx.Config.CameraGraceSeconds;

                            DebugLogger.Trace(
                                $"Fallback camera {i} grace started for store {store.Id} (duration={cam.GraceDurationSeconds}s)"
                            );
                        }
                        else
                        {
                            // ⭐ Check elapsed time
                            double elapsed = (DateTime.UtcNow - cam.GraceStartUtc).TotalSeconds;

                            if (elapsed >= cam.GraceDurationSeconds)
                            {
                                DebugLogger.Info(
                                    $"Fallback camera {i} alarm triggered after grace for store {store.Id}"
                                );

                                // ⭐ ONE-SHOT ALARM — prevent spam
                                cam.GraceActive = false;
                                cam.Destroyed = true; // treat as "triggered" camera

                                TriggerCameraFlag(store);
                            }
                        }
                    }
                    else
                    {
                        // ------------------------------------------------------------
                        // ⭐ CAMERA LOST SIGHT → RESET GRACE
                        // ------------------------------------------------------------
                        if (cam.GraceActive)
                        {
                            cam.GraceActive = false;
                            DebugLogger.Trace(
                                $"Fallback camera {i} grace reset (lost sight) for store {store.Id}"
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CameraSystem.ProcessFallbackCameras", ex);
            }
        }

        // ------------------------------------------------------------
        // CAMERA DESTRUCTION
        // ------------------------------------------------------------
        private bool PlayerDestroyedCamera(Prop cam, Ped player)
        {
            try
            {
                if (!_ctx.Config.EnableCameras)
                    return false;

                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return false;

                if (Game.IsControlJustPressed(Control.Attack) && _ctx.Player.IsArmed())
                {
                    RaycastResult ray = World.Raycast(
                        player.Position,
                        player.Position + player.ForwardVector * 20f,
                        IntersectFlags.Everything,
                        player
                    );

                    if (ray.DidHit && ray.HitEntity != null && ray.HitEntity.Handle == cam.Handle)
                    {
                        DebugLogger.Info("Interior camera destroyed by gunfire");
                        return true;
                    }
                }

                if (player.Position.DistanceTo(cam.Position) < 2f &&
                    Game.IsControlJustPressed(Control.Attack))
                {
                    DebugLogger.Info("Interior camera destroyed by melee");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CameraSystem.PlayerDestroyedCamera", ex);
                return false;
            }
        }

        private bool PlayerDestroyedFallbackCamera(CameraData cam, Ped player)
        {
            try
            {
                if (!_ctx.Config.EnableCameras)
                    return false;

                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return false;

                float dist = player.Position.DistanceTo(cam.Position);

                if (dist < 2.0f && Game.IsControlJustPressed(Control.Attack))
                {
                    DebugLogger.Info("Fallback camera destroyed by melee");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CameraSystem.PlayerDestroyedFallbackCamera", ex);
                return false;
            }
        }

        // ------------------------------------------------------------
        // CAMERA DETECTION LOGIC
        // ------------------------------------------------------------
        private bool CameraSeesPlayer(Vector3 camPos, Vector3 camDir, Ped player)
        {
            try
            {
                if (!_ctx.Config.EnableCameras)
                    return false;

                // ⭐ Suppress detection during SafeCrack
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return false;

                Vector3 toPlayer = (player.Position - camPos);
                if (toPlayer.LengthSquared() < 0.001f)
                    return false;

                toPlayer.Normalize();
                camDir.Normalize();

                float dot = Vector3.Dot(camDir, toPlayer);
                if (dot < 0.65f)
                    return false;

                RaycastResult ray = World.Raycast(
                    camPos,
                    player.Position,
                    IntersectFlags.Everything,
                    player
                );

                if (ray.DidHit && ray.HitEntity != null && ray.HitEntity.Handle != player.Handle)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CameraSystem.CameraSeesPlayer", ex);
                return false;
            }
        }

        // ------------------------------------------------------------
        // CAMERA ALARM FLAG
        // ------------------------------------------------------------
        private void TriggerCameraFlag(TrackedStore store)
        {
            try
            {
                if (!_ctx.Config.EnableCameras)
                    return;

                // ⭐ Suppress camera alarms during SafeCrack
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                {
                    DebugLogger.Trace($"Camera alarm suppressed — SafeCrack active for store {store.Id}");
                    return;
                }

                if (store.SilentRobbery)
                {
                    DebugLogger.Trace($"Camera alarm suppressed — SilentRobbery flag for store {store.Id}");
                    return;
                }

                if (!store.IsRobberyActive)
                    return;

                // ⭐ Prevent double-triggering
                if (store.AlarmTriggered)
                {
                    DebugLogger.Trace($"Camera flag ignored: store {store.Id} already alarmed");
                    return;
                }

                store.AlarmTriggered = true;
                store.HeatLevel += 1;

                DebugLogger.Info($"Camera alarm triggered for store {store.Id}, heat={store.HeatLevel}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CameraSystem.TriggerCameraFlag", ex);
            }
        }

        // ------------------------------------------------------------
        // CLEANUP
        // ------------------------------------------------------------
        public void CleanupBlipsAndProps()
        {
            DebugLogger.Trace("CleanupBlipsAndProps() called — no props to clean");
        }
    }
}
