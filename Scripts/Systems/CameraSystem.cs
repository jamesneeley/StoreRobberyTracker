using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyTrackerMod.Data;
using StoreRobberyTrackerMod.Debug;
using System;
using static System.Windows.Forms.AxHost;

namespace StoreRobberyTrackerMod.Systems
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
        // INTERIOR CAMERA DETECTION
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

                    if (PlayerDestroyedCamera(cam, player))
                    {
                        DebugLogger.Info($"Interior camera destroyed at {cam.Position} for store {store.Id}");
                        cam.Delete();
                        continue;
                    }

                    if (CameraSeesPlayer(cam.Position, cam.ForwardVector, player))
                    {
                        DebugLogger.Info($"Interior camera detected player for store {store.Id}");
                        TriggerCameraFlag(store);
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
        // FALLBACK CAMERAS
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

                if (store.SilentRobbery)
                    return;

                if (!store.IsRobberyActive)
                    return;

                int count = store.Cameras.Count;

                for (int i = 0; i < count; i++)
                {
                    CameraData cam = store.Cameras[i];

                    if (cam.Destroyed)
                    {
                        DebugLogger.Trace($"Fallback camera {i} already destroyed for store {store.Id}");
                        continue;
                    }

                    if (PlayerDestroyedFallbackCamera(cam, player))
                    {
                        cam.Destroyed = true;
                        DebugLogger.Info($"Fallback camera {i} destroyed by player for store {store.Id}");
                        continue;
                    }

                    if (cam.GraceActive)
                    {
                        double elapsed = (DateTime.UtcNow - cam.GraceStartUtc).TotalSeconds;
                        if (elapsed >= _ctx.Config.CameraGraceSeconds)
                        {
                            cam.GraceActive = false;
                            DebugLogger.Trace($"Fallback camera {i} grace period ended for store {store.Id}");
                        }
                    }

                    Vector3 camDir = (store.ClerkPos - cam.Position);
                    if (camDir.LengthSquared() < 0.001f)
                        camDir = new Vector3(0f, 1f, 0f);

                    if (CameraSeesPlayer(cam.Position, camDir, player))
                    {
                        if (!cam.GraceActive)
                        {
                            cam.GraceActive = true;
                            cam.GraceStartUtc = DateTime.UtcNow;

                            DebugLogger.Trace($"Fallback camera {i} grace started for store {store.Id}");
                        }
                        else
                        {
                            DebugLogger.Info($"Fallback camera {i} detected player for store {store.Id}");
                            TriggerCameraFlag(store);
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
