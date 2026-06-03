using System;
using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyTrackerMod.Config;
using StoreRobberyTrackerMod.Debug;
using StoreRobberyTrackerMod.UI;

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

        private bool _playerFrozen;

        // ⭐ REQUIRED BY RobberySystem
        public bool IsRunning => _state.Active;

        public SafeCrackController(
            SafeCrackState state,
            SafeCrackSettings settings,
            ISafeCrackLogic logic,
            ISafeCrackInput input,
            ISafeCrackUI ui,
            ISafeCrackAnimation anim,
            UiHelpers uiHelp)          // ⭐ ADD THIS
        {
            _state = state;
            _settings = settings;

            _logic = logic;
            _input = input;
            _ui = ui;
            _anim = anim;

            _UiHelp = uiHelp;          // ⭐ AND THIS
        }

        // ------------------------------------------------------------
        // START MINIGAME (FINAL PATCHED VERSION)
        // ------------------------------------------------------------
        public void Start(Vector3 safePos, float safeHeading, Ped player)
        {
            try
            {
                // 🔒 HARD GUARD: If already running, ignore ALL further Start() calls
                if (_state.Active)
                {
                    DebugLogger.Info("[SafeCrack] Start() ignored — already active");
                    return;
                }

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

                // Reset state and arm minigame
                _state.Reset();
                _state.Active = true;
                _state.Completed = false;
                _state.Failed = false;
                _state.ConfirmRequested = false;

                _state.SafePos = safePos;
                _state.SafeHeading = safeHeading;
                _state.Player = player;
                _state.LastUpdateTime = Game.GameTime;

                // ⭐ TELEPORT ONLY IF PLAYER IS NOT ALREADY AT THE SAFE
                float dist = player.Position.DistanceTo(safePos);
                if (dist > 0.6f)
                {
                    DebugLogger.Info($"[SafeCrack] Teleporting player to safe (dist={dist:0.00})");
                    player.Position = safePos;
                    player.Heading = safeHeading;
                }
                else
                {
                    DebugLogger.Info($"[SafeCrack] Skipping teleport — already near safe (dist={dist:0.00})");
                }

                // ⭐ HARD FREEZE PLAYER DURING MINIGAME
                if (player != null && player.Exists())
                {
                    player.IsPositionFrozen = true;
                    _playerFrozen = true;
                }

                DebugLogger.Info($"[SafeCrack] State armed at X:{safePos.X:F2} Y:{safePos.Y:F2} Z:{safePos.Z:F2}, heading={safeHeading:F2}");

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

            // 🔒 Disable controls every frame while active
            Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 0);
            // Optional: allow camera look
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.LookLeftRight);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.LookUpDown);

            int now = Game.GameTime;
            if (now - _state.LastUpdateTime < 0)
                return;

            _state.LastUpdateTime = now;

            if (!_logic.ValidatePlayerStillEligible(_state))
            {
                DebugLogger.Info("[SafeCrack] Player no longer eligible, aborting");
                Stop(false);
                return;
            }

            _input.Process(_state, _settings);
            _logic.Update(_state, _settings);

            if (_state.Completed)
            {
                DebugLogger.Info("[SafeCrack] Completed, finishing success");
                FinishSuccess();
                return;
            }

            if (_state.Failed)
            {
                DebugLogger.Info("[SafeCrack] Failed flag set, stopping");
                Stop(false);
                return;
            }
        }

        // ------------------------------------------------------------
        // SUCCESS HANDLER
        // ------------------------------------------------------------
        private void FinishSuccess()
        {
            // ⭐ Prevent repeated success loops
            _state.Completed = false;
            _state.Active = false;

            _anim.EndSuccess(_state.Player);

            int payout = _logic.CalculatePayout(_settings);
            _state.Payout = payout;

            // ⭐ Fire event ONCE
            SafeCrackEvents.OnSafeCracked(_state.SafePos, payout);

            // ⭐ Player feedback (debug-friendly, feels like real flow)
            _UiHelp.ShowSubtitle($"~g~Safe cracked! Loot: ${payout}", 4000);

            // ⭐ Start cooldown AFTER success
            _state.CooldownActive = true;
            _state.CooldownEndTime = Game.GameTime + _settings.CooldownMs;

            // ⭐ Stop minigame cleanly
            Stop(true);
        }

        // ------------------------------------------------------------
        // STOP MINIGAME
        // ------------------------------------------------------------
        public void Stop(bool success)
        {
            // If already stopped, ignore
            //if (!_state.Active)
            //    return;

            // Fully deactivate
            _state.Active = false;
            _state.ConfirmRequested = false;

            // Unfreeze player
            if (_playerFrozen && _state.Player != null && _state.Player.Exists())
            {
                _state.Player.IsPositionFrozen = false;
                _playerFrozen = false;
            }

            _anim.End(_state.Player, success);
            _ui.Clear();

            if (!success)
            {
                // ⭐ Failure feedback + retry
                _UiHelp.ShowSubtitle("~r~You stopped cracking the safe. Try again.", 3000);

                // ⭐ Reset state so the player can retry immediately
                _state.Failed = false;
                _state.Completed = false;
                _state.CooldownActive = false;
            }
        }

        // ------------------------------------------------------------
        // OPTIONAL: FORCE ABORT
        // ------------------------------------------------------------
        public void Abort()
        {
            if (!_state.Active)
                return;

            _state.Active = false;
            _state.ConfirmRequested = false;

            // Unfreeze player
            if (_playerFrozen && _state.Player != null && _state.Player.Exists())
            {
                _state.Player.IsPositionFrozen = false;
                _playerFrozen = false;
            }

            _anim.End(_state.Player, false);
            _ui.Clear();
        }
    }
}
