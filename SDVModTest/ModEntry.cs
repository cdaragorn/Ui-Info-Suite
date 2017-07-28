using UIInfoSuite.UIElements;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Globalization;
using static StardewValley.LocalizedContentManager;
using System.Resources;
using System.Reflection;
using StardewConfigFramework;

namespace UIInfoSuite
{
    public class ModEntry: Mod
    {

        private readonly SkipIntro _skipIntro = new SkipIntro();

        private String _modDataFileName;
        private ModOptions _modOptions;
        private ModConfig _modConfig;

        public static IMonitor MonitorObject { get; private set; }
        public static CultureInfo SpecificCulture { get; private set; }
        //public static ResourceManager Resources { get; private set; }
        //public static IModHelper Helper { get; private set; }

        private FeatureController _controller;

        public ModEntry()
        {

        }

        public override void Entry(IModHelper helper)
        {
            //Helper = helper;
            MonitorObject = Monitor;
            SaveEvents.AfterLoad += LoadModData;
            SaveEvents.AfterSave += SaveModData;
            SaveEvents.AfterReturnToTitle += ReturnToTitle;
            GraphicsEvents.OnPreRenderEvent += IconHandler.Handler.Reset;
            LocalizedContentManager.OnLanguageChange += LocalizedContentManager_OnLanguageChange;
            LocalizedContentManager_OnLanguageChange(LocalizedContentManager.CurrentLanguageCode);

            //Resources = new ResourceManager("UIInfoSuite.Resource.strings", Assembly.GetAssembly(typeof(ModEntry)));
            //try
            //{
            //    //Test to make sure the culture specific files are there
            //    Resources.GetString(LanguageKeys.Days, ModEntry.SpecificCulture);
            //}
            //catch
            //{
            //    Resources = Properties.Resources.ResourceManager;
            //}
        }

        private void LocalizedContentManager_OnLanguageChange(LanguageCode code)
        {
            String cultureString = code.ToString();
            SpecificCulture = CultureInfo.CreateSpecificCulture(cultureString);
        }

        private void ReturnToTitle(object sender, EventArgs e)
        {
            _controller.Dispose();
            _controller = null;
        }

        private void SaveModData(object sender, EventArgs e)
        {
            _modOptions.SaveUserSettings();
            Helper.WriteConfig(_modConfig);
        }

        private void LoadModData(object sender, EventArgs e)
        {
            var Settings = IModSettingsFramework.Instance;
            this._modOptions = ModOptions.LoadUserSettings(this);
            Settings.AddModOptions(this._modOptions);

            try
            { // create new config if no file exists
                _modConfig = Helper.ReadConfig<ModConfig>() ?? new ModConfig();
            }
            catch
            { // if parsing fails, create a new config
                _modConfig = new ModConfig();
            }

            _controller = new FeatureController(_modOptions, _modConfig, Helper);
        }
    }
}
