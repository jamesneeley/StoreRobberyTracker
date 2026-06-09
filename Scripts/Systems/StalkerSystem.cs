using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;

namespace StoreRobberyEnhanced.Systems
{
    internal class StalkerSystem
    {
        private readonly StoreContext _ctx;
        private readonly Random _rng;

        private List<string> _robberyMsgs;
        private List<string> _escapeMsgs;
        private List<string> _knockoutMsgs;
        private List<string> _gunKillMsgs;
        private List<string> _meleeKillMsgs;
        private List<string> _callAnsweredMsgs;
        private List<string> _callIgnoredMsgs;

        private Queue<StalkerEvent> _eventQueue;

        private bool _callActive;
        private bool _callAnswered;
        private int _callEndTime;

        private string _callerImage;
        private string _callerName;

        private int _messagesSentThisRobbery = 0;
        private DateTime _nextAllowedMessageTime = DateTime.MinValue;

        public StalkerSystem(StoreContext ctx)
        {
            try
            {
                _ctx = ctx;
                _rng = new Random();
                _eventQueue = new Queue<StalkerEvent>();

                DebugLogger.Info("StalkerSystem initialized");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.ctor", ex);
            }
        }

        // ------------------------------------------------------------
        // DEBUG FORCE MESSAGE
        // ------------------------------------------------------------
        public void DebugForceStalker()
        {
            try
            {
                DebugLogger.Info("DebugForceStalker() called");

                List<string> pool = _robberyMsgs;

                if (pool == null || pool.Count == 0)
                {
                    DebugLogger.Info("No stalker messages loaded");
                    _ctx.Ui.ShowNotification("~r~No stalker messages loaded");
                    return;
                }

                string msg = pool[_rng.Next(pool.Count)];

                _ctx.Ui.TextNotification(
                    _callerImage,
                    _callerName,
                    "UNKNOWN NUMBER",
                    msg
                );

                DebugLogger.Info("Forced stalker message sent");
                _ctx.Ui.ShowNotification("~r~Stalker message forced (debug)");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.DebugForceStalker", ex);
            }
        }

        // ------------------------------------------------------------
        // LOAD FROM INI
        // ------------------------------------------------------------
        public void LoadFromIni()
        {
            try
            {
                DebugLogger.Info("Loading stalker messages from INI");

                IniConfig ini = _ctx.Config;

                _callerImage = ini.StalkerCallerImage;
                _callerName = ini.StalkerCallerName;

                _robberyMsgs = ini.StalkerRobberyMsgs;
                _escapeMsgs = ini.StalkerEscapeMsgs;
                _knockoutMsgs = ini.StalkerKnockoutMsgs;
                _gunKillMsgs = ini.StalkerGunKillMsgs;
                _meleeKillMsgs = ini.StalkerMeleeKillMsgs;
                _callAnsweredMsgs = ini.StalkerCallAnsweredMsgs;
                _callIgnoredMsgs = ini.StalkerCallIgnoredMsgs;

                DebugLogger.Info("Stalker messages loaded");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.LoadFromIni", ex);
            }
        }

        // ------------------------------------------------------------
        // MESSAGE QUEUEING
        // ------------------------------------------------------------
        private void QueueMessage(List<string> pool)
        {
            try
            {
                if (!_ctx.AnyRobberyActive)
                    return;

                if (!_ctx.Config.EnableStalkerMsg)
                    return;

                if (pool == null || pool.Count == 0)
                    return;

                if (_messagesSentThisRobbery >= _ctx.Config.MaxMessagesPerRobbery)
                    return;

                if (DateTime.UtcNow < _nextAllowedMessageTime)
                    return;

                int delay = _rng.Next(5000, 10000);

                StalkerEvent evt = new StalkerEvent
                {
                    TriggerTime = Game.GameTime + delay,
                    Pool = pool
                };

                _eventQueue.Enqueue(evt);

                DebugLogger.Trace($"Queued stalker message (delay={delay}ms)");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.QueueMessage", ex);
            }
        }

        public void QueueRobberyMessage() { QueueMessage(_robberyMsgs); }
        public void QueueEscapeMessage() { QueueMessage(_escapeMsgs); }
        public void QueueKnockoutMessage() { QueueMessage(_knockoutMsgs); }
        public void QueueGunKillMessage() { QueueMessage(_gunKillMsgs); }
        public void QueueMeleeKillMessage() { QueueMessage(_meleeKillMsgs); }

        // ------------------------------------------------------------
        // PROCESS QUEUED EVENTS
        // ------------------------------------------------------------
        public void ProcessEvents()
        {
            try
            {
                var player = Game.Player.Character;

                if (!_ctx.AnyRobberyActive)
                {
                    DebugLogger.Trace("No robbery active — clearing stalker queue");
                    _eventQueue.Clear();
                    return;
                }

                if (player.IsDead || Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, Game.Player))
                {
                    DebugLogger.Trace("Player dead/arrested — clearing stalker queue");
                    _eventQueue.Clear();
                    return;
                }

                if (_ctx.AnyRobberyActive && player.IsDead)
                {
                    DebugLogger.Info("Player died during robbery — sending death message");
                    SendRandomMessage(_meleeKillMsgs);
                    _messagesSentThisRobbery = _ctx.Config.MaxMessagesPerRobbery;
                    return;
                }

                if (_callActive)
                    return;

                if (_eventQueue.Count == 0)
                    return;

                StalkerEvent evt = _eventQueue.Peek();

                if (Game.GameTime >= evt.TriggerTime)
                {
                    DebugLogger.Trace("Processing queued stalker message");
                    SendRandomMessage(evt.Pool);
                    _eventQueue.Dequeue();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.ProcessEvents", ex);
            }
        }

        private void SendRandomMessage(List<string> pool)
        {
            try
            {
                if (pool == null || pool.Count == 0)
                    return;

                if (_messagesSentThisRobbery >= _ctx.Config.MaxMessagesPerRobbery)
                    return;

                if (DateTime.UtcNow < _nextAllowedMessageTime)
                    return;

                string msg = pool[_rng.Next(pool.Count)];

                DebugLogger.Info($"Sending stalker message: {msg}");

                _ctx.Ui.TextNotification(
                    _callerImage,
                    _callerName,
                    "NO CALLER ID",
                    msg
                );

                _messagesSentThisRobbery++;
                _nextAllowedMessageTime = DateTime.UtcNow.AddSeconds(_ctx.Config.MessageCooldownSeconds);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.SendRandomMessage", ex);
            }
        }

        // ------------------------------------------------------------
        // CALL SYSTEM
        // ------------------------------------------------------------
        public void TryTriggerCall()
        {
            try
            {
                if (!_ctx.AnyRobberyActive)
                    return;

                if (!_ctx.Config.EnableStalkerCall)
                    return;

                if (_callActive)
                    return;

                int chance = _ctx.Config.StalkerCallChance;

                if (_rng.Next(0, 100) < chance)
                {
                    DebugLogger.Info("Stalker call triggered");
                    StartCall();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.TryTriggerCall", ex);
            }
        }

        private void StartCall()
        {
            try
            {
                _callActive = true;
                _callAnswered = false;
                _callEndTime = Game.GameTime + 8000;

                DebugLogger.Info("Starting stalker call");

                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "Phone_Ring", "DLC_HEIST_HACKING_SOUNDS");

                _ctx.Ui.TextNotification(
                    _callerImage,
                    _callerName,
                    "Incoming Call",
                    "~c~Unknown Caller"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.StartCall", ex);
            }
        }

        // ------------------------------------------------------------
        // CALL STATE UPDATE
        // ------------------------------------------------------------
        public void UpdateCallState()
        {
            try
            {
                if (!_callActive)
                    return;

                if (!_ctx.AnyRobberyActive)
                {
                    DebugLogger.Trace("Call cancelled — robbery ended");
                    _callActive = false;
                    return;
                }

                if (Game.IsControlJustPressed(Control.Context))
                {
                    DebugLogger.Info("Stalker call answered");
                    _callAnswered = true;
                    _callActive = false;

                    QueueMessage(_callAnsweredMsgs);
                    return;
                }

                if (Game.GameTime >= _callEndTime)
                {
                    if (!_callAnswered)
                    {
                        DebugLogger.Info("Stalker call ignored");
                        QueueMessage(_callIgnoredMsgs);
                    }

                    _callActive = false;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.UpdateCallState", ex);
            }
        }

        // ------------------------------------------------------------
        // RESET ON ROBBERY END
        // ------------------------------------------------------------
        public void ResetForNewRobbery()
        {
            try
            {
                DebugLogger.Info("Resetting stalker system for new robbery");

                _messagesSentThisRobbery = 0;
                _nextAllowedMessageTime = DateTime.MinValue;
                _eventQueue.Clear();
                _callActive = false;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.ResetForNewRobbery", ex);
            }
        }
    }
}
