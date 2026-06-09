using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;
using System;

namespace StoreRobberyEnhanced.UI
{
    internal class PlayerHelper
    {
        private readonly StoreContext _ctx;

        public PlayerHelper(StoreContext ctx)
        {
            try
            {
                _ctx = ctx;
                DebugLogger.Info("PlayerHelper initialized");
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.ctor", ex);
            }
        }

        // ------------------------------------------------------------
        // BASIC STATES
        // ------------------------------------------------------------
        public bool IsAiming()
        {
            try
            {
                bool result = Game.IsControlPressed(Control.Aim);
                DebugLogger.Trace($"IsAiming() = {result}");
                return result;
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.IsAiming", ex);
                return false;
            }
        }

        public bool IsShooting()
        {
            try
            {
                bool result = Game.IsControlPressed(Control.Attack);
                DebugLogger.Trace($"IsShooting() = {result}");
                return result;
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.IsShooting", ex);
                return false;
            }
        }

        public bool IsArmed()
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                Weapon weapon = player.Weapons.Current;
                bool result = weapon != null && weapon.Hash != WeaponHash.Unarmed;

                DebugLogger.Trace($"IsArmed() = {result}");
                return result;
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.IsArmed", ex);
                return false;
            }
        }

        public bool IsMasked()
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                // Component 1 = masks
                int maskDrawable = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, player.Handle, 1);

                // Component 0 = hats / helmets
                int hatDrawable = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, player.Handle, 0);

                // Component 7 = accessories (bandanas, scarves)
                int accessoryDrawable = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, player.Handle, 7);

                bool masked =
                    maskDrawable != 0 ||      // actual mask
                    hatDrawable != 0 ||       // hat / helmet
                    accessoryDrawable != 0;   // bandana / scarf

                DebugLogger.Trace($"IsMasked() = {masked} (mask={maskDrawable}, hat={hatDrawable}, acc={accessoryDrawable})");
                return masked;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.IsMasked", ex);
                return false;
            }
        }

        // ------------------------------------------------------------
        // WEAPON TYPE CHECKS
        // ------------------------------------------------------------
        public bool IsMeleeWeapon(WeaponHash hash)
        {
            try
            {
                bool result =
                    hash == WeaponHash.Knife ||
                    hash == WeaponHash.Nightstick ||
                    hash == WeaponHash.Hammer ||
                    hash == WeaponHash.Bat ||
                    hash == WeaponHash.Crowbar ||
                    hash == WeaponHash.GolfClub ||
                    hash == WeaponHash.Bottle ||
                    hash == WeaponHash.Dagger ||
                    hash == WeaponHash.Hatchet ||
                    hash == WeaponHash.KnuckleDuster ||
                    hash == WeaponHash.Machete ||
                    hash == WeaponHash.Flashlight ||
                    hash == WeaponHash.SwitchBlade ||
                    hash == WeaponHash.PoolCue ||
                    hash == WeaponHash.Wrench;

                DebugLogger.Trace($"IsMeleeWeapon({hash}) = {result}");
                return result;
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.IsMeleeWeapon", ex);
                return false;
            }
        }

        // ------------------------------------------------------------
        // POSITIONAL CHECKS
        // ------------------------------------------------------------
        public bool IsNear(Vector3 pos, float dist)
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                bool result = player.Position.DistanceTo(pos) <= dist;
                DebugLogger.Trace($"IsNear(pos, {dist}) = {result}");
                return result;
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.IsNear", ex);
                return false;
            }
        }

        public bool IsInsideStore(TrackedStore store, float radius)
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                bool result = player.Position.DistanceTo(store.StorePos) <= radius;
                DebugLogger.Trace($"IsInsideStore(store={store.Id}, radius={radius}) = {result}");
                return result;
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.IsInsideStore", ex);
                return false;
            }
        }

        // ------------------------------------------------------------
        // LINE OF SIGHT
        // ------------------------------------------------------------
        public bool IsInLOS(Entity target)
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                if (target == null || !target.Exists())
                    return false;

                bool result = Function.Call<bool>(
                    Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY,
                    player.Handle,
                    target.Handle,
                    17
                );

                DebugLogger.Trace($"IsInLOS(target={target.Handle}) = {result}");
                return result;
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.IsInLOS", ex);
                return false;
            }
        }

        // ------------------------------------------------------------
        // THREAT CHECK (GUN-ONLY)
        // ------------------------------------------------------------
        public bool IsThreatening(Ped target)
        {
            try
            {
                if (target == null || !target.Exists())
                    return false;

                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                Weapon current = player.Weapons.Current;
                if (current == null)
                    return false;

                // ⭐ Only guns count as threats
                bool isGun =
                    current.Hash != WeaponHash.Unarmed &&
                    current.Group != WeaponGroup.Melee;

                if (!isGun)
                {
                    DebugLogger.Trace($"IsThreatening(target={target.Handle}) = false (melee ignored)");
                    return false;
                }

                // ⭐ Must be aiming AND have LOS
                bool result =
                    IsAiming() &&
                    IsInLOS(target);

                DebugLogger.Trace($"IsThreatening(target={target.Handle}) = {result} (gun={current.Hash})");
                return result;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.IsThreatening", ex);
                return false;
            }
        }
    }
}
