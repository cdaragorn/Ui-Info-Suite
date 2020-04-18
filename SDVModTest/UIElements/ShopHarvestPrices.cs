﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Reflection;
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
            // draw shop harvest prices
            if (Game1.activeClickableMenu is ShopMenu menu)
            {
                if (menu.hoveredItem is Item hoverItem)
                {
                    String text = string.Empty;
                    bool itemHasPriceInfo = Tools.GetTruePrice(hoverItem) > 0;

                    if (hoverItem is StardewValley.Object &&
                        (hoverItem as StardewValley.Object).Type == "Seeds" &&
                        itemHasPriceInfo &&
                        hoverItem.Name != "Mixed Seeds" &&
                        hoverItem.Name != "Winter Seeds")
                    {
                        StardewValley.Object temp = 
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
                        text = "    " + temp.Price;
                    }

                    var heldItem = menu.heldItem as Item;
                    if (heldItem == null)
                    {
                        int value = 0;
                        switch (hoverItem.ParentSheetIndex)
                        {
                            case 628: value = 50; break;
                            case 629: value = 80; break;
                            case 630:
                            case 633: value = 100; break;

                            case 631:
                            case 632: value = 140; break;
                        }

                        if (value > 0)
                            text = "    " + value;

                        if (text != "" &&
                            (hoverItem as StardewValley.Object).Type == "Seeds")
                        {
                            String textToRender = _helper.SafeGetString(
                                LanguageKeys.HarvestPrice);
                            int xPosition = menu.xPositionOnScreen - 30;
                            int yPosition = menu.yPositionOnScreen + 580;
                            IClickableMenu.drawTextureBox(
                                Game1.spriteBatch,
                                xPosition + 20,
                                yPosition - 52,
                                264,
                                108,
                                Color.White);
                            Game1.spriteBatch.DrawString(
                                Game1.dialogueFont,
                                textToRender,
                                new Vector2(xPosition + 30, yPosition - 38),
                                Color.Black * 0.2f);
                            Game1.spriteBatch.DrawString(
                                Game1.dialogueFont,
                                textToRender,
                                new Vector2(xPosition + 32, yPosition - 40),
                                Color.Black * 0.8f);
                            xPosition += 80;

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

                            Game1.spriteBatch.DrawString(
                                Game1.dialogueFont,
                                text,
                                new Vector2(xPosition - 2, yPosition + 6),
                                Color.Black * 0.2f);

                            Game1.spriteBatch.DrawString(
                                Game1.dialogueFont,
                                text,
                                new Vector2(xPosition, yPosition + 4),
                                Color.Black * 0.8f);
                            
                            String hoverText = _helper.Reflection.GetField<String>(menu, "hoverText").GetValue();
                            String hoverTitle = _helper.Reflection.GetField<String>(menu, "boldTitleText").GetValue();
                            int currency = _helper.Reflection.GetField<int>(menu, "currency").GetValue();
                            IReflectedMethod getHoveredItemExtraItemIndex = _helper.Reflection.GetMethod(menu, "getHoveredItemExtraItemIndex");
                            IReflectedMethod getHoveredItemExtraItemAmount = _helper.Reflection.GetMethod(menu, "getHoveredItemExtraItemAmount");

                            IClickableMenu.drawToolTip(
                                Game1.spriteBatch,
                                hoverText,
                                hoverTitle,
                                hoverItem,
                                heldItem != null,
                                -1,
                                currency,
                                getHoveredItemExtraItemIndex.Invoke<int>(new object[0]),
                                getHoveredItemExtraItemAmount.Invoke<int>(new object[0]),
                                null,
                                menu.hoverPrice);
                        }
                    }
                }
            }
        }
    }
}
