using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UIInfoSuite.Extensions;
using StardewConfigFramework;

namespace UIInfoSuite.UIElements
{
    class ShopHarvestPrices: IDisposable
    {
        private readonly IModHelper _helper;
        private readonly ModOptionToggle _shopHarvestPrices;

        public ShopHarvestPrices(ModOptions modOptions, IModHelper helper)
        {
            _helper = helper;

            _shopHarvestPrices = modOptions.GetOptionWithIdentifier<ModOptionToggle>(OptionKeys.ShowHarvestPricesInShop) ?? new ModOptionToggle(OptionKeys.ShowHarvestPricesInShop, "Show shop harvest prices");
            _shopHarvestPrices.ValueChanged += ToggleOption;
            modOptions.AddModOption(_shopHarvestPrices);

            ToggleOption(_shopHarvestPrices.identifier, _shopHarvestPrices.IsOn);
        }

        public void ToggleOption(string identifier, bool shopHarvestPrices)
        {
            if (identifier != OptionKeys.ShowHarvestPricesInShop)
                return;

            GraphicsEvents.OnPostRenderGuiEvent -= DrawShopHarvestPrices;

            if (shopHarvestPrices)
            {
                GraphicsEvents.OnPostRenderGuiEvent += DrawShopHarvestPrices;
            }
        }

        public void Dispose()
        {
            ToggleOption(OptionKeys.ShowHarvestPricesInShop, false);
        }

        private void DrawShopHarvestPrices(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu is ShopMenu)
            {
                ShopMenu menu = Game1.activeClickableMenu as ShopMenu;
                Item hoverItem = typeof(ShopMenu).GetField("hoveredItem", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu) as Item;

                if (hoverItem != null)
                {
                    String text = string.Empty;
                    bool itemHasPriceInfo = Tools.GetTruePrice(hoverItem) > 0;

                    if (hoverItem is StardewValley.Object &&
                            (hoverItem as StardewValley.Object).type == "Seeds" &&
                            itemHasPriceInfo &&
                            hoverItem.Name != "Mixed Seeds" &&
                            hoverItem.Name != "Winter Seeds")
                    {
                        StardewValley.Object temp =
                                new StardewValley.Object(
                                        new Debris(
                                                new Crop(
                                                        hoverItem.parentSheetIndex,
                                                        0,
                                                        0)
                                                        .indexOfHarvest,
                                                Game1.player.position,
                                                Game1.player.position).chunkType,
                                        1);
                        text = "    " + temp.price;
                    }

                    Item heldItem = typeof(ShopMenu).GetField("heldItem", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu) as Item;
                    if (heldItem == null)
                    {
                        int value = 0;
                        switch (hoverItem.parentSheetIndex)
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
                                (hoverItem as StardewValley.Object).type == "Seeds")
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

                            String hoverText = _helper.Reflection.GetPrivateField<String>(menu, "hoverText").GetValue();
                            String hoverTitle = _helper.Reflection.GetPrivateField<String>(menu, "boldTitleText").GetValue();
                            Item hoverItem2 = _helper.Reflection.GetPrivateField<Item>(menu, "hoveredItem").GetValue();
                            int currency = _helper.Reflection.GetPrivateField<int>(menu, "currency").GetValue();
                            int hoverPrice = _helper.Reflection.GetPrivateField<int>(menu, "hoverPrice").GetValue();
                            IPrivateMethod getHoveredItemExtraItemIndex = _helper.Reflection.GetPrivateMethod(menu, "getHoveredItemExtraItemIndex");
                            IPrivateMethod getHoveredItemExtraItemAmount = _helper.Reflection.GetPrivateMethod(menu, "getHoveredItemExtraItemAmount");

                            IClickableMenu.drawToolTip(
                                    Game1.spriteBatch,
                                    hoverText,
                                    hoverTitle,
                                    hoverItem2,
                                    heldItem != null,
                                    -1,
                                    currency,
                                    getHoveredItemExtraItemIndex.Invoke<int>(new object[0]),
                                    getHoveredItemExtraItemAmount.Invoke<int>(new object[0]),
                                    null,
                                    hoverPrice);
                        }
                    }
                }
            }
        }
    }
}
