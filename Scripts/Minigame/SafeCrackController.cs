using GTA;
using GTA.Math;
using StoreRobberyTrackerMod.Config;

namespace StoreRobberyTrackerMod.Minigame
{
    internal class SafeCrackController
    {
        private readonly SafeCrackState _state;
        private readonly SafeCrackSettings _settings;

        private readonly ISafeCrackLogic _logic;
        private readonly ISafeCrackInput _input;
        private readonly ISafeCrackUI _ui;
        private readonly ISafeCrackAnimation _anim;

        // ⭐ REQUIRED BY RobberySystem
        public bool IsRunning => _state.Active;

        public SafeCrackController(
            SafeCrackState state,
            SafeCrackSettings settings,
            ISafeCrackLogic logic,
            ISafeCrackInput input,
            ISafeCrackUI ui,
            ISafeCrackAnimation anim)
        {
            _state = state;
            _settings = settings;

            _logic = logic;
            _input = input;
            _ui = ui;
            _anim = anim;
        }

        // ------------------------------------------------------------
        // START MINIGAME
        // ------------------------------------------------------------
        public void Start(Vector3 safePos, float safeHeading, Ped player)
        {
            if (_state.Active)
                return;

            // Cooldown check
            if (_state.CooldownActive)
            {
                int now = Game.GameTime;
                if (now < _state.CooldownEndTime)
                    return;

                _state.CooldownActive = false;
            }

            _state.Reset();
            _state.Active = true;
            _state.ConfirmRequested = false;

            _state.SafePos = safePos;
            _state.SafeHeading = safeHeading;
            _state.Player = player;

            _logic.Initialize(_state, _settings);
            _anim.Begin(player, safePos, safeHeading);
        }

        // ------------------------------------------------------------
        // UPDATE LOOP
        // ------------------------------------------------------------
        public void Update()
        {
            if (!_state.Active)
                return;

            int now = Game.GameTime;
            if (now - _state.LastUpdateTime < 0)
                return;

            _state.LastUpdateTime = now;

            if (!_logic.ValidatePlayerStillEligible(_state))
            {
                Stop(false);
                return;
            }

            _input.Process(_state, _settings);
            _logic.Update(_state, _settings);

            // UI draw happens here (only when active)
            _ui.Draw(_state, _settings);

            if (_state.Completed)
            {
                FinishSuccess();
                return;
            }

            if (_state.Failed)
            {
                Stop(false);
                return;
            }
        }

        // ------------------------------------------------------------
        // SUCCESS HANDLER
        // ------------------------------------------------------------
        private void FinishSuccess()
        {
            _anim.EndSuccess(_state.Player);

            int payout = _logic.CalculatePayout(_settings);
            _state.Payout = payout;

            SafeCrackEvents.OnSafeCracked(_state.SafePos, payout);

            _state.CooldownActive = true;
            _state.CooldownEndTime = Game.GameTime + _settings.CooldownMs;

            Stop(true);
        }

        // ------------------------------------------------------------
        // STOP MINIGAME
        // ------------------------------------------------------------
        public void Stop(bool success)
        {
            if (!_state.Active)
                return;

            _state.Active = false;

            _state.Active = false;
            _state.ConfirmRequested = false;

            _anim.End(_state.Player, success);
            _ui.Clear();
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

            _anim.End(_state.Player, false);
            _ui.Clear();
        }
    }
}
