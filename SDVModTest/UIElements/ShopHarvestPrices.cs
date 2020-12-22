using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using UIInfoSuite.Extensions;

namespace UIInfoSuite.UIElements
{
    class ShopHarvestPrices : IDisposable
    {
        private readonly IModHelper _helper;

        public ShopHarvestPrices(IModHelper helper)
        {
            _helper = helper;
        }

        public void ToggleOption(bool shopHarvestPrices)
        {
            _helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu;

            if (shopHarvestPrices)
            {
                _helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            }
        }

        public void Dispose()
        {
            ToggleOption(false);
        }

        /// <summary>When a menu is open (<see cref="Game1.activeClickableMenu"/> isn't null), raised after that menu is drawn to the sprite batch but before it's rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (!(Game1.activeClickableMenu is ShopMenu menu)) return;
            if (!(menu.hoveredItem is Item hoverItem)) return;

            // draw shop harvest prices
            var isSeeds = hoverItem is StardewValley.Object hoverObject && hoverObject.Type == "Seeds";
            var isSapling = hoverItem.Name.EndsWith("Sapling");
            var value = 0;
            if (isSeeds 
                && hoverItem.Name != "Mixed Seeds"
                && hoverItem.Name != "Winter Seeds")
            {

                var itemHasPriceInfo = Tools.GetTruePrice(hoverItem) > 0;
                if (itemHasPriceInfo)
                {
                    var temp =
                        new StardewValley.Object(
                            new Debris(
                                new Crop(
                                        hoverItem.ParentSheetIndex,
                                        0,
                                        0)
                                    .indexOfHarvest.Value,
                                Game1.player.position,
                                Game1.player.position).chunkType.Value,
                            1);
                    value = temp.Price;
                }
                else
                {
                    switch (hoverItem.ParentSheetIndex)
                    {
                        case 802: value = 75; break;    // Cactus
                    }
                }
            }
            else if (isSapling)
            {
                switch (hoverItem.ParentSheetIndex)
                {
                    case 628: value = 50; break;    // Cherry
                    case 629: value = 80; break;    // Apricot
                    case 630:                        // Orange
                    case 633: value = 100; break;    // Apple
                    case 631:                        // Peach
                    case 632: value = 140; break;    // Pomegranate
                }
            }

            if (value > 0)
            {
                var xPosition = menu.xPositionOnScreen - 30;
                var yPosition = menu.yPositionOnScreen + 580;
                var height = isSapling ? 258 : 208;
                    IClickableMenu.drawTextureBox(
                    Game1.spriteBatch,
                    xPosition + 20,
                    yPosition - 52,
                    264,
                    height,
                    Color.White);
                // Title "Harvest Price"
                var textToRender = _helper.SafeGetString(LanguageKeys.HarvestPrice);
                Utility.drawTextWithShadow(
                    Game1.spriteBatch,
                    textToRender,
                    Game1.dialogueFont,
                    new Vector2(xPosition + 32, yPosition - 40),
                    Game1.textColor
                );

                //Calculate price with skill
                if (Game1.player.professions.Contains(Farmer.tiller)) value = (int)(value * 1.1f);

                // Draw normal price
                xPosition += 80;
                yPosition += 6;
                DrawPrice(value, xPosition, yPosition, Tools.Quality.Normal);
                // Draw silver price
                yPosition += 46;
                DrawPrice(value, xPosition, yPosition, Tools.Quality.Silver);
                // Draw gold price
                yPosition += 46;
                DrawPrice(value, xPosition, yPosition, Tools.Quality.Gold);
                // Draw iridium price
                if (isSapling)
                {
                    yPosition += 48;
                    DrawPrice(value, xPosition, yPosition, Tools.Quality.Iridium);
                }


                // Found out what this was for: Redraw the tooltip so it doesn't get overlapped by harvest price
                var hoverText = _helper.Reflection.GetField<String>(menu, "hoverText").GetValue();
                var hoverTitle = _helper.Reflection.GetField<String>(menu, "boldTitleText").GetValue();
                var getHoveredItemExtraItemIndex = _helper.Reflection.GetMethod(menu, "getHoveredItemExtraItemIndex");
                var getHoveredItemExtraItemAmount = _helper.Reflection.GetMethod(menu, "getHoveredItemExtraItemAmount");
                IClickableMenu.drawToolTip(
                    Game1.spriteBatch,
                    hoverText,
                    hoverTitle,
                    hoverItem,
                    menu.heldItem != null,
                    -1,
                    menu.currency,
                    getHoveredItemExtraItemIndex.Invoke<int>(new object[0]),
                    getHoveredItemExtraItemAmount.Invoke<int>(new object[0]),
                    null,
                    menu.hoverPrice);
            }
        }

        private static void DrawPrice(int price, int xPosition, int yPosition, Tools.Quality quality = Tools.Quality.Normal)
        {
            // Tree Icon
            Game1.spriteBatch.Draw(
                Game1.mouseCursors,
                new Vector2(xPosition, yPosition),
                new Rectangle(60, 428, 10, 10),
                Color.White,
                0,
                Vector2.Zero,
                Game1.pixelZoom,
                SpriteEffects.None,
                0.85f);
            //  Coin
            Game1.spriteBatch.Draw(
                Game1.debrisSpriteSheet,
                new Vector2(xPosition + 32, yPosition + 10),
                Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16),
                Color.White,
                0,
                new Vector2(8, 8),
                4,
                SpriteEffects.None,
                0.95f);
            // Star
            DrawQualityStar(quality, xPosition, yPosition + 22);
            // Price
            Utility.drawTextWithShadow(
                Game1.spriteBatch,
                "    " + Tools.AdjustPriceForQuality(price,quality),
                Game1.dialogueFont,
                new Vector2(xPosition, yPosition + 2),
                Game1.textColor
                );
        }

        private static void DrawQualityStar(Tools.Quality quality, int xPosition, int yPosition)
        {
            int iconX;
            int iconY;
            switch (quality)
            {
                case Tools.Quality.Silver:
                    iconX = 338;
                    iconY = 400;
                    break;
                case Tools.Quality.Gold:
                    iconX = 346;
                    iconY = 400;
                    break;
                case Tools.Quality.Iridium:
                    iconX = 346;
                    iconY = 392;
                    break;
                case Tools.Quality.Normal:
                default:
                    return;
            }
            Game1.spriteBatch.Draw(
                Game1.mouseCursors,
                new Vector2(xPosition, yPosition),
                new Rectangle(iconX, iconY, 8, 8),
                Color.White,
                0,
                Vector2.Zero,
                2.5f,
                SpriteEffects.None,
                0.95f);
        }
    }
}
