﻿using UIInfoSuite.UIElements;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UIInfoSuite.Extensions;

namespace UIInfoSuite.Options
{
    class ModOptionsPageHandler : IDisposable
    {
        private List<ModOptionsElement> _optionsElements = new List<ModOptionsElement>();
        private readonly List<IDisposable> _elementsToDispose;
        private readonly IDictionary<string, String> _options;
        private ModOptionsPageButton _modOptionsPageButton;
        private ModOptionsPage _modOptionsPage;
        private readonly IModHelper _helper;

        private int _modOptionsTabPageNumber;

        private readonly LuckOfDay _luckOfDay;
        private readonly ShowBirthdayIcon _showBirthdayIcon;
        private readonly ShowAccurateHearts _showAccurateHearts = new ShowAccurateHearts();
        private readonly LocationOfTownsfolk _locationOfTownsfolk;
        private readonly ShowWhenAnimalNeedsPet _showWhenAnimalNeedsPet;
        private readonly ShowCalendarAndBillboardOnGameMenuButton _showCalendarAndBillboardOnGameMenuButton;
        private readonly ShowCropAndBarrelTime _showCropAndBarrelTime;
        private readonly ShowItemEffectRanges _showScarecrowAndSprinklerRange;
        private readonly ExperienceBar _experienceBar;
        private readonly ShowItemHoverInformation _showItemHoverInformation = new ShowItemHoverInformation();
        private readonly ShowTravelingMerchant _showTravelingMerchant;
        private readonly ShopHarvestPrices _shopHarvestPrices;
        private readonly ShowQueenOfSauceIcon _showQueenOfSauceIcon;
        private readonly ShowToolUpgradeStatus _showToolUpgradeStatus;

        public ModOptionsPageHandler(IModHelper helper, IDictionary<String, String> options)
        {
            _options = options;
            MenuEvents.MenuChanged += AddModOptionsToMenu;
            MenuEvents.MenuClosed += RemoveModOptionsFromMenu;
            _helper = helper;
            ModConfig modConfig = _helper.ReadConfig<ModConfig>();
            _luckOfDay = new LuckOfDay(helper);
			_showBirthdayIcon = new ShowBirthdayIcon(helper);
            _locationOfTownsfolk = new LocationOfTownsfolk(_helper, _options);
            _showWhenAnimalNeedsPet = new ShowWhenAnimalNeedsPet(_helper);
            _showCalendarAndBillboardOnGameMenuButton = new ShowCalendarAndBillboardOnGameMenuButton(helper);
            _showScarecrowAndSprinklerRange = new ShowItemEffectRanges(modConfig);
            _experienceBar = new ExperienceBar(helper);
            _shopHarvestPrices = new ShopHarvestPrices(helper);
            _showQueenOfSauceIcon = new ShowQueenOfSauceIcon(helper);
            _showTravelingMerchant = new ShowTravelingMerchant(helper);
            _showCropAndBarrelTime = new ShowCropAndBarrelTime(helper);
            _showToolUpgradeStatus = new ShowToolUpgradeStatus(helper);

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
                _showToolUpgradeStatus
            };

            int whichOption = 1;
            Version thisVersion = Assembly.GetAssembly(this.GetType()).GetName().Version;
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

        }


        public void Dispose()
        {
            foreach (var item in _elementsToDispose)
                item.Dispose();
        }

        private void OnButtonLeftClicked(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu)
            {
                SetActiveClickableMenuToModOptionsPage();
                Game1.playSound("smallSelect");
            }
        }

        private void RemoveModOptionsFromMenu(object sender, EventArgsClickableMenuClosed e)
        {
            GraphicsEvents.OnPostRenderGuiEvent -= DrawButton;

            if (_modOptionsPageButton != null)
                _modOptionsPageButton.OnLeftClicked -= OnButtonLeftClicked;
            if (Game1.activeClickableMenu is GameMenu)
            {
                List<IClickableMenu> tabPages = _helper.Reflection.GetField<List<IClickableMenu>>(Game1.activeClickableMenu, "pages").GetValue();
                tabPages.Remove(_modOptionsPage);
            }
        }

        private void AddModOptionsToMenu(object sender, EventArgsClickableMenuChanged e)
        {
            if (Game1.activeClickableMenu is GameMenu)
            {
                if (_modOptionsPageButton == null)
                {
                    _modOptionsPage = new ModOptionsPage(_optionsElements);
                    _modOptionsPageButton = new ModOptionsPageButton();
                }
                GraphicsEvents.OnPostRenderGuiEvent += DrawButton;
                _modOptionsPageButton.OnLeftClicked += OnButtonLeftClicked;
                List<IClickableMenu> tabPages = _helper.Reflection.GetField<List<IClickableMenu>>(Game1.activeClickableMenu, "pages").GetValue();

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
            if (Game1.activeClickableMenu is GameMenu &&
                (Game1.activeClickableMenu as GameMenu).currentTab != 3) //don't render when the map is showing
            {
                if ((Game1.activeClickableMenu as GameMenu).currentTab == _modOptionsTabPageNumber)
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
