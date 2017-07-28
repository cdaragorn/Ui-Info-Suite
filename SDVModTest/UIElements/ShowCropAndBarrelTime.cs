using Microsoft.Xna.Framework;
using UIInfoSuite.Extensions;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Resources;
using System.Reflection;
using System.Globalization;
using StardewValley.Objects;
using StardewModdingAPI;
using StardewConfigFramework;

namespace UIInfoSuite.UIElements
{
    class ShowCropAndBarrelTime: IDisposable
    {
        private Dictionary<int, String> _indexOfCropNames = new Dictionary<int, string>();
        private StardewValley.Object _currentTile;
        private TerrainFeature _terrain;
        private readonly IModHelper _helper;
        private readonly ModOptionToggle _showCropAndBarrelTime;

        public ShowCropAndBarrelTime(ModOptions modOptions, IModHelper helper)
        {
            _helper = helper;

            _showCropAndBarrelTime = modOptions.GetOptionWithIdentifier<ModOptionToggle>(OptionKeys.ShowCropAndBarrelTooltip) ?? new ModOptionToggle(OptionKeys.ShowCropAndBarrelTooltip, "Show crop and barrel times");
            _showCropAndBarrelTime.ValueChanged += ToggleOption;
            modOptions.AddModOption(_showCropAndBarrelTime);

            ToggleOption(_showCropAndBarrelTime.identifier, _showCropAndBarrelTime.IsOn);

        }

        public void ToggleOption(string identifier, bool showCropAndBarrelTimes)
        {
            if (identifier != OptionKeys.ShowCropAndBarrelTooltip)
                return;

            GraphicsEvents.OnPreRenderHudEvent -= DrawHoverTooltip;
            GameEvents.FourthUpdateTick -= GetTileUnderCursor;

            if (showCropAndBarrelTimes)
            {
                GraphicsEvents.OnPreRenderHudEvent += DrawHoverTooltip;
                GameEvents.FourthUpdateTick += GetTileUnderCursor;
            }
        }

        private void GetTileUnderCursor(object sender, EventArgs e)
        {
            _currentTile = Game1.currentLocation.Objects.SafeGet(Game1.currentCursorTile);
            _terrain = Game1.currentLocation.terrainFeatures.SafeGet(Game1.currentCursorTile);
        }

        public void Dispose()
        {
            ToggleOption(OptionKeys.ShowCropAndBarrelTooltip, false);
        }

        private void DrawHoverTooltip(object sender, EventArgs e)
        {

            //StardewValley.Object tile = Game1.currentLocation.Objects.SafeGet(Game1.currentCursorTile);
            //TerrainFeature feature = null;

            if (_currentTile != null &&
                    (!_currentTile.bigCraftable ||
                    _currentTile.minutesUntilReady > 0))
            {
                if (_currentTile.bigCraftable &&
                        _currentTile.minutesUntilReady > 0 &&
                        _currentTile.Name != "Heater")
                {
                    StringBuilder hoverText = new StringBuilder();

                    if (_currentTile is Cask)
                    {
                        Cask currentCask = _currentTile as Cask;

                        hoverText.Append((int) (currentCask.daysToMature / currentCask.agingRate))
                                .Append(" " + _helper.SafeGetString(
                                LanguageKeys.DaysToMature));
                    }
                    else
                    {
                        int hours = _currentTile.minutesUntilReady / 60;
                        int minutes = _currentTile.minutesUntilReady % 60;
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
            else if (_terrain != null)
            {
                if (_terrain is HoeDirt)
                {
                    HoeDirt hoeDirt = _terrain as HoeDirt;
                    if (hoeDirt.crop != null &&
                            !hoeDirt.crop.dead)
                    {
                        int num = 0;

                        if (hoeDirt.crop.fullyGrown &&
                                hoeDirt.crop.dayOfCurrentPhase > 0)
                        {
                            num = hoeDirt.crop.dayOfCurrentPhase;
                        }
                        else
                        {
                            for (int i = 0; i < hoeDirt.crop.phaseDays.Count - 1; ++i)
                            {
                                if (hoeDirt.crop.currentPhase == i)
                                    num -= hoeDirt.crop.dayOfCurrentPhase;

                                if (hoeDirt.crop.currentPhase <= i)
                                    num += hoeDirt.crop.phaseDays[i];
                            }
                        }

                        if (hoeDirt.crop.indexOfHarvest > 0)
                        {
                            String hoverText = _indexOfCropNames.SafeGet(hoeDirt.crop.indexOfHarvest);
                            if (String.IsNullOrEmpty(hoverText))
                            {
                                hoverText = new StardewValley.Object(new Debris(hoeDirt.crop.indexOfHarvest, Vector2.Zero, Vector2.Zero).chunkType, 1).DisplayName;
                                _indexOfCropNames.Add(hoeDirt.crop.indexOfHarvest, hoverText);
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
                            IClickableMenu.drawHoverText(
                                    Game1.spriteBatch,
                                    finalHoverText.ToString(),
                                    Game1.smallFont);
                        }
                    }
                }
                else if (_terrain is FruitTree)
                {
                    FruitTree tree = _terrain as FruitTree;

                    if (tree.daysUntilMature > 0)
                    {
                        IClickableMenu.drawHoverText(
                                Game1.spriteBatch,
                                tree.daysUntilMature + " " +
                                        _helper.SafeGetString(
                                                LanguageKeys.DaysToMature),
                                Game1.smallFont);
                    }
                }
            }
        }
    }
}
