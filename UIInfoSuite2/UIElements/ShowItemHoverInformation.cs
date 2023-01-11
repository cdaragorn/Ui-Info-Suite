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

            if (_hoverItem.Value != null
                && !(_hoverItem.Value is StardewValley.Tools.MeleeWeapon weapon && weapon.isScythe())
                && !(_hoverItem.Value is StardewValley.Tools.FishingRod))
            {
                var hoveredObject = _hoverItem.Value as StardewValley.Object;

                int itemPrice = Tools.GetSellToStorePrice(_hoverItem.Value);

                int stackPrice = 0;
                if (itemPrice > 0 && _hoverItem.Value.Stack > 1)
                    stackPrice = itemPrice * _hoverItem.Value.Stack;
                
                int cropPrice = Tools.GetHarvestPrice(_hoverItem.Value);

                bool notDonatedYet = _libraryMuseum.isItemSuitableForDonation(_hoverItem.Value);


                bool notShippedYet = (hoveredObject != null
                    && hoveredObject.countsForShippedCollection()
                    && !Game1.player.basicShipped.ContainsKey(hoveredObject.ParentSheetIndex)
                    && hoveredObject.Type != "Fish");
                if (notShippedYet && hoveredObject != null && ModEntry.DGA.IsCustomObject(hoveredObject, out var dgaHelper))
                {
                    // NB For DGA items, Game1.player.basicShipped.ContainsKey(hoveredObject.ParentSheetIndex) will always be false
                    //    and Object.countsForShippedCollection bypasses the type and category checks
                    try
                    {
                        // For shipping, DGA patches:
                        // - CollectionsPage()
                        // - ShippingMenu.parseItems()
                        // - StardewValley.Object.countsForShippedCollection()
                        // - StardewValley.Object.isIndexOkForBasicShippedCategory()
                        // But it doesn't patch Utility.getFarmerItemsShippedPercent() which determines if the requirements for the achievement are met.
                        // This means that DGA items do not (yet) count for the "Full Shipment" achievement even though they appear in the collections page.
                        
                        // Nonetheless, show the icon if that item is still hidden in the collections page.
                        int dgaId = dgaHelper.GetDgaObjectFakeId(hoveredObject);
                        string t = hoveredObject.Type;
                        bool inCollectionsPage = !(t.Contains("Arch") || t.Contains("Fish") || t.Contains("Mineral") || t.Contains("Cooking"))
                            && StardewValley.Object.isPotentialBasicShippedCategory(dgaId, hoveredObject.Category.ToString());
                            
                        notShippedYet = inCollectionsPage && !Game1.player.basicShipped.ContainsKey(dgaId);
                    }
                    catch (Exception e)
                    {
                        ModEntry.MonitorObject.LogOnce($"An error occured while checking if the DGA item {hoveredObject.Name} has been shipped.", LogLevel.Error);
                        ModEntry.MonitorObject.Log(e.ToString(), LogLevel.Debug);
                    }
                }

                string? requiredBundleName = null;
                // Bundle items must be "small" objects. This avoids marking other kinds of objects as needed, such as Chest (id 130), Recycling Machine (id 20), etc...
                if (hoveredObject != null && !hoveredObject.bigCraftable.Value && hoveredObject is not Furniture)
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

                int windowWidth;

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
                int windowX = Game1.getMouseX() - 25 - windowWidth;

                // Adjust the tooltip's position when it overflows
                var safeArea = Utility.getSafeArea();

                if (windowY + windowHeight > safeArea.Bottom)
                    windowY = safeArea.Bottom - windowHeight;

                if (Game1.getMouseX() + 300 > safeArea.Right)
                    windowX = safeArea.Right - 350 - windowWidth;
                else if (windowX < safeArea.Left)
                    windowX = Game1.getMouseX() + 350;

                Point windowPos = new Point(windowX, windowY);
                Vector2 currentDrawPos = new Vector2(windowPos.X + 30, windowPos.Y + 40);

                if (itemPrice > 0 || stackPrice > 0 || cropPrice > 0 || !String.IsNullOrEmpty(requiredBundleName) || notDonatedYet || notShippedYet)
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
                }

                if (itemPrice > 0)
                {
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
                }

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

                if (notDonatedYet)
                {
                    _museumIcon.bounds.X = windowPos.X - 30;
                    _museumIcon.bounds.Y = windowPos.Y - 60 + windowHeight;
                    _museumIcon.scale = 2;
                    _museumIcon.draw(Game1.spriteBatch);
                }

                if (!string.IsNullOrEmpty(requiredBundleName))
                {
                    int num1 = windowPos.X - 30;
                    int num2 = windowPos.Y - 14;
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

                if (notShippedYet)
                {
                        int num1 = windowPos.X + windowWidth - 66;
                        int num2 = windowPos.Y - 27;

                        _shippingBottomIcon.bounds.X = num1;
                        _shippingBottomIcon.bounds.Y = num2 - 8;
                        _shippingBottomIcon.scale = 1.2f;
                        _shippingBottomIcon.draw(Game1.spriteBatch);

                        _shippingTopIcon.bounds.X = num1;
                        _shippingTopIcon.bounds.Y = num2;
                        _shippingTopIcon.scale = 1.2f;
                        _shippingTopIcon.draw(Game1.spriteBatch);
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
    }
}
