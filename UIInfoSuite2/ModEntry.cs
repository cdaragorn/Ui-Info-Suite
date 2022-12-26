using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using UIInfoSuite2.AdditionalFeatures;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Options;

namespace UIInfoSuite2
{
    public class ModEntry : Mod
    {

        #region Properties
        public static IMonitor MonitorObject { get; private set; }
        public static DynamicGameAssetsEntry DGA { get; private set; }

        private static SkipIntro _skipIntro; // Needed so GC won't throw away object with subscriptions
        private static ModConfig _modConfig;

        private ModOptions _modOptions;
        private ModOptionsPageHandler _modOptionsPageHandler;

        private static EventHandler<ButtonsChangedEventArgs> _calendarAndQuestKeyBindingsHandler;
        #endregion


        #region Entry
        public override void Entry(IModHelper helper)
        {
            MonitorObject = Monitor;
            DGA = new DynamicGameAssetsEntry(Helper, Monitor);

            _skipIntro = new SkipIntro(helper.Events);
            _modConfig = Helper.ReadConfig<ModConfig>();

            helper.ConsoleCommands.Add("ship_all_items", "Ship all items for the Full Shipment achievement", ShipAllItems);

            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.Saved += OnSaved;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Display.Rendering += IconHandler.Handler.Reset;
        }
        #endregion


        #region Event subscriptions
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            // Unload if the main player quits.
            if (Context.ScreenId != 0) return;

            _modOptionsPageHandler?.Dispose();
            _modOptionsPageHandler = null;
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // Only load once for split screen.
            if (Context.ScreenId != 0) return;

            _modOptions = Helper.Data.ReadJsonFile<ModOptions>($"data/{Constants.SaveFolderName}.json")
                ?? Helper.Data.ReadJsonFile<ModOptions>($"data/{_modConfig.ApplyDefaultSettingsFromThisSave}.json")
                ?? new ModOptions();

            _modOptionsPageHandler = new ModOptionsPageHandler(Helper, _modOptions, _modConfig.ShowOptionsTabInMenu);
        }

        private void OnSaved(object sender, EventArgs e)
        {
            // Only save for the main player.
            if (Context.ScreenId != 0) return;

            Helper.Data.WriteJsonFile($"data/{Constants.SaveFolderName}.json", _modOptions);
        }

        public static void RegisterCalendarAndQuestKeyBindings(IModHelper helper, bool subscribe)
        {
            if (_calendarAndQuestKeyBindingsHandler == null)
                _calendarAndQuestKeyBindingsHandler = (object sender, ButtonsChangedEventArgs e) => HandleCalendarAndQuestKeyBindings(helper);

            helper.Events.Input.ButtonsChanged -= _calendarAndQuestKeyBindingsHandler;

            if (subscribe)
            {
                helper.Events.Input.ButtonsChanged += _calendarAndQuestKeyBindingsHandler;
            }
        }

        private static void HandleCalendarAndQuestKeyBindings(IModHelper helper)
        {
            if (_modConfig != null)
            {
                if (Context.IsPlayerFree && _modConfig.OpenCalendarKeybind.JustPressed())
                {
                    helper.Input.SuppressActiveKeybinds(_modConfig.OpenCalendarKeybind);
                    Game1.activeClickableMenu = new Billboard(false);
                }
                else if (Context.IsPlayerFree && _modConfig.OpenQuestBoardKeybind.JustPressed())
                {
                    helper.Input.SuppressActiveKeybinds(_modConfig.OpenQuestBoardKeybind);
                    Game1.RefreshQuestOfTheDay();
                    Game1.activeClickableMenu = new Billboard(true);
                }
            }
        }
        #endregion

        #region Generic mod config menu
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // get DGA's API
            var dgaApi = Helper.ModRegistry.GetApi<IDynamicGameAssetsApi>("spacechase0.DynamicGameAssets");
            if (dgaApi != null)
                DGA.InjectApi(dgaApi);

            // get Generic Mod Config Menu's API (if it's installed)
            var modVersion = Helper.ModRegistry.Get("spacechase0.GenericModConfigMenu")?.Manifest?.Version;
            var minModVersion = "1.6.0";
            if (modVersion?.IsOlderThan(minModVersion) == true)
            {
                Monitor.Log($"Detected Generic Mod Config Menu {modVersion} but expected {minModVersion} or newer. Disabling integration with that mod.", LogLevel.Warn);
                return;
            }

            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: ModManifest,
                reset: () => _modConfig = new ModConfig(),
                save: () => Helper.WriteConfig(_modConfig)
            );

            // add some config options
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Show options in in-game menu",
                tooltip: () => "Enables an extra tab in the in-game menu where you can configure every options for this mod.",
                getValue: () => _modConfig.ShowOptionsTabInMenu,
                setValue: value => _modConfig.ShowOptionsTabInMenu = value
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Apply default settings from this save",
                tooltip: () => "New characters will inherit the settings for the mod from this save file.",
                getValue: () => _modConfig.ApplyDefaultSettingsFromThisSave,
                setValue: value => _modConfig.ApplyDefaultSettingsFromThisSave = value
            );
            configMenu.AddKeybindList(
                mod: ModManifest,
                name: () => "Open calendar keybind",
                tooltip: () => "Opens the calendar tab.",
                getValue: () => _modConfig.OpenCalendarKeybind,
                setValue: value => _modConfig.OpenCalendarKeybind = value
            );
            configMenu.AddKeybindList(
                mod: ModManifest,
                name: () => "Open quest board keybind",
                tooltip: () => "Opens the quest board.",
                getValue: () => _modConfig.OpenQuestBoardKeybind,
                setValue: value => _modConfig.OpenQuestBoardKeybind = value
            );
        }
        #endregion

        void ShipAllItems(string command, string[] args)
        {
            if (args.Length > 0)
                throw new ArgumentException(nameof(args));

            var who = Game1.player;
            
            foreach (var item in Game1.objectInformation)
            {
                string itemName = item.Value.Split('/')[0];
                string text = item.Value.Split('/')[3];
                if (!text.Contains("Arch") && !text.Contains("Fish") && !text.Contains("Mineral") && !text.Substring(text.Length - 3).Equals("-2") && !text.Contains("Cooking") && !text.Substring(text.Length - 3).Equals("-7") && StardewValley.Object.isPotentialBasicShippedCategory(item.Key, text.Substring(text.Length - 3)))
				{
					if (!who.basicShipped.ContainsKey(item.Key))
                    {
                        Monitor.Log($"{command}: shipping {itemName}", LogLevel.Info);
					    who.basicShipped.Add(item.Key, 1);
                    }
                    else
                    {
                        Monitor.Log($"{command}: already shipped {itemName}", LogLevel.Info);
                    }
				}

            }
        }
    }
}
