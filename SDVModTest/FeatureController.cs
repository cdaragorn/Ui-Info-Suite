﻿using System;
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

            _luckOfDay = new LuckOfDay(modOptions, helper);
            _experienceBar = new ExperienceBar(modOptions, helper);
            _locationOfTownsfolk = new LocationOfTownsfolk(modOptions, modconfig, helper);
            _showBirthdayIcon = new ShowBirthdayIcon(modOptions);
            _showAccurateHearts = new ShowAccurateHearts(modOptions);
            _showWhenAnimalNeedsPet = new ShowWhenAnimalNeedsPet(modOptions, helper);
            _showCalendarAndBillboardOnGameMenuButton = new ShowCalendarAndBillboardOnGameMenuButton(modOptions, helper);
            _showCropAndBarrelTime = new ShowCropAndBarrelTime(modOptions, helper);
            _showScarecrowAndSprinklerRange = new ShowItemEffectRanges(modOptions, modconfig);
            _showItemHoverInformation = new ShowItemHoverInformation(modOptions);
            _showTravelingMerchant = new ShowTravelingMerchant(modOptions, helper);
            _shopHarvestPrices = new ShopHarvestPrices(modOptions, helper);
            _showQueenOfSauceIcon = new ShowQueenOfSauceIcon(modOptions, helper);
            _showToolUpgradeStatus = new ShowToolUpgradeStatus(modOptions, helper);

            var saveButton = new ModOptionTrigger("saveOptions", "Save Options", OptionActionType.OK);
            modOptions.AddModOption(saveButton);
            saveButton.ActionTriggered += (string identifier) =>
            {
                modOptions.SaveCharacterSettings(Constants.SaveFolderName);
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