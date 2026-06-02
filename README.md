# 🏪 Store Robbery Enhanced  
### Bringing GTA Online’s Robbery Achievements to Single Player — Rebuilt, Expanded, and Modernized

Store Robbery Enhanced is a complete re‑imagining of the classic GTA Online store robbery loop — rebuilt from the ground up for **GTA V Single Player**.

This project faithfully recreates the Online-style robbery achievement experience, then expands it with deeper systems, smarter AI, immersive UI, psychological tension, and a fully modular architecture designed for long-term growth.

In GTA Online, robbing all convenience stores is a structured progression path:

- Each store has its own state  
- Cooldowns prevent farming  
- Clerks react dynamically  
- Safes offer bonus payouts  
- Subtitles guide the player  
- Achievements track your progress  

**Store Robbery Enhanced brings that entire experience into Single Player — and then pushes it further.**

---

# 🎯 What Makes This Mod Different?

This isn’t a simple “point gun, get money” script.  
It’s a **full robbery ecosystem**, built to feel like Rockstar designed it for Story Mode:

- Every store is tracked, saved, and managed  
- Robbery states persist across sessions  
- Cooldowns mirror GTA Online’s anti‑farm system  
- Clerks behave with fear, surrender, and escalation logic  
- Safes can be cracked for bonus cash  
- Subtitles recreate the Online-style robbery prompts  
- Minimap-only blips match the Online UI  
- A psychological **Stalker System** reacts to your behavior  
- A full **debug suite** lets developers inspect every system in real time  

The goal was simple:  
**Bring the GTA Online robbery achievement system into Single Player — with the polish, depth, and reliability it always deserved.**

---

# 🔥 Features (Player Overview)

## 🏪 Dynamic Store Robberies
- 21 fully supported stores (20 Online stores + Ace Liquor)  
- Persistent store states:
  - Robbed / Not Robbed  
  - Cooldown active  
  - Safe cracked  
  - Alarm triggered  
  - Clerk killed (gun/melee)  
- Store states save across sessions  
- Cooldowns prevent farming  
- Register payouts scale per store  
- Optional safe cracking for bonus cash  

---

## 🔧 Enhanced AI & Systems
- Clerk fear, surrender, and flee logic  
- Alarm escalation and police response hooks  
- Silent alarm triggers  
- Clerk phone calls to police  
- Time‑based escalation  
- Camera system for immersion and debugging  

---

## 💰 Safe Cracking Minigame
A fully interactive, skill‑based safe cracking system:

- Dial rotation with sweet‑spot detection  
- Difficulty scaling per store  
- Bonus payout on success  
- Configurable safe crack time  
- Configurable cooldown  
- Optional controller vibration  
- Optional loading of “optional safes”  
- Clean UI with timer, dial, and feedback  

---

## 🔪 Stalker System — Psychological Threat Layer
A dynamic psychological system that reacts to your behavior:

- **350+ reactive stalker messages**  
- Categories include:
  - Knockout  
  - MeleeKill  
  - GunKill  
  - Robbery  
  - Escape  
  - CallAnswered  
  - CallIgnored  
- Stalker phone calls with custom caller ID  
- Behavior‑based reactions:
  - Violence level  
  - Escape style  
  - Sloppiness vs precision  
  - Morality patterns  
- Adds a thriller‑style narrative layer to every robbery  

---

## 🗺️ Clean Minimap Integration
- Blips appear **only on the minimap**, not the world map  
- Dynamic color changes based on robbery state  
- Matches GTA Online’s UI style  

---

## 🛠️ Full Debug Suite (Developer Tools)

### Debug Overlay
Shows:
- Store ID  
- Store name  
- Robbery state  
- Clerk state  
- Cooldown timers  
- Safe status  
- Alarm state  
- Camera state  
- System flags  

### Camera Debug
- Toggleable camera overlay  
- Real‑time camera vectors  
- Interior validation  
- Store alignment tools  

### Profiler
- Optional performance profiler  
- Auto‑dump mode  
- Interval‑based dumps  

### Scenario Runner
- Full robbery simulation  
- Quick loot simulation  
- Auto snapshot on scenario  

### Debug Hotkeys and the pre-assigned values
- Start robbery                    // 97  NumPad1
- Trigger safe crack               // 98  NumPad2
- Trigger safe crack minigame      // 107 NumPad Add
- Trigger alarm                    // 99  NumPad3
- trigger escape                   // 100 NumPad4
- Trigger payout                   // 101 NumPad5
- Trigger cooldown                 // 102 NumPad6
- Trigger stalker event            // 103 NumPad7
- Toggle UI                        // 104 NumPad8
- Toggle banner                    // 105 NumPad9
- Toggle timer                     // 96  NumPad0
- Store diagnostics                // 121 F10
- Multi‑position debug             // 111 NumPad Divide
- Misc actions                     // 106 NumPad Multiply
- Camera debug                     // 114 F3

---

# ⚙️ INI Settings (Full Breakdown)

## 📄 DebugSettings.ini

### **[Debug]** and pre-assugned valus for HotKeys (if any)
- EnableDebug — master debug toggle                  // true/false
- OverlayVisible — show/hide debug overlay           // true/false
- DebugLevel — verbosity (0–3)                       // 2 is the default
- ModifierKey — modifier for debug actions           // 162 Left CTRL
- ToggleKey — toggle overlay                         // 120 F9                
- Action_* — hotkeys for all debug actions           // SEE ABOVE SECTION
- Scenario_FullRobbery — run full robbery simulation // 112 F1 
- Scenario_QuickLoot — run quick loot simulation     // 113 F2
- EnableProfiler — enable profiler                   // true/false
- Profiler_AutoDump — auto‑dump profiler data        // true/false
- Profiler_DumpInterval — dump interval              // 30
- EnableFileManager — enable debug file manager      // true/false
- AutoSnapshotOnScenario — auto snapshot             // true/false

---

## 📄 StalkerMessages.ini
Contains **350+ lines** across categories:

- Knockout  
- MeleeKill  
- GunKill  
- Robbery  
- Escape  
- CallAnswered  
- CallIgnored  

Each category contains 35 unique lines.

---

## 📄 StoreRobberyTracker.ini (Main Settings)

### **[Main Settings]**
- EnableMessages  
- EnablePolice  
- CooldownMinutes  
- RobberyTimeLimit  
- EscapeDistance  

### **[Police]**
- SilentAlarmDelaySeconds  
- ClerkCallDelaySeconds  
- TimeEscalationSeconds  

### **[Stalker]**
- EnableStalkerMsg  
- EnableStalkerCall  
- StalkerCallChance  
- CallerImage  
- CallerName  
- MaxMessagesPerRobbery  
- MessageCooldownSeconds  

### **[Store Settings]**
- RegisterMinAmount / MaxAmount  
- SafeMinAmount / MaxAmount  
- EnableCameras  
- UseStoreNames  
- CameraGraceSeconds  
- SafeCrackTimeSeconds  
- PayoutMultiplier  
- SafeCrackCooldownMs  
- SafeCrackPadShake  
- SafeCrackLoadOptionalSafes  

### **[TIMER_UI]**
- PositionX / PositionY  
- Scale  
- DropShadow  
- Background  
- BackgroundWidth / Height  
- BackgroundOpacity  
- BackgroundColorR/G/B  

---

## 📄 StoreState.ini
Tracks persistent store data:

- IsRobbed  
- CooldownActive  
- SafeCracked  
- AlarmTriggered  
- ClerkDeathHandled  
- ClerkKilledWithGun  
- LastRobbedUtc  

All 21 stores included.

---

# 📦 Installation

### Requirements
- **GTA V (Latest Version)**  
- **ScriptHookV**  
- **ScriptHookVDotNet 3.9.0 Enhanced**  
- **.NET Framework 4.8**

### Install Steps
1. Install ScriptHookV  
2. Install ScriptHookVDotNet 3.9.0 Enhanced  
3. Drag `StoreRobberyEnhanced.dll` into:

   ```
   Grand Theft Auto V/scripts/
   ```
4. Launch the game.

---

## 🧩 Folder Structure (Developer Overview)

```
StoreRobberyEnhanced/
│
├── Properties/
│   └── AssemblyInfo.cs
│
├── Scripts/
│   ├── Config/
│   │   ├── ConfigManager.cs
│   │   └── StoreConfig.cs
│   │
│   ├── Data/
│   │   ├── TrackedStore.cs
│   │   ├── StalkerEvent.cs
│   │   └── StoreDataLoader.cs
│   │
│   ├── Debug/
│   │   ├── DebugOverlay.cs
│   │   ├── DebugLogger.cs
│   │   └── CameraDebug.cs
│   │
│   ├── Initialization/
│   │   ├── Main.cs
│   │   ├── StoreInitializer.cs
│   │   └── StoreContext.cs
│   │
│   ├── Minigame/
│   │   ├── SafeCrackController.cs
│   │   └── SafeCrackUI.cs
│   │
│   ├── Systems/
│   │   ├── RobberySystem.cs
│   │   ├── ClerkSystem.cs
│   │   ├── CooldownSystem.cs
│   │   ├── AlarmSystem.cs
│   │   ├── SubtitleSystem.cs
│   │   └── CameraSystem.cs
│   │
│   └── UI/
│       ├── NotificationUI.cs
│       ├── StoreMenu.cs
│       └── BlipUI.cs
│
├── .gitattributes
├── .gitignore
├── LICENSE.txt
├── README.md
├── StoreRobberyEnhanced.csproj
└── StoreRobberyEnhanced.sln

```

# 🧠 System Architecture

### Initialization Layer
- Script startup  
- Store registration  
- Context creation  
- Debug system bootstrapping  

### Core Systems
- RobberySystem  
- ClerkSystem  
- CooldownSystem  
- AlarmSystem  
- SubtitleSystem  
- CameraSystem  

### Minigame Layer
- Safe cracking logic  
- UI rendering  
- Difficulty scaling  

### Stalker Layer
- Behavioral tracking  
- Message selection  
- Phone call system  
- Psychological profiling  

### Debug Layer
- Overlay  
- Camera debug  
- Profiler  
- Logging  

### UI Layer
- Notifications  
- Menus  
- Minimap blips  

---

## 🔧 Restoring Project References

Due to licensing restrictions, ScriptHookV and ScriptHookVDotNet DLLs cannot be included in this repository.

To build the project:

1. Download ScriptHookVDotNet 3.9.0 Enhanced:
   https://www.gta5-mods.com/tools/script-hook-v-net-enhanced

2. Create a folder named `lib` in the project root.

3. Place the following files inside `/lib/`:
   - ScriptHookVDotNet.dll
   - ScriptHookVDotNet2.dll
   - ScriptHookVDotNet3.dll

4. Open the solution and build in Release mode.
   
---

# 🛠️ Building the Project

1. Open `StoreRobberyEnhanced.sln` in Visual Studio  
2. Ensure **.NET Framework 4.8** is installed  
3. Build in **Release** mode  
4. Output DLL appears in:
   ```
   /bin/Release/
   ```

---

## 📄 License
This project is licensed under the terms of the included `LICENSE.txt`.

---

## 🤝 Contributing
Pull requests are welcome.  
For major changes, open an issue first to discuss what you’d like to modify.

---

## ⭐ Credits
Created by **James Neeley**  AKA **FastBurst** Discord ID **FastBurst#7708**
GTA V Mod Developer & Systems Architect
