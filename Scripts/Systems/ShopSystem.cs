using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using StoreRobberyEnhanced.UI;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;

namespace StoreRobberyEnhanced.Systems
{
    internal class ShopSystem
    {
        private readonly StoreContext _ctx;

        // One menu per store
        private readonly Dictionary<int, ShopMenuUI> _menus = new Dictionary<int, ShopMenuUI>();

        private const float INTERACT_DISTANCE = 2.0f;

        public ShopSystem(StoreContext ctx)
        {
            try
            {
                _ctx = ctx;
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopSystem.ctor: {ex}");
            }
        }

        // ============================================================
        // TICK HANDLING
        // ============================================================
        public void Tick()
        {
            try
            {
                // ⭐ Prevent LemonUI from drawing during banner display
                if (DateTime.UtcNow < ShopMenuUI.UiBlockedUntil)
                    return;

                // Always process LemonUI
                _ctx.MenuPool.Process();

                var player = Game.Player.Character;
                if (!player.Exists())
                    return;

                // ------------------------------------------------------------
                // CLOSE MENU INPUT (ESC or B)
                // ------------------------------------------------------------
                bool cancelKey = Game.IsKeyPressed(System.Windows.Forms.Keys.Escape);
                bool cancelPad = Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, (int)Control.PhoneCancel);

                if (cancelKey || cancelPad)
                {
                    CloseAllMenus();
                    return;
                }

                // If any menu is open, do not show prompts
                if (IsAnyMenuOpen())
                    return;

                // ------------------------------------------------------------
                // STORE INTERACTION CHECK (Interior‑based)
                // ------------------------------------------------------------
                int playerInterior = Function.Call<int>(Hash.GET_INTERIOR_FROM_ENTITY, player.Handle);

                foreach (var store in _ctx.Stores)
                {
                    try
                    {
                        // ⭐ First: interior must match
                        if (store.InteriorId != playerInterior)
                            continue;

                        // ⭐ Second: check distance to clerk inside that interior
                        float dist = player.Position.DistanceTo(store.ClerkPos);

                        if (dist <= INTERACT_DISTANCE)
                        {
                            _ctx.Ui.ShowHelpText("Press ~INPUT_FRONTEND_ACCEPT~ to shop");

                            bool interactKey = Game.IsKeyPressed(System.Windows.Forms.Keys.E);
                            bool interactPad = Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, (int)Control.FrontendAccept);

                            if (interactKey || interactPad)
                            {
                                OpenMenu(store);
                            }

                            return; // Only show prompt for the correct store
                        }
                    }
                    catch (Exception exStore)
                    {
                        DebugLogger.Error($"ShopSystem.Tick.StoreLoop: {exStore}");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopSystem.Tick: {ex}");
            }
        }

        // ============================================================
        // MENU HANDLING
        // ============================================================
        private void OpenMenu(TrackedStore store)
        {
            try
            {
                if (!_menus.TryGetValue(store.Id, out var menu))
                {
                    // ⭐ Updated: pass the full store object, not store.Name
                    menu = new ShopMenuUI(_ctx, store);
                    _menus.Add(store.Id, menu);
                }

                menu.Show();
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopSystem.OpenMenu: {ex}");
            }
        }

        // Closes all open menus
        private void CloseAllMenus()
        {
            try
            {
                foreach (var menu in _menus.Values)
                {
                    if (menu.Menu.Visible)
                        menu.Menu.Visible = false;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopSystem.CloseAllMenus: {ex}");
            }
        }

        // Checks if any menu is currently open
        private bool IsAnyMenuOpen()
        {
            try
            {
                foreach (var menu in _menus.Values)
                {
                    if (menu.Menu.Visible)
                        return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopSystem.IsAnyMenuOpen: {ex}");
                return false;
            }
        }
    }
}
