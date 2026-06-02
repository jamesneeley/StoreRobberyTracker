## 🏪 Store Robbery Enhanced — Bringing GTA Online’s Robbery Achievements to Single Player

Store Robbery Enhanced is a complete re‑imagining of the classic GTA Online store robbery loop — rebuilt from the ground up for **GTA V Single Player**.  
This project faithfully recreates the Online-style robbery achievement experience, then expands it with deeper systems, smarter AI, immersive UI, and a fully modular architecture designed for long-term growth.

In GTA Online, robbing all convenience stores is a structured progression path:  
- Each store has its own state  
- Cooldowns prevent farming  
- Clerks react dynamically  
- Safes offer bonus payouts  
- Subtitles guide the player  
- Achievements track your progress  

**Store Robbery Enhanced brings that entire experience into Single Player — and then pushes it further.**

### 🎯 What Makes This Mod Different?
This isn’t a simple “point gun, get money” script.  
It’s a **full robbery ecosystem**, built to feel like Rockstar designed it for Story Mode:

- Every store is tracked, saved, and managed  
- Robbery states persist across sessions  
- Cooldowns mirror GTA Online’s anti‑farm system  
- Clerks behave with fear, surrender, and escalation logic  
- Safes can be cracked for bonus cash  
- Subtitles recreate the Online-style robbery prompts  
- Minimap-only blips match the Online UI  
- Debug tools let developers inspect every system in real time  

The goal was simple:  
**Bring the GTA Online robbery achievement system into Single Player — with the polish, depth, and reliability it always deserved.**

### 🧩 Built for Players *and* Modders
Under the hood, Store Robbery Enhanced is a fully modular SHVDN 3.9.0 project with clean architecture:

- Configurable store data  
- Expandable systems  
- Debug overlays  
- Camera tools  
- Minigame framework  
- UI layer  
- Event-driven logic  

Whether you're a player looking for a more immersive robbery experience or a developer wanting a clean foundation to build on, this project delivers.

### 🚀 Designed for Expansion
This mod is built with future systems in mind:

- Achievements  
- Progression  
- Vendors  
- Loadouts  
- Economy simulation  
- Pets & mounts  
- Battle pass  
- Crafting  
- Augments & sockets  

The foundation is already in place — Phase 3 completed — and the project is ready for long-term evolution.

Store Robbery Enhanced is not just a mod.  
It’s the **Single Player version of what GTA Online’s store robberies should have been.**

---

## 🔥 Features (Player Overview)

### 🏪 Dynamic Store Robberies
- Every store is fully tracked and managed.
- Clerk reactions, alarm triggers, and payout scaling.
- Optional safe cracking for bonus cash.
- Store‑specific cooldowns prevent farming.

### 🔧 Enhanced AI & Systems
- Clerk fear, surrender, and flee logic.
- Alarm escalation and police response hooks.
- Camera system for immersion and debugging.

### 💰 Safe Cracking Minigame
- Interactive safe cracking system.
- Difficulty scaling per store.
- Bonus payout on successful crack.

### 🗺️ Clean Minimap Integration
- Robbery blips appear **only on the minimap**, not the world map.
- Dynamic color changes based on robbery state.

### 🛠️ Full Debug Suite
- On‑screen debug overlay.
- Camera debug mode.
- Real‑time store state tracking.
- Logging system for developers.

---

## 📦 Installation

### Requirements
- **GTA V (Latest Version)**
- **ScriptHookV**
- **ScriptHookVDotNet (SHVDN) 3.9.0 Enhanced — Recommended**
- **.NET Framework 4.8**

### Install Steps
1. Install **ScriptHookV** (Alexander Blade).
2. Install **ScriptHookVDotNet 3.9.0 Enhanced**.
3. Drag the `StoreRobberyEnhanced.dll` (or project output) into:
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

---

## 🧠 System Architecture

### **Initialization Layer**
Handles:
- Script startup
- Store registration
- Context creation
- Debug system bootstrapping

### **Core Systems**
- **RobberySystem** — main robbery logic  
- **ClerkSystem** — clerk AI, fear, surrender  
- **CooldownSystem** — prevents repeated farming  
- **AlarmSystem** — escalation and police hooks  
- **SubtitleSystem** — dynamic robbery subtitles  
- **CameraSystem** — camera overlay + debug  

### **Minigame Layer**
- Safe cracking logic  
- UI rendering  
- Difficulty scaling  

### **Debug Layer**
- Real‑time overlay  
- Camera debug  
- Logging  

### **UI Layer**
- Notifications  
- Menus  
- Minimap blips  

---

## 🧪 Developer Debug Tools

### Debug Overlay
Shows:
- Store ID  
- Robbery state  
- Clerk state  
- Cooldown timers  
- Safe status  
- Alarm state  

### Camera Debug
- Toggleable camera overlay  
- Real‑time camera vectors  
- Store interior validation  

### Logging
- Timestamped logs  
- System‑level event tracking  
- Error tracing  

---

## 🛠️ Building the Project

1. Open `StoreRobberyEnhanced.sln` in Visual Studio.
2. Ensure **.NET Framework 4.8** is installed.
3. Build in **Release** mode.
4. Output DLL will appear in:
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
Created by **James Neeley**  AKA FastBurst
GTA V Mod Developer & Systems Architect

