using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewConfigFramework;
using System.Collections.Generic;
using UIInfoSuite.UIElements;
using System.Reflection;

namespace UIInfoSuite
{
    public class FeatureController
    {
        private readonly List<IDisposable> _elementsToDispose;
        private readonly ModOptions _modOptions;
        private readonly ModConfig _modConfig;
        private readonly IModHelper _helper;

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
        private readonly ShopHarvestPrices _shopHarvestPrices;
        private readonly ShowQueenOfSauceIcon _showQueenOfSauceIcon;
        private readonly ShowToolUpgradeStatus _showToolUpgradeStatus;

        internal FeatureController(ModOptions modOptions, ModConfig modconfig, IModHelper helper)
        {
            _modOptions = modOptions;
            _modConfig = modconfig;
            _helper = helper;


            // Create Category Label in ModConfigMenu
            Version thisVersion = Assembly.GetAssembly(this.GetType()).GetName().Version;
            var label = new ModOptionCategoryLabel("versionLabel", "UI Info Suite v" +
                            thisVersion.Major + "." + thisVersion.Minor + "." + thisVersion.Build);
            _modOptions.AddModOption(label);

            //_optionsElements.Add(new ModOptionsCheckbox("Show luck icon", whichOption++, _luckOfDay.Toggle, _options, OptionKeys.ShowLuckIcon));
            _luckOfDay = new LuckOfDay(modOptions, helper);

            //_optionsElements.Add(new ModOptionsCheckbox("Show level up animation", whichOption++, _experienceBar.ToggleLevelUpAnimation, _options, OptionKeys.ShowLevelUpAnimation));
            //_optionsElements.Add(new ModOptionsCheckbox("Show experience bar", whichOption++, _experienceBar.ToggleShowExperienceBar, _options, OptionKeys.ShowExperienceBar));
            //_optionsElements.Add(new ModOptionsCheckbox("Allow experience bar to fade out", whichOption++, _experienceBar.ToggleExperienceBarFade, _options, OptionKeys.AllowExperienceBarToFadeOut));
            //_optionsElements.Add(new ModOptionsCheckbox("Show experience gain", whichOption++, _experienceBar.ToggleShowExperienceGain, _options, OptionKeys.ShowExperienceGain));
            _experienceBar = new ExperienceBar(modOptions, helper);

            //_optionsElements.Add(new ModOptionsCheckbox("Show townspeople on map", whichOption++, _locationOfTownsfolk.ToggleShowNPCLocationsOnMap, _options, OptionKeys.ShowLocationOfTownsPeople));
            _locationOfTownsfolk = new LocationOfTownsfolk(modOptions, modconfig, helper);

            //_optionsElements.Add(new ModOptionsCheckbox("Show Birthday icon", whichOption++, _showBirthdayIcon.ToggleOption, _options, OptionKeys.ShowBirthdayIcon));
            _showBirthdayIcon = new ShowBirthdayIcon(modOptions);

            //_optionsElements.Add(new ModOptionsCheckbox("Show heart fills", whichOption++, _showAccurateHearts.ToggleOption, _options, OptionKeys.ShowHeartFills));
            _showAccurateHearts = new ShowAccurateHearts(modOptions);

            //_optionsElements.Add(new ModOptionsCheckbox("Show when animals need pets", whichOption++, _showWhenAnimalNeedsPet.ToggleOption, _options, OptionKeys.ShowAnimalsNeedPets));
            _showWhenAnimalNeedsPet = new ShowWhenAnimalNeedsPet(modOptions, helper);

            //_optionsElements.Add(new ModOptionsCheckbox("Show calendar/billboard button", whichOption++, _showCalendarAndBillboardOnGameMenuButton.ToggleOption, _options, OptionKeys.DisplayCalendarAndBillboard));
            _showCalendarAndBillboardOnGameMenuButton = new ShowCalendarAndBillboardOnGameMenuButton(modOptions, helper);

            //_optionsElements.Add(new ModOptionsCheckbox("Show crop and barrel times", whichOption++, _showCropAndBarrelTime.ToggleOption, _options, OptionKeys.ShowCropAndBarrelTooltip));
            _showCropAndBarrelTime = new ShowCropAndBarrelTime(modOptions, helper);

            //_optionsElements.Add(new ModOptionsCheckbox("Show scarecrow and sprinkler range", whichOption++, _showScarecrowAndSprinklerRange.ToggleOption, _options, OptionKeys.ShowItemEffectRanges));
            _showScarecrowAndSprinklerRange = new ShowItemEffectRanges(modOptions, modconfig);

            //_optionsElements.Add(new ModOptionsCheckbox("Show Item hover information", whichOption++, _showItemHoverInformation.ToggleOption, _options, OptionKeys.ShowExtraItemInformation));
            _showItemHoverInformation = new ShowItemHoverInformation(modOptions);

            //_optionsElements.Add(new ModOptionsCheckbox("Show Traveling Merchant", whichOption++, _showTravelingMerchant.ToggleOption, _options, OptionKeys.ShowTravelingMerchant));
            _showTravelingMerchant = new ShowTravelingMerchant(modOptions, helper);

            //_optionsElements.Add(new ModOptionsCheckbox("Show shop harvest prices", whichOption++, _shopHarvestPrices.ToggleOption, _options, OptionKeys.ShowHarvestPricesInShop));
            _shopHarvestPrices = new ShopHarvestPrices(modOptions, helper);

            //_optionsElements.Add(new ModOptionsCheckbox("Show when new recipes are available", whichOption++, _showQueenOfSauceIcon.ToggleOption, _options, OptionKeys.ShowWhenNewRecipesAreAvailable));
            _showQueenOfSauceIcon = new ShowQueenOfSauceIcon(modOptions, helper);

            _showToolUpgradeStatus = new ShowToolUpgradeStatus(modOptions, helper);

            var saveButton = new ModOptionTrigger("saveOptions", "Save Options", OptionActionType.OK);
            modOptions.AddModOption(saveButton);
            saveButton.ActionTriggered += (string identifier) =>
            {
                modOptions.SaveUserSettings();
                helper.WriteConfig(_modConfig);
            };


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

        }

        public void Dispose()
        {
            foreach (var item in _elementsToDispose)
                item.Dispose();
        }
    }
}