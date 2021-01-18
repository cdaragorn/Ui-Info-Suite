using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Text;
using UIInfoSuite.Infrastructure;
using UIInfoSuite.Infrastructure.Extensions;

namespace UIInfoSuite.UIElements
{
    class ShowCropAndBarrelTime : IDisposable
    {
        private readonly Dictionary<int, string> _indexOfCropNames = new Dictionary<int, string>();
        private readonly PerScreen<StardewValley.Object> _currentTile = new PerScreen<StardewValley.Object>();
        private readonly PerScreen<TerrainFeature> _terrain = new PerScreen<TerrainFeature>();
        private readonly PerScreen<Building> _currentTileBuilding = new PerScreen<Building>();
        private readonly IModHelper _helper;

        public ShowCropAndBarrelTime(IModHelper helper)
        {
            _helper = helper;
        }

        public void ToggleOption(bool showCropAndBarrelTimes)
        {
            _helper.Events.Display.RenderingHud -= OnRenderingHud;
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

            if (showCropAndBarrelTimes)
            {
                _helper.Events.Display.RenderingHud += OnRenderingHud;
                _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            }
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!e.IsMultipleOf(4))
                return;

            var gamepadTile = Game1.player.CurrentTool != null ? Utility.snapToInt(Game1.player.GetToolLocation()/Game1.tileSize) : Utility.snapToInt(Game1.player.GetGrabTile());
            var mouseTile = Game1.currentCursorTile;

            var tile = (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0) ? gamepadTile : mouseTile;

            // get tile under cursor
            _currentTileBuilding.Value = Game1.currentLocation is BuildableGameLocation buildableLocation
                ? buildableLocation.getBuildingAt(tile)
                : null;

            if (Game1.currentLocation != null)
            {
                if (Game1.currentLocation.Objects != null && Game1.currentLocation.Objects.TryGetValue(tile, out var currentObject))
                    _currentTile.Value = currentObject;
                else
                    _currentTile.Value = null;

                if (Game1.currentLocation.terrainFeatures != null && Game1.currentLocation.terrainFeatures.TryGetValue(tile, out var terrain))
                    _terrain.Value = terrain;
                else
                {
                    if (_currentTile.Value is IndoorPot pot && pot.hoeDirt.Value != null)
                        _terrain.Value = pot.hoeDirt.Value;
                    else
                        _terrain.Value = null;
                }
            }
            else
            {
                _currentTile.Value = null;
                _terrain.Value = null;
            }
        }

        public void Dispose()
        {
            ToggleOption(false);
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            var currentTileBuilding = _currentTileBuilding.Value;
            var currentTile = _currentTile.Value;
            var terrain = _terrain.Value;

            int overrideX = -1;
            int overrideY = -1;

            // draw hover tooltip
            if (currentTileBuilding != null && currentTileBuilding is Mill millBuilding && millBuilding.input.Value != null && !millBuilding.input.Value.isEmpty())
            {
                int wheatCount = 0;
                int beetCount = 0;

                foreach (Item item in millBuilding.input.Value.items)
                {
                    if (item != null &&
                        !string.IsNullOrEmpty(item.Name))
                    {
                        switch (item.Name)
                        {
                            case "Wheat": wheatCount = item.Stack; break;
                            case "Beet": beetCount = item.Stack; break;
                        }
                    }
                }

                StringBuilder builder = new StringBuilder();

                if (wheatCount > 0)
                    builder.Append(wheatCount + " wheat");

                if (beetCount > 0)
                {
                    if (wheatCount > 0)
                        builder.Append(Environment.NewLine);
                    builder.Append(beetCount + " beets");
                }

                if (builder.Length > 0)
                {
                    if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
                    {
                        var tilePosition = Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(new Vector2(currentTileBuilding.tileX.Value, currentTileBuilding.tileY.Value) * Game1.tileSize));
                        overrideX = (int)(tilePosition.X + Utility.ModifyCoordinateForUIScale(32));
                        overrideY = (int)(tilePosition.Y + Utility.ModifyCoordinateForUIScale(32));
                    }
                    
                    IClickableMenu.drawHoverText(
                        Game1.spriteBatch,
                        builder.ToString(),
                        Game1.smallFont, overrideX: overrideX, overrideY: overrideY);
                }
            }
            else if (currentTile != null &&
                (!currentTile.bigCraftable.Value ||
                currentTile.MinutesUntilReady > 0))
            {
                if (currentTile.bigCraftable.Value &&
                    currentTile.MinutesUntilReady > 0 &&
                    currentTile.heldObject.Value != null &&
                    currentTile.Name != "Heater")
                {
                    StringBuilder hoverText = new StringBuilder();

                    hoverText.AppendLine(currentTile.heldObject.Value.DisplayName);
                    
                    if (currentTile is Cask)
                    {
                        Cask currentCask = currentTile as Cask;
                        hoverText.Append((int)(currentCask.daysToMature.Value / currentCask.agingRate.Value))
                            .Append(" " + _helper.SafeGetString(
                            LanguageKeys.DaysToMature));
                    }
                    else
                    {
                        int timeLeft = currentTile.MinutesUntilReady;
                        int longTime = timeLeft / 60;
                        string longText = LanguageKeys.Hours;
                        int shortTime = timeLeft % 60;
                        string shortText = LanguageKeys.Minutes;

                        // 1600 minutes per day if you go to bed at 2am, more if you sleep early.
                        if (timeLeft >= 1600)
                        {
                            // Unlike crops and casks, this is only an approximate number of days
                            // because of how time works while sleeping. It's close enough though.
                            longText = LanguageKeys.Days;
                            longTime = timeLeft / 1600;

                            shortText = LanguageKeys.Hours;
                            shortTime = (timeLeft % 1600);

                            // Hours below 1200 are 60 minutes per hour. Overnight it's 100 minutes per hour.
                            // We could just divide by 60 here but then you could see strange times like
                            // "2 days, 25 hours".
                            // This is a bit of a fudge since depending on the current time of day and when the
                            // farmer goes to bed, the night might happen earlier or last longer, but it's just
                            // an approximation; regardless the processing won't finish before tomorrow.
                            if (shortTime <= 1200)
                                shortTime /= 60;
                            else
                                shortTime = 20 + (shortTime - 1200) / 100;
                        }

                        if (longTime > 0)
                            hoverText.Append(longTime).Append(" ")
                                .Append(_helper.SafeGetString(longText))
                                .Append(", ");

                        hoverText.Append(shortTime).Append(" ")
                            .Append(_helper.SafeGetString(shortText));
                    }

                    if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
                    {
                        var tilePosition = Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(new Vector2(currentTile.TileLocation.X, currentTile.TileLocation.Y) * Game1.tileSize));
                        overrideX = (int)(tilePosition.X + Utility.ModifyCoordinateForUIScale(32));
                        overrideY = (int)(tilePosition.Y + Utility.ModifyCoordinateForUIScale(32));
                    }

                    IClickableMenu.drawHoverText(
                        Game1.spriteBatch,
                        hoverText.ToString(),
                        Game1.smallFont, overrideX: overrideX, overrideY: overrideY);
                }
            }
            else if (terrain != null)
            {
                if (terrain is HoeDirt)
                {
                    HoeDirt hoeDirt = terrain as HoeDirt;
                    if (hoeDirt.crop != null &&
                        !hoeDirt.crop.dead.Value)
                    {
                        int num = 0;

                        if (hoeDirt.crop.fullyGrown.Value &&
                            hoeDirt.crop.dayOfCurrentPhase.Value > 0)
                        {
                            num = hoeDirt.crop.dayOfCurrentPhase.Value;
                        }
                        else
                        {
                            for (int i = 0; i < hoeDirt.crop.phaseDays.Count - 1; ++i)
                            {
                                if (hoeDirt.crop.currentPhase.Value == i)
                                    num -= hoeDirt.crop.dayOfCurrentPhase.Value;

                                if (hoeDirt.crop.currentPhase.Value <= i)
                                    num += hoeDirt.crop.phaseDays[i];
                            }
                        }

                        if (hoeDirt.crop.indexOfHarvest.Value > 0)
                        {
                            string hoverText = _indexOfCropNames.SafeGet(hoeDirt.crop.indexOfHarvest.Value);
                            if (string.IsNullOrEmpty(hoverText))
                            {
                                hoverText = new StardewValley.Object(new Debris(hoeDirt.crop.indexOfHarvest.Value, Vector2.Zero, Vector2.Zero).chunkType.Value, 1).DisplayName;
                                _indexOfCropNames.Add(hoeDirt.crop.indexOfHarvest.Value, hoverText);
                            }

                            StringBuilder finalHoverText = new StringBuilder();
                            finalHoverText.Append(hoverText).Append(": ");
                            if (num > 0)
                            {
                                finalHoverText.Append(num).Append(" ")
                                    .Append(_helper.SafeGetString(
                                        LanguageKeys.Days));
                            }
                            else
                            {
                                finalHoverText.Append(_helper.SafeGetString(
                                    LanguageKeys.ReadyToHarvest));
                            }

                            if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
                            {
                                var tilePosition = Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(new Vector2(terrain.currentTileLocation.X, terrain.currentTileLocation.Y) * Game1.tileSize));
                                overrideX = (int)(tilePosition.X + Utility.ModifyCoordinateForUIScale(32));
                                overrideY = (int)(tilePosition.Y + Utility.ModifyCoordinateForUIScale(32));
                            }

                            IClickableMenu.drawHoverText(
                                Game1.spriteBatch,
                                finalHoverText.ToString(),
                                Game1.smallFont, overrideX: overrideX, overrideY: overrideY);
                        }
                    }
                }
                else if (terrain is FruitTree)
                {
                    FruitTree tree = terrain as FruitTree;
                    var text = new StardewValley.Object(new Debris(tree.indexOfFruit.Value, Vector2.Zero, Vector2.Zero).chunkType.Value, 1).DisplayName;
                    if (tree.daysUntilMature.Value > 0)
                    {
                        text += Environment.NewLine + tree.daysUntilMature.Value + " " +
                                _helper.SafeGetString(
                                    LanguageKeys.DaysToMature);

                    }

                    if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
                    {
                        var tilePosition = Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(new Vector2(terrain.currentTileLocation.X, terrain.currentTileLocation.Y) * Game1.tileSize));
                        overrideX = (int)(tilePosition.X + Utility.ModifyCoordinateForUIScale(32));
                        overrideY = (int)(tilePosition.Y + Utility.ModifyCoordinateForUIScale(32));
                    }

                    IClickableMenu.drawHoverText(
                            Game1.spriteBatch,
                            text,
                            Game1.smallFont, overrideX: overrideX, overrideY: overrideY);
                }
            }
        }
    }
}
