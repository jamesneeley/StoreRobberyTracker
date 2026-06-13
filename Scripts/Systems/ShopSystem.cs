using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
using StoreRobberyEnhanced.UI;
using StoreRobberyEnhanced.Data;

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
            _ctx = ctx;
        }

        public void Tick()
        {
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
        }

        private void OpenMenu(TrackedStore store)
        {
            if (!_menus.TryGetValue(store.Id, out var menu))
            {
                // ⭐ Updated: pass the full store object, not store.Name
                menu = new ShopMenuUI(_ctx, store);
                _menus.Add(store.Id, menu);
            }

            menu.Show();
        }

        private void CloseAllMenus()
        {
            foreach (var menu in _menus.Values)
            {
                if (menu.Menu.Visible)
                    menu.Menu.Visible = false;
            }
        }

        private bool IsAnyMenuOpen()
        {
            foreach (var menu in _menus.Values)
            {
                if (menu.Menu.Visible)
                    return true;
            }
            return false;
        }
    }
}
