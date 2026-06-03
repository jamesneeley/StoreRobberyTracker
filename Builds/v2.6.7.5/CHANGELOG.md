Store Robbery Enhanced — Changelog
Version: v2.6.7.5
Initial Release

OVERVIEW
--------
This is the first official public release of Store Robbery Enhanced.
It represents the completed Phase 3 architecture and the fully rebuilt
Single Player version of the GTA Online store robbery achievement system.

ADDED
-----
- Full 21‑store dataset (20 Online stores + Ace Liquor)
- Store ID remapping finalized (Old Store 14 → New Store 8)
- Complete robbery system:
    - Register payouts
    - Clerk reactions
    - Alarm escalation
    - Cooldowns
    - Subtitles
- Clerk AI system:
    - Fear, surrender, flee, escalation logic
- SafeCrack minigame with UI, difficulty scaling, and bonus payouts
- Minimap‑only blip system with dynamic state colors
- Full debug suite:
    - Debug overlay
    - Camera debug
    - Logging system
- Modular architecture:
    - Config
    - Data
    - Systems
    - UI
    - Minigame
    - Initialization
    - Debug

IMPROVED
--------
- Store alignment logic across all 21 stores
- Cooldown logic to prevent farming
- Register and safe position validation
- Camera overlay stability and performance
- Subtitle timing and state transitions

FIXED
-----
- Removed duplicate interiors
- Fixed store ID inconsistencies
- Corrected clerk position desync issues
- Fixed SafeCrack UI alignment
- Resolved blip visibility bugs on world map

NOTES
-----
- Store 8 is the official baseline reference store
- All systems are stable and ready for Phase 4 expansion
