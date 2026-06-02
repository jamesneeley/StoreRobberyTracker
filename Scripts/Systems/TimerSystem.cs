using System;
using System.Collections.Generic;
using GTA;

namespace StoreRobberyTrackerMod.Systems
{
    internal class TimerSystem
    {
        private readonly List<ScheduledAction> _actions = new List<ScheduledAction>();

        public void Schedule(int triggerTime, Action action)
        {
            _actions.Add(new ScheduledAction(triggerTime, action));
        }

        public void Update()
        {
            int currentTime = Game.GameTime;
            for (int i = _actions.Count - 1; i >= 0; i--)
            {
                if (currentTime >= _actions[i].TriggerTime)
                {
                    try
                    {
                        _actions[i].Action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.DebugLogger.LogException("TimerScheduler.Update", ex);
                    }
                    _actions.RemoveAt(i);
                }
            }
        }

        private class ScheduledAction
        {
            public int TriggerTime { get; }
            public Action Action { get; }

            public ScheduledAction(int triggerTime, Action action)
            {
                TriggerTime = triggerTime;
                Action = action;
            }
        }
    }
}
