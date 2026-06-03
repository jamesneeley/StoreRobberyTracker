using System;
using GTA.Math;

namespace StoreRobberyTrackerMod.Data
{
    internal class CameraData
    {
        public Vector3 Position;
        public bool Destroyed;

        // ⭐ Grace system
        public bool GraceActive;
        public DateTime GraceStartUtc;
        public double GraceDurationSeconds;   // ⭐ NEW

        // Optional future expansion (CameraSystem may use this later)
        public float Direction;

        // ------------------------------------------------------------
        // DEFAULT CONSTRUCTOR
        // ------------------------------------------------------------
        public CameraData()
        {
            Position = new Vector3(0f, 0f, 0f);
            Destroyed = false;
            GraceActive = false;
            GraceStartUtc = DateTime.MinValue;
            GraceDurationSeconds = 0;   // ⭐ NEW
            Direction = 0f;
        }

        // ------------------------------------------------------------
        // CONSTRUCTOR USED BY StoreInitializer
        // ------------------------------------------------------------
        public CameraData(Vector3 pos)
        {
            Position = pos;
            Destroyed = false;
            GraceActive = false;
            GraceStartUtc = DateTime.MinValue;
            GraceDurationSeconds = 0;   // ⭐ NEW
            Direction = 0f;
        }

        // ------------------------------------------------------------
        // OPTIONAL: Constructor with direction
        // ------------------------------------------------------------
        public CameraData(Vector3 pos, float direction)
        {
            Position = pos;
            Destroyed = false;
            GraceActive = false;
            GraceStartUtc = DateTime.MinValue;
            GraceDurationSeconds = 0;   // ⭐ NEW
            Direction = direction;
        }
    }
}
