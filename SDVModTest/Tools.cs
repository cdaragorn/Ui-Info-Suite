using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace UIInfoSuite
{
    internal static class Tools
    {
        public enum Quality
        {
            Normal,
            Silver,
            Gold,
            Iridium
        }

        private const float QualityModSilver = 1.25f;
        private const float QualityModGold = 1.5f;
        private const float QualityModIridium = 2f;

        public static void CreateSafeDelayedDialogue(string dialogue, int timer)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(timer);

                do
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
                while (Game1.activeClickableMenu is GameMenu);
                Game1.setDialogue(dialogue, true);
            });
        }

        public static int GetWidthInPlayArea()
        {
            var result = 0;

            if (Game1.isOutdoorMapSmallerThanViewport())
            {
                var right = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right;
                var totalWidth = Game1.currentLocation.map.Layers[0].LayerWidth * Game1.tileSize;
                var someOtherWidth = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right - totalWidth;

                result = right - someOtherWidth / 2;
            }
            else
            {
                result = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right;
            }

            return result;
        }

        public static int GetTruePrice(Item item)
        {
            var truePrice = 0;

            switch (item)
            {
                case StardewValley.Object objectItem:
                    truePrice = objectItem.sellToStorePrice() * 2;
                    break;
                case Item thing:
                    truePrice = thing.salePrice();
                    break;
            }


            return truePrice;
        }

        public static void DrawMouseCursor()
        {
            if (!Game1.options.hardwareCursor)
            {
                var mouseCursorToRender = Game1.options.gamepadControls ? Game1.mouseCursor + 44 : Game1.mouseCursor;
                var what = Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, mouseCursorToRender, 16, 16);

                Game1.spriteBatch.Draw(
                    Game1.mouseCursors,
                    new Vector2(Game1.getMouseX(), Game1.getMouseY()),
                    what,//new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, Game1.mouseCursor + 32, 16, 16)),
                    Color.White,
                    0.0f,
                    Vector2.Zero,
                    Game1.pixelZoom + (Game1.dialogueButtonScale / 150.0f),
                    SpriteEffects.None,
                    1f);
            }
        }

        public static Item GetHoveredItem()
        {
            Item hoverItem = null;

            foreach (var t in Game1.onScreenMenus)
            {
                if (!(t is Toolbar onScreenMenu)) continue;
                var hoverItemField = typeof(Toolbar).GetField("hoverItem", BindingFlags.Instance | BindingFlags.NonPublic);
                if (!(hoverItemField is null)) hoverItem = hoverItemField.GetValue(onScreenMenu) as Item;
                //hoverItemField.SetValue(onScreenMenu, null);
            }

            if (Game1.activeClickableMenu is GameMenu gameMenu)
            {
                foreach (var menu in gameMenu.pages)
                {
                    if (!(menu is InventoryPage inventory)) continue;
                    var hoveredItemField = typeof(InventoryPage).GetField("hoveredItem", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (!(hoveredItemField is null)) hoverItem = hoveredItemField.GetValue(inventory) as Item;
                    //typeof(InventoryPage).GetField("hoverText", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(menu, "");
                }
            }

            if (Game1.activeClickableMenu is ItemGrabMenu itemMenu)
            {
                
                hoverItem = itemMenu.hoveredItem;
                //(Game1.activeClickableMenu as MenuWithInventory).hoveredItem = null;
            }

            return hoverItem;
        }

        public static int AdjustPriceForQuality(int price, Quality quality)
        {
            var ret = price;
            switch (quality)
            {
                case Quality.Silver:
                    ret = (int) (price * QualityModSilver);
                    break;
                case Quality.Gold:
                    ret = (int)(price * QualityModGold);
                    break;
                case Quality.Iridium:
                    ret = (int)(price * QualityModIridium);
                    break;
                case Quality.Normal:
                default:
                    break;
            }

            return ret;
        }
    }
}
