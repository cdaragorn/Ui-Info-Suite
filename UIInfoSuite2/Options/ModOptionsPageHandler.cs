using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Reflection;
using UIInfoSuite.Infrastructure.Extensions;
using UIInfoSuite.UIElements;

namespace UIInfoSuite.Options
{
    class ModOptionsPageHandler : IDisposable
    {
        private List<ModOptionsElement> _optionsElements = new List<ModOptionsElement>();
        private readonly List<IDisposable> _elementsToDispose;
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
        private readonly ShowItemHoverInformation _showItemHoverInformation;
        private readonly ShowTravelingMerchant _showTravelingMerchant;
        private readonly ShowRainyDayIcon _showRainyDayIcon;
        private readonly ShopHarvestPrices _shopHarvestPrices;
        private readonly ShowQueenOfSauceIcon _showQueenOfSauceIcon;
        private readonly ShowToolUpgradeStatus _showToolUpgradeStatus;
        private readonly ShowRobinBuildingStatusIcon _showRobinBuildingStatusIcon;
        private readonly ShowTodaysGifts _showTodaysGift;

        public ModOptionsPageHandler(IModHelper helper, ModOptions _options)
        {
            helper.Events.Display.MenuChanged += ToggleModOptions;
            _helper = helper;
            _luckOfDay = new LuckOfDay(helper);
            _showBirthdayIcon = new ShowBirthdayIcon(helper.Events);
            _showAccurateHearts = new ShowAccurateHearts(helper.Events);
            _locationOfTownsfolk = new LocationOfTownsfolk(helper, _options);
            _showWhenAnimalNeedsPet = new ShowWhenAnimalNeedsPet(helper);
            _showCalendarAndBillboardOnGameMenuButton = new ShowCalendarAndBillboardOnGameMenuButton(helper);
            _showScarecrowAndSprinklerRange = new ShowItemEffectRanges(helper);
            _experienceBar = new ExperienceBar(helper);
            _showItemHoverInformation = new ShowItemHoverInformation(helper.Events);
            _shopHarvestPrices = new ShopHarvestPrices(helper);
            _showQueenOfSauceIcon = new ShowQueenOfSauceIcon(helper);
            _showTravelingMerchant = new ShowTravelingMerchant(helper);
            _showRainyDayIcon = new ShowRainyDayIcon(helper);
            _showCropAndBarrelTime = new ShowCropAndBarrelTime(helper);
            _showToolUpgradeStatus = new ShowToolUpgradeStatus(helper);
            _showRobinBuildingStatusIcon = new ShowRobinBuildingStatusIcon(helper);
            _showTodaysGift = new ShowTodaysGifts(helper);

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
                _showRainyDayIcon,
                _shopHarvestPrices,
                _showQueenOfSauceIcon,
                _showToolUpgradeStatus,
                _showRobinBuildingStatusIcon
            };

            int whichOption = 1;
            Version thisVersion = Assembly.GetAssembly(this.GetType()).GetName().Version;
            _optionsElements.Add(new ModOptionsElement("UI Info Suite 2 v" + thisVersion.Major + "." + thisVersion.Minor + "." + thisVersion.Build));


            var luckIcon = new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.ShowLuckIcon)), whichOption++, _luckOfDay.ToggleOption, () => _options.ShowLuckIcon, v => _options.ShowLuckIcon = v);
            _optionsElements.Add(luckIcon);
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.ShowExactValue)), whichOption++, _luckOfDay.ToggleShowExactValueOption, () => _options.ShowExactValue, v => _options.ShowExactValue = v, luckIcon));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.ShowLevelUpAnimation)), whichOption++, _experienceBar.ToggleLevelUpAnimation, () => _options.ShowLevelUpAnimation, v => _options.ShowLevelUpAnimation = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.ShowExperienceBar)), whichOption++, _experienceBar.ToggleShowExperienceBar, () => _options.ShowExperienceBar, v => _options.ShowExperienceBar = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.AllowExperienceBarToFadeOut)), whichOption++, _experienceBar.ToggleExperienceBarFade, () => _options.AllowExperienceBarToFadeOut, v => _options.AllowExperienceBarToFadeOut = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.ShowExperienceGain)), whichOption++, _experienceBar.ToggleShowExperienceGain, () => _options.ShowExperienceGain, v => _options.ShowExperienceGain = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.ShowLocationOfTownsPeople)), whichOption++, _locationOfTownsfolk.ToggleShowNPCLocationsOnMap, () => _options.ShowLocationOfTownsPeople, v => _options.ShowLocationOfTownsPeople = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.ShowBirthdayIcon)), whichOption++, _showBirthdayIcon.ToggleOption, () => _options.ShowBirthdayIcon, v => _options.ShowBirthdayIcon = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.ShowHeartFills)), whichOption++, _showAccurateHearts.ToggleOption, () => _options.ShowHeartFills, v => _options.ShowHeartFills = v));
            var animalPetIcon = new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.ShowAnimalsNeedPets)), whichOption++, _showWhenAnimalNeedsPet.ToggleOption, () => _options.ShowAnimalsNeedPets, v => _options.ShowAnimalsNeedPets = v);
            _optionsElements.Add(animalPetIcon);
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.HideAnimalPetOnMaxFriendship)), whichOption++, _showWhenAnimalNeedsPet.ToggleDisableOnMaxFirendshipOption, () => _options.HideAnimalPetOnMaxFriendship, v => _options.HideAnimalPetOnMaxFriendship = v, animalPetIcon));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.DisplayCalendarAndBillboard)), whichOption++, _showCalendarAndBillboardOnGameMenuButton.ToggleOption, () => _options.DisplayCalendarAndBillboard, v => _options.DisplayCalendarAndBillboard = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.ShowCropAndBarrelTooltip)), whichOption++, _showCropAndBarrelTime.ToggleOption, () => _options.ShowCropAndBarrelTooltip, v => _options.ShowCropAndBarrelTooltip = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.ShowItemEffectRanges)), whichOption++, _showScarecrowAndSprinklerRange.ToggleOption, () => _options.ShowItemEffectRanges, v => _options.ShowItemEffectRanges = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.ShowExtraItemInformation)), whichOption++, _showItemHoverInformation.ToggleOption, () => _options.ShowExtraItemInformation, v => _options.ShowExtraItemInformation = v));
            var travellingMerchantIcon = new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.ShowTravelingMerchant)), whichOption++, _showTravelingMerchant.ToggleOption, () => _options.ShowTravelingMerchant, v => _options.ShowTravelingMerchant = v);
            _optionsElements.Add(travellingMerchantIcon);
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.HideMerchantWhenVisited)), whichOption++, _showTravelingMerchant.ToggleHideWhenVisitedOption, () => _options.HideMerchantWhenVisited, v => _options.HideMerchantWhenVisited = v, travellingMerchantIcon));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.ShowRainyDay)), whichOption++, _showRainyDayIcon.ToggleOption, () => _options.ShowRainyDay, v => _options.ShowRainyDay = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.ShowHarvestPricesInShop)), whichOption++, _shopHarvestPrices.ToggleOption, () => _options.ShowHarvestPricesInShop, v => _options.ShowHarvestPricesInShop = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.ShowWhenNewRecipesAreAvailable)), whichOption++, _showQueenOfSauceIcon.ToggleOption, () => _options.ShowWhenNewRecipesAreAvailable, v => _options.ShowWhenNewRecipesAreAvailable = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.ShowToolUpgradeStatus)), whichOption++, _showToolUpgradeStatus.ToggleOption, () => _options.ShowToolUpgradeStatus, v => _options.ShowToolUpgradeStatus = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.ShowRobinBuildingStatusIcon)), whichOption++, _showRobinBuildingStatusIcon.ToggleOption, () => _options.ShowRobinBuildingStatusIcon, v => _options.ShowRobinBuildingStatusIcon = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(_options.ShowTodaysGifts)), whichOption++, _showTodaysGift.ToggleOption, () => _options.ShowTodaysGifts, v => _options.ShowTodaysGifts = v));
        }


        public void Dispose()
        {
            foreach (var item in _elementsToDispose)
                item.Dispose();
        }

        private void OnButtonLeftClicked(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu
                && !GameMenu.forcePreventClose) // Do not activate when an action is being remapped
            {
                SetActiveClickableMenuToModOptionsPage();
                Game1.playSound("smallSelect");
            }
        }

        private void ToggleModOptions(object sender, MenuChangedEventArgs e)
        {
            // Remove from old menu
            if (e.OldMenu != null)
            {
                _helper.Events.Display.RenderedActiveMenu -= DrawButton;
                if (_modOptionsPageButton != null)
                    _modOptionsPageButton.OnLeftClicked -= OnButtonLeftClicked;

                if (e.OldMenu is GameMenu gameMenu)
                {
                    List<IClickableMenu> tabPages = gameMenu.pages;
                    tabPages.Remove(_modOptionsPage);
                }
            }

            // Add to new menu
            if (e.NewMenu is GameMenu newMenu)
            {
                if (_modOptionsPageButton == null)
                {
                    _modOptionsPage = new ModOptionsPage(_optionsElements, _helper.Events);
                    _modOptionsPageButton = new ModOptionsPageButton(_helper.Events);
                }

                _helper.Events.Display.RenderedActiveMenu += DrawButton;
                _modOptionsPageButton.OnLeftClicked += OnButtonLeftClicked;
                List<IClickableMenu> tabPages = newMenu.pages;

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
            if (Game1.activeClickableMenu is GameMenu gameMenu &&
                gameMenu.currentTab != 3 // Do not render when the map is showing
                && !GameMenu.forcePreventClose) // Do not render when an action is being remapped
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
