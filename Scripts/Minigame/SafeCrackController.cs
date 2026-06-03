using System;
using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyTrackerMod.Config;
using StoreRobberyTrackerMod.Debug;
using StoreRobberyTrackerMod.UI;
using StoreRobberyTrackerMod.Data;
using StoreRobberyTrackerMod.Systems; // RobberySystem namespace

namespace StoreRobberyTrackerMod.Minigame
{
    internal class SafeCrackController
    {
        private readonly SafeCrackState _state;
        private readonly SafeCrackSettings _settings;
        private readonly UiHelpers _UiHelp;

        private readonly ISafeCrackLogic _logic;
        private readonly ISafeCrackInput _input;
        private readonly ISafeCrackUI _ui;
        private readonly ISafeCrackAnimation _anim;

        private readonly RobberySystem _robberySystem;
        private TrackedStore _store;

        private bool _playerFrozen;
        private int _savedCameraMode = -1;

        // ⭐ REQUIRED BY RobberySystem
        public bool IsRunning => _state.Active;

        public SafeCrackController(
            SafeCrackState state,
            SafeCrackSettings settings,
            ISafeCrackLogic logic,
            ISafeCrackInput input,
            ISafeCrackUI ui,
            ISafeCrackAnimation anim,
            UiHelpers uiHelp,
            RobberySystem robberySystem)
        {
            _state = state;
            _settings = settings;

            _logic = logic;
            _input = input;
            _ui = ui;
            _anim = anim;

            _UiHelp = uiHelp;
            _robberySystem = robberySystem;
        }

        // ------------------------------------------------------------
        // START MINIGAME
        // ------------------------------------------------------------
        public void Start(TrackedStore store, Vector3 safePos, float safeHeading, Ped player)
        {
            try
            {
                // 🔒 HARD GUARD: If already running, ignore ALL further Start() calls
                if (_state.Active)
                {
                    DebugLogger.Info("[SafeCrack] Start() ignored — already active");
                    return;
                }

                _store = store;

                DebugLogger.Info("[SafeCrack] Start() called");

                // Cooldown check
                if (_state.CooldownActive)
                {
                    int now = Game.GameTime;
                    if (now < _state.CooldownEndTime)
                    {
                        DebugLogger.Info("[SafeCrack] Start() blocked — cooldown active");
                        return;
                    }

                    _state.CooldownActive = false;
                }

                // Reset state
                _state.Reset();
                _state.Active = true;
                _state.Completed = false;
                _state.Failed = false;
                _state.ConfirmRequested = false;

                _state.SafePos = safePos;
                _state.SafeHeading = safeHeading;
                _state.Player = player;
                _state.LastUpdateTime = Game.GameTime;

                // ⭐ STEALTH ROBBERY MODE ENABLED
                SuppressStoreSystems();

                // Save current camera mode so we can restore it later
                _savedCameraMode = Function.Call<int>(Hash.GET_FOLLOW_PED_CAM_VIEW_MODE);

                // Teleport if needed
                float dist = player.Position.DistanceTo(safePos);
                if (dist > 0.6f)
                {
                    player.Position = safePos;
                    player.Heading = safeHeading;
                }

                // Let GTA settle the ped for a few frames
                for (int i = 0; i < 10; i++)
                {
                    Script.Yield();
                }

                // ⭐ Force GTA to ground the ped
                Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET, player.Handle,
                    safePos.X, safePos.Y, safePos.Z, false, false, false);

                // Yield again to let physics apply
                Script.Yield();

                // ⭐ NOW freeze
                player.Task.ClearAllImmediately();
                player.IsPositionFrozen = true;
                _playerFrozen = true;

                // Initialize logic + animation
                _logic.Initialize(_state, _settings);
                _anim.Begin(player, safePos, safeHeading);

                DebugLogger.Info("SafeCrack minigame started");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("SafeCrackController.Start", ex);
                _state.Active = false;
            }
        }

        // ------------------------------------------------------------
        // UPDATE LOOP
        // ------------------------------------------------------------
        public void Update()
        {
            if (!_state.Active)
                return;

            DisableGameplayControls();

            int now = Game.GameTime;
            if (now - _state.LastUpdateTime < 0)
                return;

            _state.LastUpdateTime = now;

            // Eligibility
            if (!_logic.ValidatePlayerStillEligible(_state))
            {
                DebugLogger.Info("[SafeCrack] Player no longer eligible, aborting");
                FailAndResetStore();
                return;
            }

            _input.Process(_state, _settings);
            _logic.Update(_state, _settings);

            if (_state.Completed)
            {
                FinishSuccess();
                return;
            }

            if (_state.Failed)
            {
                FailAndResetStore();
                return;
            }
        }

        // ------------------------------------------------------------
        // SUCCESS HANDLER
        // ------------------------------------------------------------
        private void FinishSuccess()
        {
            _state.Completed = false;
            _state.Active = false;

            _anim.EndSuccess(_state.Player);

            int payout = _logic.CalculatePayout(_settings);
            _state.Payout = payout;

            Function.Call(Hash.SET_CONTROL_SHAKE, 0, 250, 200);

            // Fire event
            SafeCrackEvents.OnSafeCracked(_state.SafePos, payout);

            _UiHelp.ShowSubtitle($"~g~Safe cracked! Loot: ${payout}", 4000);

            // ⭐ Mark store safe as cracked and add payout into robbery total
            if (_store != null)
            {
                _store.SafeCracked = true;
                _store.PendingPayout += payout;
            }

            // Cooldown
            _state.CooldownActive = true;
            _state.CooldownEndTime = Game.GameTime + _settings.CooldownMs;

            Stop(true);
        }

        // ------------------------------------------------------------
        // FAILURE HANDLER
        // ------------------------------------------------------------
        private void FailAndResetStore()
        {
            Stop(false);

            if (_store != null)
            {
                DebugLogger.Info("[SafeCrack] Resetting store after failure");
                _robberySystem.DebugResetStore(_store);
            }
        }

        // ------------------------------------------------------------
        // STOP MINIGAME
        // ------------------------------------------------------------
        public void Stop(bool success)
        {
            _state.Active = false;
            _state.ConfirmRequested = false;

            // Clear any running tasks/animations
            if (_state.Player != null && _state.Player.Exists())
            {
                _state.Player.Task.ClearAll();
            }

            if (_playerFrozen && _state.Player != null && _state.Player.Exists())
            {
                _state.Player.IsPositionFrozen = false;
                _playerFrozen = false;
            }

            // Restore camera mode if we changed it
            if (_savedCameraMode != -1)
            {
                Function.Call(Hash.SET_FOLLOW_PED_CAM_VIEW_MODE, _savedCameraMode);
                _savedCameraMode = -1;
            }

            Function.Call(Hash.STOP_CONTROL_SHAKE, 0);

            _anim.End(_state.Player, success);
            _ui.Clear();

            if (!success)
            {
                Function.Call(Hash.SET_CONTROL_SHAKE, 0, 300, 255);
                _UiHelp.ShowSubtitle("~r~You stopped cracking the safe. Try again.", 3000);
            }

            RestoreControls();
        }

        // ------------------------------------------------------------
        // ABORT
        // ------------------------------------------------------------
        public void Abort()
        {
            if (!_state.Active)
                return;

            Stop(false);

            if (_store != null)
                _robberySystem.DebugResetStore(_store);
        }

        // ------------------------------------------------------------
        // CONTROL HELPERS
        // ------------------------------------------------------------
        private void DisableGameplayControls()
        {
            // Disable combat + actions
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.Attack);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.Aim);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.Reload);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.MeleeAttack1);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.MeleeAttack2);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.VehicleAccelerate);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.VehicleBrake);

            // ⭐ DO NOT disable Jump or Sprint — they kill the A button
            // ⭐ DO NOT disable MoveUpDown or MoveLeftRight — they kill left stick rotation

            // Allow camera look
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.LookLeftRight);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.LookUpDown);
        }


        private void RestoreControls()
        {
            Function.Call(Hash.ENABLE_ALL_CONTROL_ACTIONS, 0);

            // Restore weapon wheel, phone, pause menu, radio, cover
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.SelectWeapon);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.CharacterWheel);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.Phone);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.FrontendPause);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.RadioWheelLeftRight);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.RadioWheelUpDown);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.Cover);
        }

        // ------------------------------------------------------------
        // STORE SUPPRESSION (STEALTH MODE)
        // ------------------------------------------------------------
        private void SuppressStoreSystems()
        {
            if (_store == null)
                return;

            _store.IsRobberyActive = true;
            _store.SilentRobbery = true;

            _store.AlarmTriggered = false;
            _store.ClerkCallingPolice = false;
            _store.SilentAlarmPressed = false;

            _store.HeatLevel = 0;
            _store.RepeatRobberyEscalationApplied = false;
            _store.MaskEscalationApplied = false;
            _store.FightEscalationApplied = false;
            _store.TimeEscalationApplied = false;

            // Disable cameras
            foreach (var cam in _store.Cameras)
            {
                cam.GraceActive = true;
                cam.GraceStartUtc = DateTime.UtcNow;
            }

            DebugLogger.Info("[SafeCrack] Store systems suppressed for stealth safecrack");
        }
    }
}
