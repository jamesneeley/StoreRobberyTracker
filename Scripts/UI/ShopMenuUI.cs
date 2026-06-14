using GTA;
using LemonUI.Elements;
using LemonUI.Menus;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;
using System;
using System.Drawing;
using static StoreRobberyEnhanced.Data.ShopItemData;

namespace StoreRobberyEnhanced.UI
{
    internal class ShopMenuUI
    {
        private readonly NativeMenu _menu;
        private readonly StoreContext _ctx;

        public NativeMenu Menu => _menu;
        public static DateTime UiBlockedUntil = DateTime.MinValue;

        public static void BlockUIForSeconds(int seconds)
        {
            UiBlockedUntil = DateTime.UtcNow.AddSeconds(seconds);
        }

        public ShopMenuUI(StoreContext ctx, TrackedStore store)
        {
            try
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
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopMenuUI.ctor: {ex}");
            }
        }

        // ============================================================
        // STORE NAME TO SUBTITLE MAPPING
        // ============================================================
        private string GetSubtitleForStore(TrackedStore store)
        {
            try
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
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopMenuUI.GetSubtitleForStore: {ex}");
                return "Store";
            }
        }

        // ============================================================
        // BANNER SELECTION
        // ============================================================
        private string GetBannerTextureDict(TrackedStore store)
        {
            try
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
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopMenuUI.GetBannerTextureDict: {ex}");
                return "shopui_title_conveniencestore";
            }
        }

        // Rockstar uses same dict + texture name for each store type, so we can reuse the dict name as the texture name
        private string GetBannerTextureName(TrackedStore store)
        {
            try
            {
                // Rockstar uses same dict + texture name
                return GetBannerTextureDict(store);
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopMenuUI.GetBannerTextureName: {ex}");
                return "shopui_title_conveniencestore";
            }
        }

        // ============================================================
        // MENU BUILDING
        // ============================================================
        private void BuildMenu()
        {
            try
            {
                foreach (var item in ShopItemDatabase.Items.Values)
                {
                    var menuItem = new NativeItem(item.Name, item.Description)
                    {
                        AltTitle = $"${item.Price}"   // Right‑aligned price
                    };

                    menuItem.Activated += (sender, args) =>
                    {
                        try
                        {
                            HandlePurchase(item);
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.Error($"ShopMenuUI.MenuItem.Activated: {ex}");
                        }
                    };

                    _menu.Add(menuItem);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopMenuUI.BuildMenu: {ex}");
            }
        }

        // Show the menu (called when player interacts with store)
        public void Show()
        {
            try
            {
                _menu.Visible = true;
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopMenuUI.Show: {ex}");
            }
        }

        // Hide the menu (called when player walks away from store)
        public void Hide()
        {
            try
            {
                _menu.Visible = false;
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopMenuUI.Hide: {ex}");
            }
        }
        // ============================================================
        // PURCHASE HANDLING
        // ============================================================
        private void HandlePurchase(ShopItemData item)
        {
            try
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
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopMenuUI.HandlePurchase: {ex}");
            }
        }        
    }
}
