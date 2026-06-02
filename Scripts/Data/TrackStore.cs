using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using StoreRobberyTrackerMod.Minigame;

namespace StoreRobberyTrackerMod.Data
{
    public enum ClerkReactionType
    {
        NormalPanic,
        Flee,
        FightPistol,
        FightShotgun
    }

    internal class TrackedStore
    {
        public int Id;

        // Basic store info
        public string Name;
        public Vector3 StorePos;
        public float Radius = 3.0f;     // ⭐ NEW

        // Clerk
        public Ped Clerk; // already there, just noting
        public bool DefaultClerkRemoved;
        public Vector3 ClerkPos;
        public float ClerkHeading;
        public DateTime LastClerkSweepUtc;
        public bool IsOurClerk; // new
        public bool ClerkIdle;
        public bool ClerkOpeningRegister;
        public bool ClerkGrabbingCash;
        public bool ClerkThrowingBag;
        public bool ClerkPanicking;
        public bool ClerkFleeing;
        public DateTime ClerkAnimStartUtc;
        public int ClerkAnimDurationMs;
        // --- ClerkReplacementSystem timing support ---
        public bool PlayerInside { get; set; } = false;
        public DateTime PlayerEnteredUtc { get; set; } = DateTime.MinValue;


        public ClerkReactionType ReactionType;

        // ⭐ NEW — dummy / neutralizer support
        public Ped DummyClerk;              // invisible native stand‑in
        public bool DummyClerkSpawned;      // we created the dummy
        public bool NativeClerksNeutralized; // Rockstar clerks swept/disabled

        // Registers info
        public Vector3 RegisterPos;
        public float RegisterHeading;

        // Safe
        public Vector3 SafePos;
        public float SafeHeading;
        public bool SafeCracked;
        public SafeCrackController SafeCrack;

        // Door
        public Vector3 DoorPos;
        public int DoorModelHash;
        public int DoorSystemId;
        public float DoorHeading = 0f;  // default until set in initializer

        // Derived door forward vector (used for cooldown blocker placement)
        public Vector3 DoorForward
        {
            get
            {
                // Convert heading (degrees) to radians
                float rad = DoorHeading * (float)(Math.PI / 180.0);
                // GTA coordinate system: X = east-west, Y = north-south
                return new Vector3((float)Math.Sin(rad), (float)Math.Cos(rad), 0f);
            }
        }

        // Cameras (fallback cameras only)
        public List<CameraData> Cameras = new List<CameraData>();
        //public List<Vector3> Cameras = new List<Vector3>();

        // Robbery state
        public bool IsRobbed;
        public bool PendingCompletion;
        public int PendingPayout;
        public DateTime RobberyStartUtc = DateTime.MinValue;

        // Cooldown
        public bool CooldownActive;
        public DateTime LastRobbedUtc = DateTime.MinValue;
        public Prop CooldownBlocker;

        // Clerk reaction state
        public bool ClerkReacted;
        public bool ClerkStalling;
        public DateTime StallStartUtc = DateTime.MinValue;
        public int StallDurationMs;
        public bool ClerkDeathHandled;
        public bool ClerkKilledWithGun;
        public bool SilentRobbery;

        // Alarm / police
        public bool AlarmTriggered;
        public int HeatLevel;

        // Player state at robbery start
        public bool PlayerMaskedAtStart;

        // Blip reference
        public Blip Blip;

        // ⭐ NEW — Required for camera/police/stalker logic
        public bool IsRobberyActive;     // True only while robbery is in progress
        public bool IsPlayerInsideStore; // True only when player is physically inside store radius

        // ⭐ PHASE 3 — NEW FIELDS

        // Silent alarm pressed by clerk
        public bool SilentAlarmPressed;
        public DateTime SilentAlarmUtc = DateTime.MinValue;

        // Clerk calling police
        public bool ClerkCallingPolice;
        public DateTime ClerkCallStartUtc = DateTime.MinValue;

        // Repeat robbery memory
        public int TimesRobbed;
        public bool RepeatRobberyEscalationApplied;

        // Mask escalation
        public bool MaskEscalationApplied;

        // Clerk fight-back escalation
        public bool FightEscalationApplied;

        // Multi-stage escalation (robbery taking too long)
        public bool TimeEscalationApplied;

        // Store reputation / fear memory
        public bool ClerkRecognizedPlayer;

        public TrackedStore()
        {
            Cameras = new List<CameraData>();

            // Initialize new fields
            IsRobberyActive = false;
            IsPlayerInsideStore = false;

            SilentAlarmPressed = false;
            ClerkCallingPolice = false;
            TimesRobbed = 0;
            RepeatRobberyEscalationApplied = false;
            MaskEscalationApplied = false;
            FightEscalationApplied = false;
            TimeEscalationApplied = false;
            ClerkRecognizedPlayer = false;

            // Dummy / neutralizer fields
            DummyClerk = null;
            DummyClerkSpawned = false;
            NativeClerksNeutralized = false;
        }
    }

    internal class StalkerEvent
    {
        public int TriggerTime;
        public List<string> Pool;
    }
}
