using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyTrackerMod.Config;
using StoreRobberyTrackerMod.Data;
using StoreRobberyTrackerMod.Debug;
using StoreRobberyTrackerMod.Systems; // RobberySystem namespace
using StoreRobberyTrackerMod.UI;
using System;

namespace StoreRobberyTrackerMod.Minigame
{
    internal class SafeCrackController
    {
        private readonly SafeCrackState _state;
        private readonly SafeCrackSettings _settings;
        private readonly UiHelpers _UiHelp;
        private readonly StoreContext _ctx;
        private readonly IniConfig _config;

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
            RobberySystem robberySystem,
            StoreContext ctx,
            IniConfig config)
        {
            _state = state;
            _settings = settings;

            _logic = logic;
            _input = input;
            _ui = ui;
            _anim = anim;

            _UiHelp = uiHelp;
            _robberySystem = robberySystem;

            _ctx = ctx;          // ⭐ REQUIRED
            _config = config;    // ⭐ REQUIRED
        }

        // ------------------------------------------------------------
        // START MINIGAME (FULLY PATCHED)
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

                // ------------------------------------------------------------
                // ⭐ Cooldown check
                // ------------------------------------------------------------
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

                // ------------------------------------------------------------
                // ⭐ Reset state
                // ------------------------------------------------------------
                _state.Reset();
                _state.Active = true;
                _state.Completed = false;
                _state.Failed = false;
                _state.ConfirmRequested = false;

                _state.SafePos = safePos;
                _state.SafeHeading = safeHeading;
                _state.Player = player;
                _state.StartTime = Game.GameTime;
                _state.LastUpdateTime = Game.GameTime;

                // ------------------------------------------------------------
                // ⭐ STEALTH ROBBERY MODE ENABLED
                // ------------------------------------------------------------
                SuppressStoreSystems();   // sets SilentRobbery + suppresses clerk + cameras + police

                // ------------------------------------------------------------
                // ⭐ Save current camera mode so we can restore it later
                // ------------------------------------------------------------
                _savedCameraMode = Function.Call<int>(Hash.GET_FOLLOW_PED_CAM_VIEW_MODE);

                // ------------------------------------------------------------
                // ⭐ Teleport player if needed
                // ------------------------------------------------------------
                float dist = player.Position.DistanceTo(safePos);
                if (dist > 0.6f)
                {
                    player.Position = safePos;
                    player.Heading = safeHeading;
                }

                // Let GTA settle the ped for a few frames
                for (int i = 0; i < 10; i++)
                    Script.Yield();

                // ⭐ Force GTA to ground the ped
                Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET, player.Handle,
                    safePos.X, safePos.Y, safePos.Z, false, false, false);

                Script.Yield(); // let physics apply

                // ------------------------------------------------------------
                // ⭐ Freeze player
                // ------------------------------------------------------------
                player.Task.ClearAllImmediately();
                player.IsPositionFrozen = true;
                _playerFrozen = true;

                // ------------------------------------------------------------
                // ⭐ Initialize logic + animation
                // ------------------------------------------------------------
                _logic.Initialize(_state, _settings);
                _anim.Begin(player, safePos, safeHeading);

                // ------------------------------------------------------------
                // ⭐ Initialize UI (timer + dial)
                // ------------------------------------------------------------
                _ui.Draw(_state, _settings);

                DebugLogger.Info("[SafeCrack] Minigame started successfully");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("SafeCrackController.Start", ex);
                _state.Active = false;
            }
        }

        // ------------------------------------------------------------
        // UPDATE LOOP (FULLY PATCHED)
        // ------------------------------------------------------------
        public void Update()
        {
            if (!_state.Active)
                return;

            // ------------------------------------------------------------
            // Disable gameplay controls while minigame is active
            // ------------------------------------------------------------
            DisableGameplayControls();

            int now = Game.GameTime;
            if (now - _state.LastUpdateTime < 0)
                return;

            _state.LastUpdateTime = now;

            // ------------------------------------------------------------
            // SAFECRACK COUNTDOWN TIMER (THROTTLED + CORRECT)
            // ------------------------------------------------------------
            int elapsed = now - _state.StartTime;
            int total = _config.SafeCrackTimeSeconds * 1000;
            int remaining = (total - elapsed) / 1000;

            if (remaining < 0)
            {
                _state.Failed = true;
                return;
            }

            // Update timer text once per second
            if (now - _state.LastTimerUpdate > 1000)
            {
                _UiHelp.SetTimerText($"Safe time left: {remaining}", remaining);
                _state.LastTimerUpdate = now;
            }

            // ------------------------------------------------------------
            // ELIGIBILITY CHECK
            // ------------------------------------------------------------
            if (!_logic.ValidatePlayerStillEligible(_state))
            {
                DebugLogger.Info("[SafeCrack] Player no longer eligible, aborting");
                FailAndResetStore();
                return;
            }

            // ------------------------------------------------------------
            // INPUT + LOGIC
            // ------------------------------------------------------------
            // Preserve ConfirmRequested between frames until logic consumes it
            bool confirmHeld = _state.ConfirmRequested;
            _input.Process(_state, _settings);
            if (confirmHeld)
                _state.ConfirmRequested = true;

            _logic.Update(_state, _settings);

            // ------------------------------------------------------------
            // DRAW UI EACH FRAME
            // ------------------------------------------------------------
            _ui.Draw(_state, _settings);

            // ------------------------------------------------------------
            // COMPLETION / FAILURE
            // ------------------------------------------------------------
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
        // SUCCESS HANDLER (FULLY PATCHED)
        // ------------------------------------------------------------
        private void FinishSuccess()
        {
            // Guard: only run once
            if (!_state.Active)
                return;

            _state.Completed = true;
            _state.Active = false;

            _anim.EndSuccess(_state.Player);

            int payout = _logic.CalculatePayout(_settings);
            _state.Payout = payout;

            // ⭐ Success vibration
            Function.Call(Hash.SET_CONTROL_SHAKE, 0, 250, 200);

            // ⭐ Play success sound (no payout reveal)
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");

            // ⭐ Subtitle without payout
            _UiHelp.ShowSubtitle("~g~Safe cracked!", 3000);

            // Fire event
            SafeCrackEvents.OnSafeCracked(_state.SafePos, payout);

            // ⭐ Apply multiplier + add to robbery total
            if (_store != null)
            {
                _store.SafeCracked = true;

                payout = (int)(payout * _ctx.Config.PayoutMultiplier);
                _store.PendingPayout += payout;

                // ⭐ Exit stealth mode cleanly
                _store.SilentRobbery = false;
                _store.AlarmTriggered = false;
                _store.ClerkCallingPolice = false;
                _store.SilentAlarmPressed = false;
            }

            // Cooldown
            _state.CooldownActive = true;
            _state.CooldownEndTime = Game.GameTime + _settings.CooldownMs;

            Stop(true);
        }

        // ------------------------------------------------------------
        // FAILURE HANDLER (FULLY PATCHED)
        // ------------------------------------------------------------
        private void FailAndResetStore()
        {
            Stop(false);

            if (_store != null)
            {
                DebugLogger.Info("[SafeCrack] Resetting store after failure");
                _store.SilentRobbery = false;
                _robberySystem.DebugResetStore(_store);
            }
        }

        // ------------------------------------------------------------
        // STOP MINIGAME (FULLY PATCHED)
        // ------------------------------------------------------------
        public void Stop(bool success)
        {
            _state.Active = false;
            _state.ConfirmRequested = false;

            // Prevent fail message during game load or initialization
            if (Game.IsLoading)
                success = true;

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

            // Always restore controls
            RestoreControls();

            // Clear stealth flag on store
            if (_store != null)
            {
                _store.SilentRobbery = false;
            }

            // Only show fail message if player actually stopped mid‑game
            if (!success && !_state.Completed)
            {
                Function.Call(Hash.SET_CONTROL_SHAKE, 0, 300, 255);
                _UiHelp.ShowSubtitle("~r~You stopped cracking the safe. Try again.", 3000);
            }
        }

        // ------------------------------------------------------------
        // ABORT (FULLY PATCHED)
        // ------------------------------------------------------------
        public void Abort()
        {
            if (!_state.Active)
                return;

            Stop(false);

            if (_store != null)
            {
                _store.SilentRobbery = false;
                _robberySystem.DebugResetStore(_store);
            }
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
        // STORE SUPPRESSION (STEALTH MODE) (FULLY PATCHED)
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

            DebugLogger.Info("[SafeCrack] Store systems suppressed for stealth safecrack");
        }
    }
}
