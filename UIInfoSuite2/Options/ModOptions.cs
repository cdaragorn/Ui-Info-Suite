using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIInfoSuite.Options
{
    class ModOptions
    {
        public bool AllowExperienceBarToFadeOut { get; set; } = true;
        public bool ShowExperienceBar { get; set; } = true;
        public bool ShowExperienceGain { get; set; } = true;
        public bool ShowLevelUpAnimation { get; set; } = true;
        public bool ShowHeartFills { get; set; } = true;
        public bool ShowExtraItemInformation { get; set; } = true;
        public bool ShowLocationOfTownsPeople { get; set; } = true;
        public bool ShowLuckIcon { get; set; } = true;
        public bool ShowTravelingMerchant { get; set; } = true;
        public bool ShowRainyDay { get; set; } = true;
        public bool ShowLocationOfTownsPeopleShowQuestIcon { get; set; } = true;
        public bool ShowCropAndBarrelTooltip { get; set; } = true;
        public bool ShowBirthdayIcon { get; set; } = true;
        public bool ShowAnimalsNeedPets { get; set; } = true;
        public bool HideAnimalPetOnMaxFriendship { get; set; } = true;
        public bool ShowItemEffectRanges { get; set; } = true;
        public bool ShowItemsRequiredForBundles { get; set; } = true;
        public bool ShowHarvestPricesInShop { get; set; } = true;
        public bool DisplayCalendarAndBillboard { get; set; } = true;
        public bool ShowWhenNewRecipesAreAvailable { get; set; } = true;
        public bool ShowToolUpgradeStatus { get; set; } = true;
        public bool HideMerchantWhenVisited { get; set; } = false;
        public bool ShowExactValue { get; set; } = false;
        public bool ShowRobinBuildingStatusIcon { get; set; } = true;
        public bool ShowTodaysGifts { get; set; } = true;
        public KeybindList OpenCalendarKeybind { get; set; } = KeybindList.ForSingle(SButton.B);
        public KeybindList OpenQuestBoardKeybind { get; set; } = KeybindList.ForSingle(SButton.H);
        public Dictionary<string, bool> ShowLocationOfFriends { get; set; } = new Dictionary<string, bool>();

        public object Get(string propertyName)
        {
            return GetType().GetProperties().Single(pi => pi.Name == propertyName).GetValue(this);
        }

        public void Set(string propertyName, object value)
        {
            GetType().GetProperties().Single(pi => pi.Name == propertyName).SetValue(this, value);
        }

        public bool Equals(ModOptions options)
        {
            if (AllowExperienceBarToFadeOut == options.AllowExperienceBarToFadeOut &&
                ShowExperienceBar == options.ShowExperienceBar &&
                ShowExperienceGain == options.ShowExperienceGain &&
                ShowLevelUpAnimation == options.ShowLevelUpAnimation &&
                ShowHeartFills == options.ShowHeartFills &&
                ShowExtraItemInformation == options.ShowExtraItemInformation &&
                ShowLocationOfTownsPeople == options.ShowLocationOfTownsPeople &&
                ShowLuckIcon == options.ShowLuckIcon &&
                ShowTravelingMerchant == options.ShowTravelingMerchant &&
                ShowRainyDay == options.ShowRainyDay &&
                ShowLocationOfTownsPeopleShowQuestIcon == options.ShowLocationOfTownsPeopleShowQuestIcon &&
                ShowCropAndBarrelTooltip == options.ShowCropAndBarrelTooltip &&
                ShowBirthdayIcon == options.ShowBirthdayIcon &&
                ShowAnimalsNeedPets == options.ShowAnimalsNeedPets &&
                HideAnimalPetOnMaxFriendship == options.HideAnimalPetOnMaxFriendship &&
                ShowItemEffectRanges == options.ShowItemEffectRanges &&
                ShowItemsRequiredForBundles == options.ShowItemsRequiredForBundles &&
                ShowHarvestPricesInShop == options.ShowHarvestPricesInShop &&
                DisplayCalendarAndBillboard == options.DisplayCalendarAndBillboard &&
                ShowWhenNewRecipesAreAvailable == options.ShowWhenNewRecipesAreAvailable &&
                ShowToolUpgradeStatus == options.ShowToolUpgradeStatus &&
                HideMerchantWhenVisited == options.HideMerchantWhenVisited &&
                ShowExactValue == options.ShowExactValue &&
                ShowRobinBuildingStatusIcon == options.ShowRobinBuildingStatusIcon &&
                ShowTodaysGifts == options.ShowTodaysGifts &&
                OpenCalendarKeybind == options.OpenCalendarKeybind &&
                OpenQuestBoardKeybind == options.OpenQuestBoardKeybind)
            {
                if (ShowLocationOfFriends.Count != options.ShowLocationOfFriends.Count)
                    return false;
                if (ShowLocationOfFriends.Keys.Except(options.ShowLocationOfFriends.Keys).Any())
                    return false;
                if (options.ShowLocationOfFriends.Keys.Except(ShowLocationOfFriends.Keys).Any())
                    return false;
                foreach (var pair in ShowLocationOfFriends)
                    if (pair.Value != options.ShowLocationOfFriends[pair.Key])
                        return false;
                return true;
            }
            return false;
        }
    }
}
