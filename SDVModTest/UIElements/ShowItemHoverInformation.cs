using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UIInfoSuite.Extensions;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UIInfoSuite.UIElements
{
    class ShowItemHoverInformation : IDisposable
    {
        private readonly Dictionary<String, List<int>> _prunedRequiredBundles = new Dictionary<string, List<int>>();
        private readonly ClickableTextureComponent _bundleIcon = 
            new ClickableTextureComponent(
                "", 
                new Rectangle(0, 0, Game1.tileSize, Game1.tileSize), 
                "", 
                Game1.content.LoadString("Strings\\UI:GameMenu_JunimoNote_Hover", new object[0]), 
                Game1.mouseCursors, 
                new Rectangle(331, 374, 15, 14), 
                Game1.pixelZoom);

        private Item _hoverItem;
        private CommunityCenter _communityCenter;
        private Dictionary<String, String> _bundleData;

        public void ToggleOption(bool showItemHoverInformation)
        {
            PlayerEvents.InventoryChanged -= PopulateRequiredBundles;
            GraphicsEvents.OnPostRenderEvent -= DrawAdvancedTooltipForMenu;
            GraphicsEvents.OnPostRenderHudEvent -= DrawAdvancedTooltipForToolbar;
            GraphicsEvents.OnPreRenderEvent -= GetHoverItem;

            if (showItemHoverInformation)
            {
                _communityCenter = Game1.getLocationFromName("CommunityCenter") as CommunityCenter;
                _bundleData = Game1.content.Load<Dictionary<String, String>>("Data\\Bundles");
                PopulateRequiredBundles(null, null);
                PlayerEvents.InventoryChanged += PopulateRequiredBundles;
                GraphicsEvents.OnPostRenderEvent += DrawAdvancedTooltipForMenu;
                GraphicsEvents.OnPostRenderHudEvent += DrawAdvancedTooltipForToolbar;
                GraphicsEvents.OnPreRenderEvent += GetHoverItem;
            }
        }

        public void Dispose()
        {
            ToggleOption(false);
        }

        private void GetHoverItem(object sender, EventArgs e)
        {
            for (int i = 0; i < Game1.onScreenMenus.Count; ++i)
            {
                Toolbar onScreenMenu = Game1.onScreenMenus[i] as Toolbar;
                if (onScreenMenu != null)
                {
                    FieldInfo hoverItemField = typeof(Toolbar).GetField("hoverItem", BindingFlags.Instance | BindingFlags.NonPublic);
                    _hoverItem = hoverItemField.GetValue(onScreenMenu) as Item;
                    hoverItemField.SetValue(onScreenMenu, null);
                }
            }

            if (Game1.activeClickableMenu is GameMenu)
            {
                List<IClickableMenu> menuList = typeof(GameMenu).GetField("pages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Game1.activeClickableMenu) as List<IClickableMenu>;
                foreach (var menu in menuList)
                {
                    if (menu is InventoryPage)
                    {
                        FieldInfo hoveredItemField = typeof(InventoryPage).GetField("hoveredItem", BindingFlags.Instance | BindingFlags.NonPublic);
                        _hoverItem = hoveredItemField.GetValue(menu) as Item;
                        typeof(InventoryPage).GetField("hoverText", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(menu, "");
                    }
                }
            }

            if (Game1.activeClickableMenu is ItemGrabMenu)
            {
                _hoverItem = (Game1.activeClickableMenu as MenuWithInventory).hoveredItem;
                (Game1.activeClickableMenu as MenuWithInventory).hoveredItem = null;
            }
        }

        private void DrawAdvancedTooltipForToolbar(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu == null)
            {
                DrawAdvancedTooltip(sender, e);
            }
        }

        private void DrawAdvancedTooltipForMenu(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu != null)
            {
                DrawAdvancedTooltip(sender, e);
            }
        }

        private void PopulateRequiredBundles(object sender, EventArgsInventoryChanged e)
        {
            _prunedRequiredBundles.Clear();
            foreach (var bundle in _bundleData)
            {
                String[] bundleRoomInfo = bundle.Key.Split('/');
                String bundleRoom = bundleRoomInfo[0];
                int roomNum;

                switch(bundleRoom)
                {
                    case "Pantry": roomNum = 0; break;
                    case "Crafts Room": roomNum = 1; break;
                    case "Fish Tank": roomNum = 2; break;
                    case "Boiler Room": roomNum = 3; break;
                    case "Vault": roomNum = 4; break;
                    case "Bulletin Board": roomNum = 5; break;
                    default: continue;
                }

                if (_communityCenter.shouldNoteAppearInArea(roomNum))
                {
                    int bundleNumber = bundleRoomInfo[1].SafeParseInt32();
                    string[] bundleInfo = bundle.Value.Split('/');
                    string bundleName = bundleInfo[0];
                    string[] bundleValues = bundleInfo[2].Split(' ');
                    List<int> source = new List<int>();

                    for (int i = 0; i < bundleValues.Length; i += 3)
                    {
                        int bundleValue = bundleValues[i].SafeParseInt32();
                        if (bundleValue != -1 &&
                            !_communityCenter.bundles[bundleNumber][i / 3])
                        {
                            source.Add(bundleValue);
                        }
                    }

                    if (source.Count > 0)
                        _prunedRequiredBundles.Add(bundleName, source);
                }
            }
        }

        private void DrawAdvancedTooltip(object sender, EventArgs e)
        {
            if (_hoverItem != null)
            {
                String text = string.Empty;
                String extra = string.Empty;
                int truePrice = Tools.GetTruePrice(_hoverItem);
                int itemPrice = truePrice / 2;
                int stackPrice = 0;

                if (truePrice > 0 &&
                    _hoverItem.Name != "Scythe")
                {
                    int width = (int)Game1.smallFont.MeasureString(" ").Length();
                    int numberOfSpaces = 46 / ((int)Game1.smallFont.MeasureString(" ").Length()) + 1;
                    StringBuilder spaces = new StringBuilder();
                    for (int i = 0; i < numberOfSpaces; ++i)
                    {
                        spaces.Append(" ");
                    }
                    text = "\n" + spaces.ToString() + (truePrice / 2);
                    if (_hoverItem.getStack() > 1)
                    {
                        stackPrice = (truePrice / 2 * _hoverItem.getStack());
                        text += " (" + (truePrice / 2 * _hoverItem.getStack()) + ")";
                    }
                }
                int cropPrice = 0;

                bool flag = false;
                if (_hoverItem is StardewValley.Object && 
                    (_hoverItem as StardewValley.Object).type == "Seeds" &&
                    text != string.Empty &&
                    (_hoverItem.Name != "Mixed Seeds" ||
                    _hoverItem.Name != "Winter Seeds"))
                {
                    StardewValley.Object itemObject = new StardewValley.Object(new Debris(new Crop(_hoverItem.parentSheetIndex, 0, 0).indexOfHarvest, Game1.player.position, Game1.player.position).chunkType, 1);
                    extra += "    " + itemObject.Price;
                    cropPrice = itemObject.Price;
                    flag = true;
                }

                String hoverTile = _hoverItem.DisplayName + text + extra;
                String description = _hoverItem.getDescription();
                Vector2 vector2 = DrawTooltip(Game1.spriteBatch, _hoverItem.getDescription(), hoverTile, _hoverItem);
                vector2.X += 30;
                vector2.Y -= 10;



                if (text != "")
                {
                    int largestTextWidth = 0;
                    int stackTextWidth = (int)(Game1.smallFont.MeasureString(stackPrice.ToString()).Length());
                    int itemTextWidth = (int)(Game1.smallFont.MeasureString(itemPrice.ToString()).Length());
                    largestTextWidth = (stackTextWidth > itemTextWidth) ? stackTextWidth : itemTextWidth;
                    int windowWidth = largestTextWidth + 90;

                    Vector2 windowPos = new Vector2(Game1.getMouseX() - windowWidth - 25, Game1.getMouseY() + 20);

                    int windowHeight = 65;

                    if (stackPrice > 0)
                        windowHeight += 60;

                    if (cropPrice > 0)
                        windowHeight += 60;

                    IClickableMenu.drawTextureBox(
                        Game1.spriteBatch,
                        Game1.menuTexture,
                        new Rectangle(0, 256, 60, 60),
                        (int)windowPos.X,
                        (int)windowPos.Y,
                        windowWidth,
                        windowHeight,
                        Color.White);

                    windowPos.X += 30;
                    windowPos.Y += 30;

                    Game1.spriteBatch.Draw(
                        Game1.debrisSpriteSheet,
                        new Vector2(windowPos.X, windowPos.Y + 4),
                        Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16),
                        Color.White,
                        0,
                        new Vector2(8, 8),
                        Game1.pixelZoom,
                        SpriteEffects.None,
                        0.95f);

                    //batch.DrawString(
                    //    Game1.dialogueFont,
                    //    boldTitleText,
                    //    new Vector2(xPos + Game1.tileSize / 4, yPos + Game1.tileSize / 4 + 4) + new Vector2(0, 2),
                    //    Game1.textShadowColor);

                    //batch.DrawString(
                    //    Game1.dialogueFont,
                    //    boldTitleText,
                    //    new Vector2(xPos + Game1.tileSize / 4, yPos + Game1.tileSize / 4 + 4),
                    //    Game1.textColor);

                    Game1.spriteBatch.DrawString(
                        Game1.dialogueFont,
                        itemPrice.ToString(),
                        new Vector2(windowPos.X + 20, windowPos.Y - 20 + 2),
                        Game1.textShadowColor);

                    Game1.spriteBatch.DrawString(
                        Game1.dialogueFont,
                        itemPrice.ToString(),
                        new Vector2(windowPos.X + 20, windowPos.Y - 20),
                        Game1.textColor);

                    windowPos.Y += 50;

                    if (stackPrice > 0)
                    {
                        Game1.spriteBatch.Draw(
                            Game1.debrisSpriteSheet,
                            new Vector2(windowPos.X, windowPos.Y),
                            Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16),
                            Color.White,
                            0,
                            new Vector2(8, 8),
                            Game1.pixelZoom,
                            SpriteEffects.None,
                            0.95f);

                        Game1.spriteBatch.Draw(
                            Game1.debrisSpriteSheet,
                            new Vector2(windowPos.X, windowPos.Y + 10),
                            Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16),
                            Color.White,
                            0,
                            new Vector2(8, 8),
                            Game1.pixelZoom,
                            SpriteEffects.None,
                            0.95f);

                        Game1.spriteBatch.DrawString(
                            Game1.dialogueFont,
                            stackPrice.ToString(),
                            new Vector2(windowPos.X + 20, windowPos.Y - 20 + 2),
                            Game1.textShadowColor);

                        Game1.spriteBatch.DrawString(
                            Game1.dialogueFont,
                            stackPrice.ToString(),
                            new Vector2(windowPos.X + 20, windowPos.Y - 20),
                            Game1.textColor);

                        windowPos.Y += 50;
                    }

                    //Game1.spriteBatch.Draw(
                    //    Game1.debrisSpriteSheet,
                    //    new Vector2(vector2.X, vector2.Y),
                    //    Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16),
                    //    Color.White,
                    //    0,
                    //    new Vector2(8, 8),
                    //    Game1.pixelZoom,
                    //    SpriteEffects.None,
                    //    0.95f);

                    if (flag)
                    {
                        Game1.spriteBatch.Draw(
                            Game1.mouseCursors, new Vector2(vector2.X + Game1.dialogueFont.MeasureString(text).X - 10.0f, vector2.Y - 20f), 
                            new Rectangle(60, 428, 10, 10), 
                            Color.White, 
                            0.0f, 
                            Vector2.Zero, 
                            Game1.pixelZoom, 
                            SpriteEffects.None, 
                            0.85f);

                        Game1.spriteBatch.Draw(
                            Game1.mouseCursors, 
                            new Vector2(windowPos.X, windowPos.Y),
                            new Rectangle(60, 428, 10, 10),
                            Color.White,
                            0.0f,
                            new Vector2(8, 8),
                            Game1.pixelZoom,
                            SpriteEffects.None,
                            0.85f);
                    }
                }

                foreach (var requiredBundle in _prunedRequiredBundles)
                {
                    if (requiredBundle.Value.Contains(_hoverItem.parentSheetIndex) &&
                        !_hoverItem.Name.Contains("arecrow"))
                    {
                        int num1 = (int)vector2.X - 64;
                        int num2 = (int)vector2.Y - 110;
                        int num3 = num1 + 52;
                        int y3 = num2 - 2;
                        int num4 = 288;
                        int height = 36;
                        int num5 = 36;
                        int width = num4 / num5;
                        int num6 = 6;

                        for (int i = 0; i < 36; ++i)
                        {
                            float num7 = (float)(i >= num6 ? 0.92 - (i - num6) * (1.0 / (num5 - num6)) : 0.92f);
                            Game1.spriteBatch.Draw(
                                Game1.staminaRect,
                                new Rectangle(num3 + width * i, y3, width, height),
                                Color.Crimson * num7);
                        }

                        Game1.spriteBatch.DrawString(
                            Game1.dialogueFont,
                            requiredBundle.Key,
                            new Vector2(num1 + 72, num2),
                            Color.White);

                        _bundleIcon.bounds.X = num1 + 16;
                        _bundleIcon.bounds.Y = num2;
                        _bundleIcon.scale = 3;
                        _bundleIcon.draw(Game1.spriteBatch);
                        break;
                    }
                }
                RestoreMenuState();
            }
        }

        private void RestoreMenuState()
        {
            if (Game1.activeClickableMenu is ItemGrabMenu)
            {
                (Game1.activeClickableMenu as MenuWithInventory).hoveredItem = _hoverItem;
            }
        }


        private static Vector2 DrawTooltip(SpriteBatch batch, String hoverText, String hoverTitle, Item hoveredItem)
        {
            bool flag = hoveredItem != null &&
                hoveredItem is StardewValley.Object &&
                (hoveredItem as StardewValley.Object).edibility != -300;

            int healAmmountToDisplay = flag ? (hoveredItem as StardewValley.Object).edibility : -1;
            string[] buffIconsToDisplay = null;
            if (flag)
            {
                String objectInfo = Game1.objectInformation[(hoveredItem as StardewValley.Object).ParentSheetIndex];
                if (Game1.objectInformation[(hoveredItem as StardewValley.Object).parentSheetIndex].Split('/').Length >= 7)
                {
                    buffIconsToDisplay = Game1.objectInformation[(hoveredItem as StardewValley.Object).parentSheetIndex].Split('/')[6].Split('^');
                }
            }

            return DrawHoverText(batch, hoverText, Game1.smallFont, -1, -1, -1, hoverTitle, -1, buffIconsToDisplay, hoveredItem);
        }

        private static Vector2 DrawHoverText(SpriteBatch batch, String text, SpriteFont font, int xOffset = 0, int yOffset = 0, int moneyAmountToDisplayAtBottom = -1, String boldTitleText = null, int healAmountToDisplay = -1, string[] buffIconsToDisplay = null, Item hoveredItem = null)
        {
            Vector2 result = Vector2.Zero;

            if (String.IsNullOrEmpty(text))
            {
                result = Vector2.Zero;
            }
            else
            {
                if (String.IsNullOrEmpty(boldTitleText))
                    boldTitleText = null;

                int num1 = 20;
                int infoWindowWidth = (int)Math.Max(healAmountToDisplay != -1 ? font.MeasureString(healAmountToDisplay.ToString() + "+ Energy" + (Game1.tileSize / 2)).X : 0, Math.Max(font.MeasureString(text).X, boldTitleText != null ? Game1.dialogueFont.MeasureString(boldTitleText).X : 0)) + Game1.tileSize / 2;
                int extraInfoBackgroundHeight = (int)Math.Max(
                    num1 * 3, 
                    font.MeasureString(text).Y + Game1.tileSize / 2 + (moneyAmountToDisplayAtBottom > -1 ? (font.MeasureString(string.Concat(moneyAmountToDisplayAtBottom)).Y + 4.0) : 0) + (boldTitleText != null ? Game1.dialogueFont.MeasureString(boldTitleText).Y + (Game1.tileSize / 4) : 0) + (healAmountToDisplay != -1 ? 38 : 0));
                if (buffIconsToDisplay != null)
                {
                    for (int i = 0; i < buffIconsToDisplay.Length; ++i)
                    {
                        if (!buffIconsToDisplay[i].Equals("0"))
                            extraInfoBackgroundHeight += 34;
                    }
                    extraInfoBackgroundHeight += 4;
                }

                String categoryName = null;
                if (hoveredItem != null)
                {
                    extraInfoBackgroundHeight += (Game1.tileSize + 4) * hoveredItem.attachmentSlots();
                    categoryName = hoveredItem.getCategoryName();
                    if (categoryName.Length > 0)
                        extraInfoBackgroundHeight += (int)font.MeasureString("T").Y;

                    if (hoveredItem is MeleeWeapon)
                    {
                        extraInfoBackgroundHeight = (int)(Math.Max(
                            num1 * 3, 
                            (boldTitleText != null ? 
                                Game1.dialogueFont.MeasureString(boldTitleText).Y + (Game1.tileSize / 4) 
                                : 0) + 
                            Game1.tileSize / 2) +  
                            font.MeasureString("T").Y + 
                            (moneyAmountToDisplayAtBottom > -1 ? 
                                font.MeasureString(string.Concat(moneyAmountToDisplayAtBottom)).Y + 4.0 
                                : 0) + 
                            (hoveredItem as MeleeWeapon).getNumberOfDescriptionCategories() * 
                            Game1.pixelZoom * 12 + 
                            font.MeasureString(Game1.parseText((hoveredItem as MeleeWeapon).Description, 
                            Game1.smallFont, 
                            Game1.tileSize * 4 + 
                            Game1.tileSize / 4)).Y);

                        infoWindowWidth = (int)Math.Max(infoWindowWidth, font.MeasureString("99-99 Damage").X + (15 * Game1.pixelZoom) + (Game1.tileSize / 2));
                    }
                    else if (hoveredItem is Boots)
                    {
                        Boots hoveredBoots = hoveredItem as Boots;
                        extraInfoBackgroundHeight = extraInfoBackgroundHeight - (int)font.MeasureString(text).Y + (int)(hoveredBoots.getNumberOfDescriptionCategories() * Game1.pixelZoom * 12 + font.MeasureString(Game1.parseText(hoveredBoots.description, Game1.smallFont, Game1.tileSize * 4 + Game1.tileSize / 4)).Y);
                        infoWindowWidth = (int)Math.Max(infoWindowWidth, font.MeasureString("99-99 Damage").X + (15 * Game1.pixelZoom) + (Game1.tileSize / 2));
                    }
                    else if (hoveredItem is StardewValley.Object &&
                        (hoveredItem as StardewValley.Object).edibility != -300)
                    {
                        StardewValley.Object hoveredObject = hoveredItem as StardewValley.Object;
                        healAmountToDisplay = (int)Math.Ceiling(hoveredObject.Edibility * 2.5) + hoveredObject.quality * hoveredObject.Edibility;
                        extraInfoBackgroundHeight += (Game1.tileSize / 2 + Game1.pixelZoom * 2) * (healAmountToDisplay > 0 ? 2 : 1);
                    }
                }

                //Crafting ingredients were never used

                int xPos = Game1.getOldMouseX() + Game1.tileSize / 2 + xOffset;
                int yPos = Game1.getOldMouseY() + Game1.tileSize / 2 + yOffset;

                if (xPos + infoWindowWidth > Game1.viewport.Width)
                {
                    xPos = Game1.viewport.Width - infoWindowWidth;
                    yPos += Game1.tileSize / 4;
                }

                if (yPos + extraInfoBackgroundHeight > Game1.viewport.Height)
                {
                    xPos += Game1.tileSize / 4;
                    yPos = Game1.viewport.Height - extraInfoBackgroundHeight;
                }
                int hoveredItemHeight = (int)(hoveredItem == null || categoryName.Length <= 0 ? 0 : font.MeasureString("asd").Y);

                IClickableMenu.drawTextureBox(
                    batch,
                    Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    xPos,
                    yPos,
                    infoWindowWidth,
                    extraInfoBackgroundHeight,
                    Color.White);

                if (boldTitleText != null)
                {
                    IClickableMenu.drawTextureBox(
                        batch,
                        Game1.menuTexture,
                        new Rectangle(0, 256, 60, 60),
                        xPos,
                        yPos,
                        infoWindowWidth,
                        (int)(Game1.dialogueFont.MeasureString(boldTitleText).Y + Game1.tileSize / 2 + hoveredItemHeight - Game1.pixelZoom),
                        Color.White,
                        1,
                        false);

                    batch.Draw(
                        Game1.menuTexture,
                        new Rectangle(xPos + Game1.pixelZoom * 3, yPos + (int)Game1.dialogueFont.MeasureString(boldTitleText).Y + Game1.tileSize / 2 + hoveredItemHeight - Game1.pixelZoom, infoWindowWidth - Game1.pixelZoom * 6, Game1.pixelZoom),
                        new Rectangle(44, 300, 4, 4),
                        Color.White);

                    batch.DrawString(
                        Game1.dialogueFont,
                        boldTitleText,
                        new Vector2(xPos + Game1.tileSize / 4, yPos + Game1.tileSize / 4 + 4) + new Vector2(2, 2),
                        Game1.textShadowColor);

                    batch.DrawString(
                        Game1.dialogueFont,
                        boldTitleText,
                        new Vector2(xPos + Game1.tileSize / 4, yPos + Game1.tileSize / 4 + 4) + new Vector2(0, 2),
                        Game1.textShadowColor);

                    batch.DrawString(
                        Game1.dialogueFont,
                        boldTitleText,
                        new Vector2(xPos + Game1.tileSize / 4, yPos + Game1.tileSize / 4 + 4),
                        Game1.textColor);

                    yPos += (int)Game1.dialogueFont.MeasureString(boldTitleText).Y;
                }

                int yPositionToReturn = yPos;
                if (hoveredItem != null && categoryName.Length > 0)
                {
                    yPos -= 4;
                    Utility.drawTextWithShadow(
                        batch,
                        categoryName,
                        font,
                        new Vector2(xPos + Game1.tileSize / 4, yPos + Game1.tileSize / 4 + 4), 
                        hoveredItem.getCategoryColor(), 
                        1, 
                        -1, 
                        2, 
                        2);
                    yPos += (int)(font.MeasureString("T").Y + (boldTitleText != null ? Game1.tileSize / 4 : 0) + Game1.pixelZoom);
                }
                else
                {
                    yPos += (boldTitleText != null ? Game1.tileSize / 4 : 0);
                }

                if (hoveredItem is Boots)
                {
                    Boots boots = hoveredItem as Boots;
                    Utility.drawTextWithShadow(
                        batch,
                        Game1.parseText(
                            boots.description,
                            Game1.smallFont,
                            Game1.tileSize * 4 + Game1.tileSize / 4),
                        font,
                        new Vector2(xPos + Game1.tileSize / 4, yPos + Game1.tileSize / 4 + 4),
                        Game1.textColor);

                    yPos += (int)font.MeasureString(
                        Game1.parseText(
                            boots.description, 
                            Game1.smallFont, 
                            Game1.tileSize * 4 + Game1.tileSize / 4)).Y;

                    if (boots.defenseBonus > 0)
                    {
                        Utility.drawWithShadow(
                            batch,
                            Game1.mouseCursors,
                            new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 4),
                            new Rectangle(110, 428, 10, 10),
                            Color.White,
                            0,
                            Vector2.Zero,
                            Game1.pixelZoom);

                        Utility.drawTextWithShadow(
                            batch,
                            Game1.content.LoadString("Strings\\UI:ItemHover_DefenseBonus", new object[] { boots.defenseBonus }),
                            font,
                            new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 13, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3),
                            Game1.textColor * 0.9f);
                        yPos += (int)Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);
                    }

                    if (boots.immunityBonus > 0)
                    {
                        Utility.drawWithShadow(
                            batch,
                            Game1.mouseCursors,
                            new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 4),
                            new Rectangle(150, 428, 10, 10),
                            Color.White,
                            0,
                            Vector2.Zero,
                            Game1.pixelZoom);
                        Utility.drawTextWithShadow(
                            batch,
                            Game1.content.LoadString("Strings\\UI:ItemHover_ImmunityBonus", new object[] { boots.immunityBonus }),
                            font,
                            new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 13, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3),
                            Game1.textColor * 0.9f);

                        yPos += (int)Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);
                    }
                }
                else if (hoveredItem is MeleeWeapon)
                {
                    MeleeWeapon meleeWeapon = hoveredItem as MeleeWeapon;
                    Utility.drawTextWithShadow(
                        batch,
                        Game1.parseText(meleeWeapon.Description, Game1.smallFont, Game1.tileSize * 4 + Game1.tileSize / 4),
                        font,
                        new Vector2(xPos + Game1.tileSize / 4, yPos + Game1.tileSize / 4 + 4),
                        Game1.textColor);
                    yPos += (int)font.MeasureString(Game1.parseText(meleeWeapon.Description, Game1.smallFont, Game1.tileSize * 4 + Game1.tileSize / 4)).Y;

                    if ((meleeWeapon as Tool).indexOfMenuItemView != 47)
                    {
                        Utility.drawWithShadow(
                            batch,
                            Game1.mouseCursors,
                            new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 4),
                            new Rectangle(120, 428, 10, 10),
                            Color.White,
                            0,
                            Vector2.Zero,
                            Game1.pixelZoom);

                        Utility.drawTextWithShadow(
                            batch,
                            Game1.content.LoadString("Strings\\UI:ItemHover_Damage", new object[] { meleeWeapon.minDamage, meleeWeapon.maxDamage }),
                            font,
                            new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 13, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3),
                            Game1.textColor * 0.9f);
                        yPos += (int)Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);

                        if (meleeWeapon.speed != (meleeWeapon.type == 2 ? -8 : 0))
                        {
                            Utility.drawWithShadow(
                                batch,
                                Game1.mouseCursors,
                                new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 4),
                                new Rectangle(130, 428, 10, 10),
                                Color.White,
                                0,
                                Vector2.Zero,
                                Game1.pixelZoom,
                                false,
                                1);
                            bool flag = meleeWeapon.type == 2 ? meleeWeapon.speed < -8 : meleeWeapon.speed < 0;
                            String speedText = ((meleeWeapon.type == 2 ? meleeWeapon.speed + 8 : meleeWeapon.speed) / 2).ToString();
                            Utility.drawTextWithShadow(
                                batch,
                                Game1.content.LoadString("Strings\\UI:ItemHover_Speed", new object[] { (meleeWeapon.speed > 0 ? "+" : "") + speedText }),
                                font,
                                new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 13, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3),
                                flag ? Color.DarkRed : Game1.textColor * 0.9f);
                            yPos += (int)Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);
                        }

                        if (meleeWeapon.addedDefense > 0)
                        {
                            Utility.drawWithShadow(
                                batch,
                                Game1.mouseCursors,
                                new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 4),
                                new Rectangle(110, 428, 10, 10),
                                Color.White,
                                0.0f,
                                Vector2.Zero,
                                Game1.pixelZoom,
                                false, 
                                1f);
                            Utility.drawTextWithShadow(
                                batch, 
                                Game1.content.LoadString("Strings\\UI:ItemHover_DefenseBonus", new object[] { meleeWeapon.addedDefense }), 
                                font, 
                                new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 13, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3), 
                                Game1.textColor * 0.9f);
                            yPos += (int)Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);
                        }

                        if (meleeWeapon.critChance / 0.02 >= 2.0)
                        {
                            Utility.drawWithShadow(
                                batch, 
                                Game1.mouseCursors, 
                                new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 4), 
                                new Rectangle(40, 428, 10, 10), 
                                Color.White, 
                                0.0f, 
                                Vector2.Zero, 
                                Game1.pixelZoom, 
                                false, 
                                1f);
                            Utility.drawTextWithShadow(
                                batch, Game1.content.LoadString("Strings\\UI:ItemHover_CritChanceBonus", new object[] { meleeWeapon.critChance / 0.02 }), 
                                font, 
                                new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 13, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3), 
                                Game1.textColor * 0.9f);
                            yPos += (int)Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);
                        }

                        if (((double)meleeWeapon.critMultiplier - 3.0) / 0.02 >= 1.0)
                        {
                            Utility.drawWithShadow(
                                batch, 
                                Game1.mouseCursors, 
                                new Vector2(xPos + Game1.tileSize / 4, yPos + Game1.tileSize / 4 + 4), 
                                new Rectangle(160, 428, 10, 10), 
                                Color.White, 
                                0.0f, 
                                Vector2.Zero, 
                                Game1.pixelZoom, 
                                false, 
                                1f);

                            Utility.drawTextWithShadow(
                                batch, Game1.content.LoadString("Strings\\UI:ItemHover_CritPowerBonus", new object[] { (int)((meleeWeapon.critMultiplier - 3.0) / 0.02) }), 
                                font, 
                                new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 11, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3), 
                                Game1.textColor * 0.9f);
                            yPos += (int)Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);
                        }

                        if (meleeWeapon.knockback != meleeWeapon.defaultKnockBackForThisType(meleeWeapon.type))
                        {
                            Utility.drawWithShadow(
                                batch, 
                                Game1.mouseCursors, 
                                new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 4), 
                                new Rectangle(70, 428, 10, 10), 
                                Color.White, 
                                0.0f, 
                                Vector2.Zero, Game1.pixelZoom, 
                                false, 
                                1f);
                            Utility.drawTextWithShadow(
                                batch, 
                                Game1.content.LoadString(
                                    "Strings\\UI:ItemHover_Weight", 
                                    new object[] { meleeWeapon.knockback > meleeWeapon.defaultKnockBackForThisType(meleeWeapon.type) ? "+" : "" + Math.Ceiling(Math.Abs(meleeWeapon.knockback - meleeWeapon.defaultKnockBackForThisType(meleeWeapon.type) * 10.0)) }), 
                                font, 
                                new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 13, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3), 
                                Game1.textColor * 0.9f);
                            yPos += (int)Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);
                        }
                    }

                }
                else if (text.Length > 1)
                {
                    int textXPos = xPos + Game1.tileSize / 4;
                    int textYPos = yPos + Game1.tileSize / 4 + 4;
                    batch.DrawString(
                        font,
                        text,
                        new Vector2(textXPos, textYPos) + new Vector2(2, 2),
                        Game1.textShadowColor);

                    batch.DrawString(
                        font,
                        text,
                        new Vector2(textXPos, textYPos) + new Vector2(0, 2),
                        Game1.textShadowColor);

                    batch.DrawString(
                        font,
                        text,
                        new Vector2(textXPos, textYPos) + new Vector2(2, 0),
                        Game1.textShadowColor);

                    batch.DrawString(
                        font,
                        text,
                        new Vector2(textXPos, textYPos),
                        Game1.textColor * 0.9f);

                    yPos += (int)font.MeasureString(text).Y + 4;
                }

                if (healAmountToDisplay != -1)
                {
                    Utility.drawWithShadow(
                        batch, 
                        Game1.mouseCursors, 
                        new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4), 
                        new Rectangle(healAmountToDisplay < 0 ? 140 : 0, 428, 10, 10), 
                        Color.White, 
                        0.0f, 
                        Vector2.Zero, 
                        3f, 
                        false, 
                        0.95f);
                    Utility.drawTextWithShadow(
                        batch, Game1.content.LoadString("Strings\\UI:ItemHover_Energy", new object[] { ((healAmountToDisplay > 0 ? "+" : "") + healAmountToDisplay) }), 
                        font, 
                        new Vector2(xPos + Game1.tileSize / 4 + 34 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 8), 
                        Game1.textColor);
                    yPos += 34;

                    if (healAmountToDisplay > 0)
                    {
                        Utility.drawWithShadow(
                            batch,
                            Game1.mouseCursors,
                            new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4),
                            new Rectangle(0, 438, 10, 10),
                            Color.White,
                            0,
                            Vector2.Zero,
                            3,
                            false,
                            0.95f);

                        Utility.drawTextWithShadow(
                            batch,
                            Game1.content.LoadString(
                                "Strings\\UI:ItemHover_Health",
                                new object[] { "+" + (healAmountToDisplay * 0.4) }),
                            font,
                            new Vector2(xPos + Game1.tileSize / 4 + 34 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 8),
                            Game1.textColor);

                        yPos += 34;
                    }
                }

                if (buffIconsToDisplay != null)
                {
                    for (int i = 0; i < buffIconsToDisplay.Length; ++i)
                    {
                        String buffIcon = buffIconsToDisplay[i];
                        if (buffIcon != "0")
                        {
                            Utility.drawWithShadow(
                                batch,
                                Game1.mouseCursors,
                                new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4),
                                new Rectangle(10 + i * 10, 428, 10, 10),
                                Color.White,
                                0, Vector2.Zero,
                                3,
                                false,
                                0.95f);

                            string textToDraw = (buffIcon.SafeParseInt32() > 0 ? "+" : string.Empty) + buffIcon + " ";

                            //if (i <= 10)
                            //    textToDraw = Game1.content.LoadString("Strings\\UI:ItemHover_Buff" + i, new object[] { textToDraw });

                            Utility.drawTextWithShadow(
                                batch,
                                textToDraw,
                                font,
                                new Vector2(xPos + Game1.tileSize / 4 + 34 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 8),
                                Game1.textColor);
                            yPos += 34;
                        }
                    }
                }

                if (hoveredItem != null &&
                    hoveredItem.attachmentSlots() > 0)
                {
                    yPos += 16;
                    hoveredItem.drawAttachments(batch, xPos + Game1.tileSize / 4, yPos);
                    if (moneyAmountToDisplayAtBottom > -1)
                        yPos += Game1.tileSize * hoveredItem.attachmentSlots();
                }

                if (moneyAmountToDisplayAtBottom > -1)
                {

                }

                result = new Vector2(xPos, yPositionToReturn);
            }

            return result;
        }
    }
}
