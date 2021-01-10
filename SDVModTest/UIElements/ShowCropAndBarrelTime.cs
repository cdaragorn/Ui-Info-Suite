using Microsoft.Xna.Framework;
using UIInfoSuite.Extensions;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Text;
using StardewValley.Objects;
using StardewModdingAPI;
using StardewValley.Locations;
using StardewValley.Buildings;

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

            // get tile under cursor
            _currentTileBuilding.Value = Game1.currentLocation is BuildableGameLocation buildableLocation
                ? buildableLocation.getBuildingAt(Game1.currentCursorTile)
                : null;

            if (Game1.currentLocation != null)
            {
                if (Game1.currentLocation.Objects != null && Game1.currentLocation.Objects.TryGetValue(Game1.currentCursorTile, out var currentTile))
                    _currentTile.Value = currentTile;
                else
                    _currentTile.Value = null;

                if (Game1.currentLocation.terrainFeatures != null && Game1.currentLocation.terrainFeatures.TryGetValue(Game1.currentCursorTile, out var terrain))
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
            // draw hover tooltip
            if (currentTileBuilding != null)
            {
                if (currentTileBuilding is Mill millBuilding)
                {
                    if (millBuilding.input.Value != null)
                    {
                        if (!millBuilding.input.Value.isEmpty())
                        {
                            var wheatCount = 0;
                            var beetCount = 0;

                            foreach (var item in millBuilding.input.Value.items)
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

                            var builder = new StringBuilder();

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
                                IClickableMenu.drawHoverText(
                                   Game1.spriteBatch,
                                   builder.ToString(),
                                   Game1.smallFont);
                            }
                        }
                    }
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
                    var hoverText = new StringBuilder();
                    hoverText.AppendLine(currentTile.heldObject.Value.DisplayName);
                    
                    if (currentTile is Cask)
                    {
                        var currentCask = currentTile as Cask;
                        hoverText.Append((int)(currentCask.daysToMature.Value / currentCask.agingRate.Value))
                            .Append(" " + _helper.SafeGetString(
                            LanguageKeys.DaysToMature));
                    }
                    else
                    {
                        var hours = currentTile.MinutesUntilReady / 60;
                        var minutes = currentTile.MinutesUntilReady % 60;
                        if (hours > 0)
                            hoverText.Append(hours).Append(" ")
                                .Append(_helper.SafeGetString(
                                    LanguageKeys.Hours))
                                .Append(", ");
                        hoverText.Append(minutes).Append(" ")
                            .Append(_helper.SafeGetString(
                                LanguageKeys.Minutes));
                    }
                    IClickableMenu.drawHoverText(
                        Game1.spriteBatch,
                        hoverText.ToString(),
                        Game1.smallFont);
                }
            }
            else if (terrain != null)
            {
                if (terrain is HoeDirt)
                {
                    var hoeDirt = terrain as HoeDirt;
                    if (hoeDirt.crop != null &&
                        !hoeDirt.crop.dead.Value)
                    {
                        var num = 0;

                        if (hoeDirt.crop.fullyGrown.Value &&
                            hoeDirt.crop.dayOfCurrentPhase.Value > 0)
                        {
                            num = hoeDirt.crop.dayOfCurrentPhase.Value;
                        }
                        else
                        {
                            for (var i = 0; i < hoeDirt.crop.phaseDays.Count - 1; ++i)
                            {
                                if (hoeDirt.crop.currentPhase.Value == i)
                                    num -= hoeDirt.crop.dayOfCurrentPhase.Value;

                                if (hoeDirt.crop.currentPhase.Value <= i)
                                    num += hoeDirt.crop.phaseDays[i];
                            }
                        }

                        if (hoeDirt.crop.indexOfHarvest.Value > 0)
                        {
                            var hoverText = _indexOfCropNames.SafeGet(hoeDirt.crop.indexOfHarvest.Value);
                            if (string.IsNullOrEmpty(hoverText))
                            {
                                hoverText = new StardewValley.Object(new Debris(hoeDirt.crop.indexOfHarvest.Value, Vector2.Zero, Vector2.Zero).chunkType.Value, 1).DisplayName;
                                _indexOfCropNames.Add(hoeDirt.crop.indexOfHarvest.Value, hoverText);
                            }

                            var finalHoverText = new StringBuilder();
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
                            IClickableMenu.drawHoverText(
                                Game1.spriteBatch,
                                finalHoverText.ToString(),
                                Game1.smallFont);
                        }
                    }
                }
                else if (terrain is FruitTree)
                {
                    var tree = terrain as FruitTree;
                    var text = new StardewValley.Object(new Debris(tree.indexOfFruit.Value, Vector2.Zero, Vector2.Zero).chunkType.Value, 1).DisplayName;
                    if (tree.daysUntilMature.Value > 0)
                    {
                        text += Environment.NewLine + tree.daysUntilMature.Value + " " +
                                _helper.SafeGetString(
                                    LanguageKeys.DaysToMature);

                    }
                    IClickableMenu.drawHoverText(
                            Game1.spriteBatch,
                            text,
                            Game1.smallFont);
                }
            }
        }
    }
}
