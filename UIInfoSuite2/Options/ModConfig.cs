using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace UIInfoSuite.Options
{
    class ModConfig
    {
        public bool ShowOptionsTabInMenu { get; set; } = true;
        public string ApplyDefaultSettingsFromThisSave { get; set; } = "JohnDoe_123456789";
        public KeybindList OpenCalendarKeybind { get; set; } = KeybindList.ForSingle(SButton.B);
        public KeybindList OpenQuestBoardKeybind { get; set; } = KeybindList.ForSingle(SButton.H);
    }
}
