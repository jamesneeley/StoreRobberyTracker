using GTA;
using System;
using System.Collections.Generic;

namespace StoreRobberyEnhanced.Debug
{
    internal static class DebugProfiler
    {
        private class ProfileEntry
        {
            public long StartTime;
            public long LastDuration;
            public long Min = long.MaxValue;
            public long Max = 0;
            public long Total = 0;
            public int Count = 0;
        }

        private static readonly Dictionary<string, ProfileEntry> _profiles = new Dictionary<string, ProfileEntry>();

        // ⭐ NEW — global enable/disable flag
        private static bool _enabled = true;
        private static bool _initialized = false;

        // ------------------------------------------------------------
        // INITIALIZE
        // ------------------------------------------------------------
        internal static void Initialize(bool enableProfiler)
        {
            try
            {
                _enabled = enableProfiler;
                _initialized = true;

                if (_enabled)
                    DebugLogger.Info("DebugProfiler initialized.");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("DebugProfiler.Initialize", ex);
            }
        }

        // ------------------------------------------------------------
        // PUBLIC API
        // ------------------------------------------------------------
        internal static void Begin(string name)
        {
            try
            {
                if (!_initialized || !_enabled)
                    return;

                if (!_profiles.ContainsKey(name))
                    _profiles[name] = new ProfileEntry();

                _profiles[name].StartTime = DateTime.UtcNow.Ticks;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("DebugProfiler.Begin", ex);
            }
        }

        internal static void End(string name)
        {
            try
            {
                if (!_initialized || !_enabled)
                    return;

                if (!_profiles.ContainsKey(name))
                    return;

                long now = DateTime.UtcNow.Ticks;
                ProfileEntry entry = _profiles[name];

                long duration = now - entry.StartTime;
                entry.LastDuration = duration;

                entry.Min = Math.Min(entry.Min, duration);
                entry.Max = Math.Max(entry.Max, duration);

                entry.Total += duration;
                entry.Count++;

                DebugLogger.Trace($"Profiler [{name}] = {duration / 10000f:0.000} ms");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("DebugProfiler.End", ex);
            }
        }

        internal static string GetReport()
        {
            try
            {
                if (!_initialized || !_enabled)
                    return "Profiler disabled.";

                if (_profiles.Count == 0)
                    return "No profiler data.";

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("~y~DEBUG PROFILER~s~");

                foreach (var kv in _profiles)
                {
                    string name = kv.Key;
                    ProfileEntry e = kv.Value;

                    float last = e.LastDuration / 10000f;
                    float avg = (e.Total / (float)e.Count) / 10000f;
                    float min = e.Min / 10000f;
                    float max = e.Max / 10000f;

                    sb.AppendLine($"{name}: ~c~Last {last:0.000} ms | Avg {avg:0.000} ms | Min {min:0.000} | Max {max:0.000}");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("DebugProfiler.GetReport", ex);
                return "Profiler error.";
            }
        }

        internal static void DumpToFile()
        {
            try
            {
                if (!_initialized || !_enabled)
                    return;

                string report = GetReport();
                DebugFileManager.WriteText($"Profiler_{DateTime.Now:HH-mm-ss}.txt", report);
                DebugLogger.Info("Profiler report dumped to file.");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("DebugProfiler.DumpToFile", ex);
            }
        }
    }
}
