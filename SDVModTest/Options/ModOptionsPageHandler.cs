﻿using UIInfoSuite.UIElements;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Reflection;
using UIInfoSuite.Extensions;

namespace UIInfoSuite.Options
{
    class ModOptionsPageHandler : IDisposable
    {
        private List<ModOptionsElement> _optionsElements = new List<ModOptionsElement>();
        private readonly List<IDisposable> _elementsToDispose;
        private readonly IDictionary<string, string> _options;
        private ModOptionsPageButton _modOptionsPageButton;
        private ModOptionsPage _modOptionsPage;
        private readonly IModHelper _helper;

        private int _modOptionsTabPageNumber;

        private readonly LuckOfDay _luckOfDay;
        private readonly ShowBirthdayIcon _showBirthdayIcon;
        private readonly ShowAccurateHearts _showAccurateHearts;
        private readonly LocationOfTownsfolk _locationOfTownsfolk;
        private readonly ShowWhenAnimalNeedsPet _showWhenAnimalNeedsPet;
        private readonly ShowCalendarAndBillboardOnGameMenuButton _showCalendarAndBillboardOnGameMenuButton;
        private readonly ShowCropAndBarrelTime _showCropAndBarrelTime;
        private readonly ShowItemEffectRanges _showScarecrowAndSprinklerRange;
        private readonly ExperienceBar _experienceBar;
        private readonly ItemHoverInformation _showItemHoverInformation;
        private readonly ShowTravelingMerchant _showTravelingMerchant;
        private readonly ShopHarvestPrices _shopHarvestPrices;
        private readonly ShowQueenOfSauceIcon _showQueenOfSauceIcon;
        private readonly ShowToolUpgradeStatus _showToolUpgradeStatus;
        private readonly ShowRobinBuildingStatusIcon _showRobinBuildingStatusIcon;

        public ModOptionsPageHandler(IModHelper helper, IDictionary<string, string> options)
        {
            _options = options;
            helper.Events.Display.MenuChanged += ToggleModOptions;
            _helper = helper;

            _luckOfDay = new LuckOfDay(helper);
            _showBirthdayIcon = new ShowBirthdayIcon(helper);
            _showAccurateHearts = new ShowAccurateHearts(helper.Events);
            _locationOfTownsfolk = new LocationOfTownsfolk(helper, _options);
            _showWhenAnimalNeedsPet = new ShowWhenAnimalNeedsPet(helper);
            _showCalendarAndBillboardOnGameMenuButton = new ShowCalendarAndBillboardOnGameMenuButton(helper);
            _showScarecrowAndSprinklerRange = new ShowItemEffectRanges(helper.Events);
            _experienceBar = new ExperienceBar(helper);
            _showItemHoverInformation = new ItemHoverInformation(helper.Events);
            _shopHarvestPrices = new ShopHarvestPrices(helper);
            _showQueenOfSauceIcon = new ShowQueenOfSauceIcon(helper);
            _showTravelingMerchant = new ShowTravelingMerchant(helper);
            _showCropAndBarrelTime = new ShowCropAndBarrelTime(helper);
            _showToolUpgradeStatus = new ShowToolUpgradeStatus(helper);
            _showRobinBuildingStatusIcon = new ShowRobinBuildingStatusIcon(helper);

            _elementsToDispose = new List<IDisposable>()
            {
                _luckOfDay,
                _showBirthdayIcon,
                _showAccurateHearts,
                _locationOfTownsfolk,
                _showWhenAnimalNeedsPet,
                _showCalendarAndBillboardOnGameMenuButton,
                _showCropAndBarrelTime,
                _experienceBar,
                _showItemHoverInformation,
                _showTravelingMerchant,
                _shopHarvestPrices,
                _showQueenOfSauceIcon,
                _showToolUpgradeStatus,
                _showRobinBuildingStatusIcon
            };

            var whichOption = 1;
            var thisVersion = Assembly.GetAssembly(GetType()).GetName().Version;
            _optionsElements.Add(new ModOptionsElement("UI Info Suite v" +
                thisVersion.Major + "." + thisVersion.Minor + "." + thisVersion.Build));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(OptionKeys.ShowLuckIcon), whichOption++, _luckOfDay.Toggle, _options, OptionKeys.ShowLuckIcon));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(OptionKeys.ShowLevelUpAnimation), whichOption++, _experienceBar.ToggleLevelUpAnimation, _options, OptionKeys.ShowLevelUpAnimation));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(OptionKeys.ShowExperienceBar), whichOption++, _experienceBar.ToggleShowExperienceBar, _options, OptionKeys.ShowExperienceBar));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(OptionKeys.AllowExperienceBarToFadeOut), whichOption++, _experienceBar.ToggleExperienceBarFade, _options, OptionKeys.AllowExperienceBarToFadeOut));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(OptionKeys.ShowExperienceGain), whichOption++, _experienceBar.ToggleShowExperienceGain, _options, OptionKeys.ShowExperienceGain));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(OptionKeys.ShowLocationOfTownsPeople), whichOption++, _locationOfTownsfolk.ToggleShowNPCLocationsOnMap, _options, OptionKeys.ShowLocationOfTownsPeople));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(OptionKeys.ShowBirthdayIcon), whichOption++, _showBirthdayIcon.ToggleOption, _options, OptionKeys.ShowBirthdayIcon));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(OptionKeys.ShowHeartFills), whichOption++, _showAccurateHearts.ToggleOption, _options, OptionKeys.ShowHeartFills));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(OptionKeys.ShowAnimalsNeedPets), whichOption++, _showWhenAnimalNeedsPet.ToggleOption, _options, OptionKeys.ShowAnimalsNeedPets));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(OptionKeys.DisplayCalendarAndBillboard), whichOption++, _showCalendarAndBillboardOnGameMenuButton.ToggleOption, _options, OptionKeys.DisplayCalendarAndBillboard));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(OptionKeys.ShowCropAndBarrelTooltip), whichOption++, _showCropAndBarrelTime.ToggleOption, _options, OptionKeys.ShowCropAndBarrelTooltip));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(OptionKeys.ShowItemEffectRanges), whichOption++, _showScarecrowAndSprinklerRange.ToggleOption, _options, OptionKeys.ShowItemEffectRanges));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(OptionKeys.ShowExtraItemInformation), whichOption++, _showItemHoverInformation.ToggleOption, _options, OptionKeys.ShowExtraItemInformation));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(OptionKeys.ShowTravelingMerchant), whichOption++, _showTravelingMerchant.ToggleOption, _options, OptionKeys.ShowTravelingMerchant));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(OptionKeys.ShowHarvestPricesInShop), whichOption++, _shopHarvestPrices.ToggleOption, _options, OptionKeys.ShowHarvestPricesInShop));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(OptionKeys.ShowWhenNewRecipesAreAvailable), whichOption++, _showQueenOfSauceIcon.ToggleOption, _options, OptionKeys.ShowWhenNewRecipesAreAvailable));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(OptionKeys.ShowToolUpgradeStatus), whichOption++, _showToolUpgradeStatus.ToggleOption, _options, OptionKeys.ShowToolUpgradeStatus));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(OptionKeys.ShowRobinBuildingStatusIcon), whichOption++, _showRobinBuildingStatusIcon.ToggleOption, _options, OptionKeys.ShowRobinBuildingStatusIcon));

        }


        public void Dispose()
        {
            foreach (var item in _elementsToDispose)
                item.Dispose();
        }

        private void OnButtonLeftClicked(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu
                && !GameMenu.forcePreventClose) // Don't activate when an action is being remapped
            {
                SetActiveClickableMenuToModOptionsPage();
                Game1.playSound("smallSelect");
            }
        }

        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ToggleModOptions(object sender, MenuChangedEventArgs e)
        {
            // remove from old menu
            if (e.OldMenu != null)
            {
                _helper.Events.Display.RenderedActiveMenu -= DrawButton;
                if (_modOptionsPageButton != null)
                    _modOptionsPageButton.OnLeftClicked -= OnButtonLeftClicked;

                if (e.OldMenu is GameMenu gameMenu)
                {
                    var tabPages = gameMenu.pages;
                    tabPages.Remove(_modOptionsPage);
                }
            }

            // add to new menu
            if (e.NewMenu is GameMenu newMenu)
            {
                if (_modOptionsPageButton == null)
                {
                    _modOptionsPage = new ModOptionsPage(_optionsElements, _helper.Events);
                    _modOptionsPageButton = new ModOptionsPageButton(_helper);
                }

                _helper.Events.Display.RenderedActiveMenu += DrawButton;
                _modOptionsPageButton.OnLeftClicked += OnButtonLeftClicked;
                var tabPages = newMenu.pages;

                _modOptionsTabPageNumber = tabPages.Count;
                tabPages.Add(_modOptionsPage);
            }
        }

        private void SetActiveClickableMenuToModOptionsPage()
        {
            if (Game1.activeClickableMenu is GameMenu menu)
                menu.currentTab = _modOptionsTabPageNumber;
        }

        private void DrawButton(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu gameMenu
                && gameMenu.currentTab != 3 // Don't render when the map is showing
                && !GameMenu.forcePreventClose) // Don't render when an action is being remapped
            {
                if (gameMenu.currentTab == _modOptionsTabPageNumber)
                {
                    _modOptionsPageButton.yPositionOnScreen = Game1.activeClickableMenu.yPositionOnScreen + 24;
                }
                else
                {
                    _modOptionsPageButton.yPositionOnScreen = Game1.activeClickableMenu.yPositionOnScreen + 16;
                }
                _modOptionsPageButton.draw(Game1.spriteBatch);

                //Might need to render hover text here
            }
        }
    }
}
