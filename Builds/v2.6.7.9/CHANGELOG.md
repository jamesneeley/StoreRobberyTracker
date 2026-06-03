Store Robbery Enhanced — Version v2.3.7.9
Release Date: June 2026

🚀 Overview
v2.3.7.9 is a major stability and integration update that finalizes the SafeCrack minigame and synchronizes it with every core system: Robbery, Police, Cameras, UI, Controls, and Animations.
This is the most stable and polished version of the mod to date.

---

🧩 SafeCrack System
- Added full stealth‑mode suppression for store systems during SafeCrack
- Added payout merging into robbery total (PendingPayout += safePayout)
- Added camera mode save/restore (fixes first‑person lock bug)
- Added full control restoration (weapon wheel, phone, pause, radio, cover)
- Added animation task clearing to prevent stuck player pose
- Added clean Stop() and Abort() logic
- Added cooldown enforcement and state reset
- Removed misuse of DebugForcePayout / DebugForceEscape in normal flow
- Improved eligibility checks and failure handling
- Improved subtitle flow for success/failure

---

🚨 Police System
- Police logic fully suppressed during SafeCrack
- SilentRobbery flag now respected across all escalation paths
- Wanted level locked to 0 during SafeCrack
- Heat level locked to 0 during SafeCrack
- Clerk calls, silent alarms, camera alarms, and timers fully disabled
- Added debug trace logging for suppression events

---

📹 Camera System
- Camera detection fully disabled during SafeCrack
- Camera alarms suppressed during SilentRobbery
- Interior and fallback cameras now respect stealth mode
- Grace timers prevented from expiring during SafeCrack
- Debug camera triggers suppressed
- Cleaned fallback camera direction logic
- Removed invalid store references in helper methods

---

💰 Robbery System
- Robbery logic paused during SafeCrack
- Timer UI cleared during SafeCrack
- No subtitles during SafeCrack (escape, don’t leave yet, etc.)
- Completion now requires SafeCracked if store has a safe
- Early escape logic updated to respect SafeCracked flag
- DebugResetStore now resets SafeCrack state
- SafeCrack interaction trigger cleaned and stabilized
- Prevented double‑start of SafeCrack
-  heist banner during SafeCrack
- Improved payout + cooldown flow

---

🎨 UI / Subtitle System
- Removed all conflicting subtitles during SafeCrack
- Removed timer UI overlap
- Removed early escape spam
- Added clean subtitle flow for SafeCrack success/failure
- Ensured heist banner only appears after full robbery completion

---

🛠 Stability & Cleanup
- Fixed multiple stuck‑control scenarios
- Fixed first‑person camera not restoring
- Fixed animation lock after minigame
- Improved debug escape behavior
- Improved debug payout behavior
- Added additional trace logging for QA
- General code cleanup and consistency pass

---

📦 Summary
v2.3.7.9 is the first fully stable, fully integrated SafeCrack release.
Every subsystem now communicates correctly, respects stealth mode, and restores the player to a clean state after the minigame.

This version is recommended for all users and required for anyone using the SafeCrack feature.
