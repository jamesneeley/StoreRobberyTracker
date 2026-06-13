using GTA;
using LemonUI;
using LemonUI.Elements;
using LemonUI.Menus;
using StoreRobberyEnhanced.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using static StoreRobberyEnhanced.Data.ShopItemData;

namespace StoreRobberyEnhanced.UI
{
    internal class ShopMenuUI
    {
        private readonly NativeMenu _menu;
        private readonly StoreContext _ctx;

        public NativeMenu Menu => _menu;

        public ShopMenuUI(StoreContext ctx, TrackedStore store)
        {
            _ctx = ctx;

            // Build correct subtitle based on store type
            string subtitle = GetSubtitleForStore(store);

            // Remove store name text from banner, use dynamic subtitle
            _menu = new NativeMenu("", subtitle);

            // Add to global pool
            _ctx.MenuPool.Add(_menu);

            // Apply correct Rockstar banner based on store type
            _menu.Banner = new ScaledTexture(
                new PointF(0f, 0f),
                new SizeF(512f, 128f),
                GetBannerTextureDict(store),
                GetBannerTextureName(store)
            );

            BuildMenu();
        }

        // ============================================================
        // STORE NAME TO SUBTITLE MAPPING (NAME‑BASED, NOT INTERIOR‑BASED)
        // ============================================================
        private string GetSubtitleForStore(TrackedStore store)
        {
            string name = store.Name.ToLower();

            if (name.Contains("rob"))
                return "Rob's Liquor";

            if (name.Contains("ltd"))
                return "LTD Gas Station";

            if (name.Contains("ace"))
                return "Liquor Ace";

            // Default for all 24/7 stores
            return "24/7 Supermarket";
        }

        // ============================================================
        // BANNER SELECTION (INTERIOR‑BASED, NOT NAME‑BASED)
        // ============================================================

        private string GetBannerTextureDict(TrackedStore store)
        {
            string name = store.Name.ToLower();

            // LTD Gasoline
            if (name.Contains("ltd"))
                return "shopui_title_gasstation";

            // Rob's Liquor (6 stores)
            if (name.Contains("rob"))
                return "shopui_title_liquorstore2";

            // Ace Liquor (unique interior)
            if (name.Contains("ace"))
                return "shopui_title_liquorstore";

            // Default 24/7
            return "shopui_title_conveniencestore";
        }

        private string GetBannerTextureName(TrackedStore store)
        {
            // Rockstar uses same dict + texture name
            return GetBannerTextureDict(store);
        }

        // ============================================================
        // MENU BUILDING
        // ============================================================
        private void BuildMenu()
        {
            foreach (var item in ShopItemDatabase.Items.Values)
            {
                var menuItem = new NativeItem(item.Name, item.Description)
                {
                    AltTitle = $"${item.Price}"   // ⭐ Right‑aligned price
                };

                menuItem.Activated += (sender, args) =>
                {
                    HandlePurchase(item);
                };

                _menu.Add(menuItem);
            }
        }

        // ============================================================
        // PURCHASE HANDLING
        // ============================================================

        private void HandlePurchase(ShopItemData item)
        {
            if (Game.Player.Money < item.Price)
            {
                _ctx.Ui.ShowSubtitle("~r~Not enough money.");
                return;
            }

            Game.Player.Money -= item.Price;

            // Hand off to ShopConsumeSystem for animation + effects
            _ctx.ConsumeSystem.QueueItem(item.Id);

            _ctx.Ui.ShowSubtitle(
                $"Purchased ~g~{item.Name}~w~ for ~g~${item.Price}"
            );

            _menu.Visible = false;
        }

        public void Show() => _menu.Visible = true;
        public void Hide() => _menu.Visible = false;
    }
}
