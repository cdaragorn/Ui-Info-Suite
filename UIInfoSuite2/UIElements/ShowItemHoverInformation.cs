using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.UIElements
{
    internal class ShowItemHoverInformation : IDisposable
    {
        private readonly Dictionary<string, List<KeyValuePair<int, int>>> _prunedRequiredBundles = new();
        private readonly ClickableTextureComponent _bundleIcon =
            new(
                "",
                new Rectangle(0, 0, Game1.tileSize, Game1.tileSize),
                "",
                Game1.content.LoadString("Strings\\UI:GameMenu_JunimoNote_Hover", new object[0]),
                Game1.mouseCursors,
                new Rectangle(331, 374, 15, 14),
                Game1.pixelZoom);
        private readonly ClickableTextureComponent _museumIcon;
        private readonly ClickableTextureComponent _shippingBottomIcon =
            new(
                "",
                new Rectangle(0, 0, Game1.tileSize, Game1.tileSize),
                "",
                "",
                Game1.mouseCursors,
                new Rectangle(526, 218, 30, 22),
                Game1.pixelZoom);
        private readonly ClickableTextureComponent _shippingTopIcon =
            new(
                "",
                new Rectangle(0, 0, Game1.tileSize, Game1.tileSize),
                "",
                "",
                Game1.mouseCursors,
                new Rectangle(134, 236, 30, 15),
                Game1.pixelZoom);

        private readonly PerScreen<Item> _hoverItem = new();
        private CommunityCenter _communityCenter;
        private LibraryMuseum _libraryMuseum;

        private Dictionary<string, string> _bundleNameOverrides;

        private readonly IModHelper _helper;

        public ShowItemHoverInformation(IModHelper helper)
        {
            _helper = helper;

            var gunther = Game1.getCharacterFromName("Gunther");
            if (gunther == null) {
                ModEntry.MonitorObject.Log($"{this.GetType().Name}: Could not find Gunther in the game, creating a fake one for ourselves.", LogLevel.Warn);
                gunther = new NPC() {
                    Name = "Gunther",
                    Age = 0,
                    Sprite = new AnimatedSprite("Characters\\Gunther"),
                };
            }

            _museumIcon = new(
                "",
                new Rectangle(0, 0, Game1.tileSize, Game1.tileSize),
                "",
                Game1.content.LoadString("Strings\\Locations:ArchaeologyHouse_Gunther_Donate", new object[0]),
                gunther.Sprite.Texture,
                gunther.GetHeadShot(),
                Game1.pixelZoom);
        }

        public void ToggleOption(bool showItemHoverInformation)
        {
            _helper.Events.GameLoop.DayStarted -= OnDayStarted;
            _helper.Events.Player.InventoryChanged -= OnInventoryChanged;
            _helper.Events.Display.Rendered -= OnRendered;
            _helper.Events.Display.RenderedHud -= OnRenderedHud;
            _helper.Events.Display.Rendering -= OnRendering;

            if (showItemHoverInformation)
            {
                _communityCenter = Game1.getLocationFromName("CommunityCenter") as CommunityCenter;

                _libraryMuseum = Game1.getLocationFromName("ArchaeologyHouse") as LibraryMuseum;

                _helper.Events.GameLoop.DayStarted += OnDayStarted;
                _helper.Events.Player.InventoryChanged += OnInventoryChanged;
                _helper.Events.Display.Rendered += OnRendered;
                _helper.Events.Display.RenderedHud += OnRenderedHud;
                _helper.Events.Display.Rendering += OnRendering;
            }
        }

        public void Dispose()
        {
            ToggleOption(false);
        }

        // [EventPriority(EventPriority.Low)]
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            // NB The Custom Community Center mod is only ready at this point
            if (_helper.GameContent.CurrentLocaleConstant == LocalizedContentManager.LanguageCode.en && _helper.ModRegistry.IsLoaded("blueberry.CustomCommunityCentre"))
                _bundleNameOverrides = GetEnglishNamesForCustomCommunityCenterBundles();
            else
                _bundleNameOverrides = null;
            PopulateRequiredBundles();
        }

        /// <summary>Raised before the game draws anything to the screen in a draw tick, as soon as the sprite batch is opened.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRendering(object sender, EventArgs e)
        {
            _hoverItem.Value = Tools.GetHoveredItem();
        }

        /// <summary>Raised after drawing the HUD (item toolbar, clock, etc) to the sprite batch, but before it's rendered to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open). Content drawn to the sprite batch at this point will appear over the HUD.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedHud(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu == null)
            {
                DrawAdvancedTooltip();
            }
        }

        /// <summary>Raised after the game draws to the sprite patch in a draw tick, just before the final sprite batch is rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRendered(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu != null)
            {
                DrawAdvancedTooltip();
            }
        }

        /// <summary>Raised after items are added or removed to a player's inventory. NOTE: this event is currently only raised for the current player.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnInventoryChanged(object sender, InventoryChangedEventArgs e)
        {
            if (e.IsLocalPlayer)
                this.PopulateRequiredBundles();
        }

        /// Retrieve English bundle names for Custom Community Center bundles as the bundle id is not the bundle's English name.
        /// For example, the bundle id is "Custom_blueberry_Kitchen_Area0_Bundle0" but the name is "Baker's"
        private Dictionary<string, string> GetEnglishNamesForCustomCommunityCenterBundles()
        {
            try
            {
                Dictionary<string, string> englishNames = new();
                var bundleNamesAsset = _helper.GameContent.Load<Dictionary<string, string>>(@"Strings\BundleNames");
                foreach (var namePair in bundleNamesAsset)
                {
                    if (namePair.Key != namePair.Value)
                        englishNames[namePair.Key] = namePair.Value;
                }
                return englishNames;
            }
            catch (Exception e)
            {
                ModEntry.MonitorObject.LogOnce("Failed to retrieve English names for Custom Community Center bundles. Custom bundle names may be displayed incorrectly.", LogLevel.Warn);
                ModEntry.MonitorObject.Log(e.ToString());
                return null;
            }
        }

        private void PopulateRequiredBundles()
        {
            _prunedRequiredBundles.Clear();
            if (!Game1.player.mailReceived.Contains("JojaMember"))
            {

                foreach (var bundle in Game1.netWorldState.Value.BundleData)
                {
                    string[] bundleRoomInfo = bundle.Key.Split('/');
                    string bundleRoom = bundleRoomInfo[0];
                    int areaNum = CommunityCenter.getAreaNumberFromName(bundleRoom);

                    if (_communityCenter.shouldNoteAppearInArea(areaNum))
                    {
                        int bundleNumber = bundleRoomInfo[1].SafeParseInt32();
                        string[] bundleInfo = bundle.Value.Split('/');
                        string bundleName = _helper.GameContent.CurrentLocaleConstant == LocalizedContentManager.LanguageCode.en || int.TryParse(bundleInfo[^1], out _)
                            ? bundleInfo[0]
                            : bundleInfo[^1];
                        string[] bundleValues = bundleInfo[2].Split(' ');
                        List<KeyValuePair<int, int>> source = new List<KeyValuePair<int, int>>();

                        for (int i = 0; i < bundleValues.Length; i += 3)
                        {
                            int bundleValue = bundleValues[i].SafeParseInt32();
                            int quality = bundleValues[i + 2].SafeParseInt32();
                            if (bundleValue != -1 &&
                                !_communityCenter.bundles[bundleNumber][i / 3])
                            {
                                source.Add(new KeyValuePair<int, int>(bundleValue, quality));
                            }
                        }

                        if (source.Count > 0)
                        {
                            if (_bundleNameOverrides != null && _bundleNameOverrides.TryGetValue(bundleName, out string value))
                                bundleName = value;
                            _prunedRequiredBundles.Add(bundleName, source);
                        }
                    }
                }
            }
        }

        //private Item _lastHoverItem;
        //private int lastStackSize;
        //private int lastItemPrice;
        //private int lastStackPrice;
        //private int lastCropPrice;
        //private int lastTruePrice;
        //private int lastWindowWidth;
        //private string lastRequiredBundleName;

        private void DrawAdvancedTooltip()
        {

            if (_hoverItem.Value != null &&
                _hoverItem.Value.Name != "Scythe" &&
              !(_hoverItem.Value is StardewValley.Tools.FishingRod))
            {
                int itemPrice = 0;
                int stackPrice = 0;
                int cropPrice = 0;
                int truePrice = Tools.GetTruePrice(_hoverItem.Value);
                int windowWidth;
                string requiredBundleName = null;

                //if (_lastHoverItem == null || _hoverItem.Value.Name != _lastHoverItem.Name || _hoverItem.Value.Stack != lastStackSize)
                //{
                if (truePrice > 0)
                {
                    itemPrice = truePrice / 2;

                    if (_hoverItem.Value.Stack > 1)
                    {
                        stackPrice = (itemPrice * _hoverItem.Value.Stack);
                    }
                }

                if (_hoverItem.Value is StardewValley.Object
                    && itemPrice > 0
                    && (_hoverItem.Value as StardewValley.Object).Type == "Seeds"
                    && (_hoverItem.Value.Name != "Mixed Seeds" || _hoverItem.Value.Name != "Winter Seeds"))
                {
                    StardewValley.Object itemObject = new StardewValley.Object(
                            new Debris(new Crop(_hoverItem.Value.ParentSheetIndex, 0, 0).indexOfHarvest.Value,
                            Game1.player.position,
                            Game1.player.position).chunkType.Value,
                            1);
                    cropPrice = itemObject.Price;
                }

                // Bundle items must be "small" objects. This avoids marking other kinds of objects as needed, such as Chest (id 130), Recycling Machine (id 20), etc...
                if (_hoverItem.Value is StardewValley.Object hoveredObject && !hoveredObject.bigCraftable.Value && hoveredObject is not Furniture)
                {
                    foreach (var requiredBundle in _prunedRequiredBundles)
                    {
                        if (requiredBundle.Value.Any(itemQuality => itemQuality.Key == hoveredObject.ParentSheetIndex && hoveredObject.Quality >= itemQuality.Value))
                        {
                            requiredBundleName = requiredBundle.Key;
                            break;
                        }
                    }
                }

                int bundleTextWidth = 0;
                if (!string.IsNullOrEmpty(requiredBundleName))
                {
                    bundleTextWidth = (int)Game1.dialogueFont.MeasureString(requiredBundleName).Length();
                    bundleTextWidth -= 30; //Text offset from left
                }
                int stackTextWidth = (int)(Game1.smallFont.MeasureString(stackPrice.ToString()).Length());
                int itemTextWidth = (int)(Game1.smallFont.MeasureString(itemPrice.ToString()).Length());
                int largestTextWidth = Math.Max(bundleTextWidth, Math.Max(stackTextWidth, itemTextWidth));
                windowWidth = largestTextWidth + 90;

                int windowHeight = 75;

                if (stackPrice > 0)
                    windowHeight += 40;

                if (cropPrice > 0)
                    windowHeight += 40;

                int windowY = Game1.getMouseY() + 20;
                windowY = Game1.viewport.Height - windowHeight - windowY < 0 ? Game1.viewport.Height - windowHeight : windowY;

                int windowX = Game1.getMouseX() - windowWidth - 25;

                if (Game1.getMouseX() > Game1.viewport.Width - 300)
                {
                    windowX = Game1.viewport.Width - windowWidth - 350;
                }
                else if (windowX < 0)
                {
                    windowX = Game1.getMouseX() + 350;
                }

                Vector2 windowPos = new Vector2(windowX, windowY);
                Vector2 currentDrawPos = new Vector2(windowPos.X + 30, windowPos.Y + 40);

                if (itemPrice > 0)
                {
                    IClickableMenu.drawTextureBox(
                        Game1.spriteBatch,
                        Game1.menuTexture,
                        new Rectangle(0, 256, 60, 60),
                        (int)windowPos.X,
                        (int)windowPos.Y,
                        windowWidth,
                        windowHeight,
                        Color.White);

                    Game1.spriteBatch.Draw(
                        Game1.debrisSpriteSheet,
                        new Vector2(currentDrawPos.X, currentDrawPos.Y + 4),
                        Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16),
                        Color.White,
                        0,
                        new Vector2(8, 8),
                        Game1.pixelZoom,
                        SpriteEffects.None,
                        0.95f);

                    Game1.spriteBatch.DrawString(
                        Game1.smallFont,
                        itemPrice.ToString(),
                        new Vector2(currentDrawPos.X + 22, currentDrawPos.Y - 8),
                        Game1.textShadowColor);

                    Game1.spriteBatch.DrawString(
                        Game1.smallFont,
                        itemPrice.ToString(),
                        new Vector2(currentDrawPos.X + 20, currentDrawPos.Y - 10),
                        Game1.textColor);

                    currentDrawPos.Y += 40;

                    if (stackPrice > 0)
                    {
                        Game1.spriteBatch.Draw(
                            Game1.debrisSpriteSheet,
                            new Vector2(currentDrawPos.X, currentDrawPos.Y),
                            Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16),
                            Color.White,
                            0,
                            new Vector2(8, 8),
                            Game1.pixelZoom,
                            SpriteEffects.None,
                            0.95f);

                        Game1.spriteBatch.Draw(
                            Game1.debrisSpriteSheet,
                            new Vector2(currentDrawPos.X, currentDrawPos.Y + 10),
                            Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16),
                            Color.White,
                            0,
                            new Vector2(8, 8),
                            Game1.pixelZoom,
                            SpriteEffects.None,
                            0.95f);

                        Game1.spriteBatch.DrawString(
                            Game1.smallFont,
                            stackPrice.ToString(),
                            new Vector2(currentDrawPos.X + 22, currentDrawPos.Y - 8),
                            Game1.textShadowColor);

                        Game1.spriteBatch.DrawString(
                            Game1.smallFont,
                            stackPrice.ToString(),
                            new Vector2(currentDrawPos.X + 20, currentDrawPos.Y - 10),
                            Game1.textColor);

                        currentDrawPos.Y += 40;
                    }

                    if (cropPrice > 0)
                    {

                        Game1.spriteBatch.Draw(
                            Game1.mouseCursors,
                            new Vector2(currentDrawPos.X - 15, currentDrawPos.Y - 10),
                            new Rectangle(60, 428, 10, 10),
                            Color.White,
                            0.0f,
                            Vector2.Zero,
                            Game1.pixelZoom * 0.75f,
                            SpriteEffects.None,
                            0.85f);

                        Game1.spriteBatch.DrawString(
                            Game1.smallFont,
                            cropPrice.ToString(),
                            new Vector2(currentDrawPos.X + 22, currentDrawPos.Y - 8),
                            Game1.textShadowColor);

                        Game1.spriteBatch.DrawString(
                            Game1.smallFont,
                            cropPrice.ToString(),
                            new Vector2(currentDrawPos.X + 20, currentDrawPos.Y - 10),
                            Game1.textColor);
                    }
                }

                if (_libraryMuseum.isItemSuitableForDonation(_hoverItem.Value))
                {
                    _museumIcon.bounds.X = (int)windowPos.X - 30;
                    _museumIcon.bounds.Y = (int)windowPos.Y - 60 + windowHeight;
                    _museumIcon.scale = 2;
                    _museumIcon.draw(Game1.spriteBatch);
                }

                if (!string.IsNullOrEmpty(requiredBundleName))
                {
                    int num1 = (int)windowPos.X - 30;
                    int num2 = (int)windowPos.Y - 14;
                    int num3 = num1 + 52;
                    int y3 = num2 + 4;
                    int height = 36;
                    int num5 = 36;
                    int width = windowWidth / num5;
                    int num6 = 6;

                    for (int i = 0; i < num5; ++i)
                    {
                        float num7 = (float)(i >= num6 ? 0.92 - (i - num6) * (1.0 / (num5 - num6)) : 0.92f);
                        Game1.spriteBatch.Draw(
                            Game1.staminaRect,
                            new Rectangle(num3 + width * i, y3, width, height),
                            Color.Crimson * num7);
                    }

                    Game1.spriteBatch.DrawString(
                        Game1.dialogueFont,
                        requiredBundleName,
                        new Vector2(num1 + 72, num2),
                        Color.White);

                    _bundleIcon.bounds.X = num1 + 16;
                    _bundleIcon.bounds.Y = num2 - 6;
                    _bundleIcon.scale = 3;
                    _bundleIcon.draw(Game1.spriteBatch);
                }

                if (_hoverItem.Value is StardewValley.Object obj)
                {
                    if (obj.countsForShippedCollection() && !Game1.player.basicShipped.ContainsKey(obj.ParentSheetIndex))
                    {
                        int num1 = (int)windowPos.X + windowWidth - 66;
                        int num2 = (int)windowPos.Y - 27;

                        _shippingBottomIcon.bounds.X = num1;
                        _shippingBottomIcon.bounds.Y = num2 - 8;
                        _shippingBottomIcon.scale = 1.2f;
                        _shippingBottomIcon.draw(Game1.spriteBatch);

                        _shippingTopIcon.bounds.X = num1;
                        _shippingTopIcon.bounds.Y = num2;
                        _shippingTopIcon.scale = 1.2f;
                        _shippingTopIcon.draw(Game1.spriteBatch);
                    }
                }

                //memorize the result to save processing time when calling again with same values
                //_lastHoverItem = (_lastHoverItem != _hoverItem.Value) ? _hoverItem.Value : _lastHoverItem;
                //lastItemPrice = (itemPrice != lastItemPrice) ? itemPrice : lastItemPrice;
                //lastCropPrice = (lastCropPrice != cropPrice) ? cropPrice : lastCropPrice;
                //lastStackPrice = (lastStackPrice != stackPrice) ? stackPrice : lastStackPrice;
                //lastTruePrice = (lastTruePrice != truePrice) ? truePrice : lastTruePrice;
                //lastWindowWidth = (lastWindowWidth != windowWidth) ? windowWidth : lastWindowWidth;
                //lastRequiredBundleName = (lastRequiredBundleName != requiredBundleName) ? requiredBundleName : lastRequiredBundleName;
                //lastStackSize = (_hoverItem.Value != null && lastStackSize != _hoverItem.Value.Stack) ? _hoverItem.Value.Stack : lastStackSize;
            }
        }

        private void RestoreMenuState()
        {
            if (Game1.activeClickableMenu is ItemGrabMenu)
            {
                (Game1.activeClickableMenu as MenuWithInventory).hoveredItem = _hoverItem.Value;
            }
        }


        private static Vector2 DrawTooltip(SpriteBatch batch, string hoverText, string hoverTitle, Item hoveredItem)
        {
            bool flag = hoveredItem != null &&
                hoveredItem is StardewValley.Object &&
                (hoveredItem as StardewValley.Object).Edibility != -300;

            int healAmmountToDisplay = flag ? (hoveredItem as StardewValley.Object).Edibility : -1;
            string[] buffIconsToDisplay = null;
            if (flag)
            {
                string objectInfo = Game1.objectInformation[(hoveredItem as StardewValley.Object).ParentSheetIndex];
                if (Game1.objectInformation[(hoveredItem as StardewValley.Object).ParentSheetIndex].Split('/').Length >= 7)
                {
                    buffIconsToDisplay = Game1.objectInformation[(hoveredItem as StardewValley.Object).ParentSheetIndex].Split('/')[6].Split('^');
                }
            }

            return DrawHoverText(batch, hoverText, Game1.smallFont, -1, -1, -1, hoverTitle, -1, buffIconsToDisplay, hoveredItem);
        }

        private static Vector2 DrawHoverText(SpriteBatch batch, string text, SpriteFont font, int xOffset = 0, int yOffset = 0, int moneyAmountToDisplayAtBottom = -1, string boldTitleText = null, int healAmountToDisplay = -1, string[] buffIconsToDisplay = null, Item hoveredItem = null)
        {
            Vector2 result;

            if (string.IsNullOrEmpty(text))
            {
                result = Vector2.Zero;
            }
            else
            {
                if (string.IsNullOrEmpty(boldTitleText))
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

                string categoryName = null;
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
                        (hoveredItem as StardewValley.Object).Edibility != -300)
                    {
                        StardewValley.Object hoveredObject = hoveredItem as StardewValley.Object;
                        healAmountToDisplay = (int)Math.Ceiling(hoveredObject.Edibility * 2.5) + hoveredObject.Quality * hoveredObject.Edibility;
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

                    if (boots.defenseBonus.Value > 0)
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
                            Game1.content.LoadString("Strings\\UI:ItemHover_DefenseBonus", new object[] { boots.defenseBonus.Value }),
                            font,
                            new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 13, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3),
                            Game1.textColor * 0.9f);
                        yPos += (int)Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);
                    }

                    if (boots.immunityBonus.Value > 0)
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
                            Game1.content.LoadString("Strings\\UI:ItemHover_ImmunityBonus", new object[] { boots.immunityBonus.Value }),
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

                    if (meleeWeapon.IndexOfMenuItemView != 47)
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
                            Game1.content.LoadString("Strings\\UI:ItemHover_Damage", new object[] { meleeWeapon.minDamage.Value, meleeWeapon.maxDamage.Value }),
                            font,
                            new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 13, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3),
                            Game1.textColor * 0.9f);
                        yPos += (int)Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);

                        if (meleeWeapon.speed.Value != (meleeWeapon.type.Value == 2 ? -8 : 0))
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
                            bool flag = meleeWeapon.type.Value == 2 ? meleeWeapon.speed.Value < -8 : meleeWeapon.speed.Value < 0;
                            string speedText = ((meleeWeapon.type.Value == 2 ? meleeWeapon.speed.Value + 8 : meleeWeapon.speed.Value) / 2).ToString();
                            Utility.drawTextWithShadow(
                                batch,
                                Game1.content.LoadString("Strings\\UI:ItemHover_Speed", new object[] { (meleeWeapon.speed.Value > 0 ? "+" : "") + speedText }),
                                font,
                                new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 13, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3),
                                flag ? Color.DarkRed : Game1.textColor * 0.9f);
                            yPos += (int)Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);
                        }

                        if (meleeWeapon.addedDefense.Value > 0)
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
                                Game1.content.LoadString("Strings\\UI:ItemHover_DefenseBonus", new object[] { meleeWeapon.addedDefense.Value }),
                                font,
                                new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 13, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3),
                                Game1.textColor * 0.9f);
                            yPos += (int)Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);
                        }

                        if (meleeWeapon.critChance.Value / 0.02 >= 2.0)
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
                                batch, Game1.content.LoadString("Strings\\UI:ItemHover_CritChanceBonus", new object[] { meleeWeapon.critChance.Value / 0.02 }),
                                font,
                                new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 13, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3),
                                Game1.textColor * 0.9f);
                            yPos += (int)Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);
                        }

                        if (((double)meleeWeapon.critMultiplier.Value - 3.0) / 0.02 >= 1.0)
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
                                batch, Game1.content.LoadString("Strings\\UI:ItemHover_CritPowerBonus", new object[] { (int)((meleeWeapon.critMultiplier.Value - 3.0) / 0.02) }),
                                font,
                                new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 11, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3),
                                Game1.textColor * 0.9f);
                            yPos += (int)Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);
                        }

                        if (meleeWeapon.knockback.Value != meleeWeapon.defaultKnockBackForThisType(meleeWeapon.type.Value))
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
                                    new object[] { meleeWeapon.knockback.Value > meleeWeapon.defaultKnockBackForThisType(meleeWeapon.type.Value) ? "+" : "" + Math.Ceiling(Math.Abs(meleeWeapon.knockback.Value - meleeWeapon.defaultKnockBackForThisType(meleeWeapon.type.Value) * 10.0)) }),
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
                        string buffIcon = buffIconsToDisplay[i];
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
