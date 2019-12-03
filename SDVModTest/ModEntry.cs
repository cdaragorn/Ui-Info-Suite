using UIInfoSuite.Options;
using UIInfoSuite.UIElements;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace UIInfoSuite
{
    public class ModEntry : Mod
    {
        private SkipIntro _skipIntro;

        private String _modDataFileName;
        private readonly Dictionary<String, String> _options = new Dictionary<string, string>();

        public static IMonitor MonitorObject { get; private set; }

        private ModOptionsPageHandler _modOptionsPageHandler;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            //Helper = helper;
            MonitorObject = Monitor;
            _skipIntro = new SkipIntro(helper.Events);

            Monitor.Log("starting.", LogLevel.Debug);
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.Saved += OnSaved;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.Display.Rendering += IconHandler.Handler.Reset;
            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;

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

        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {

        }

        /// <summary>Raised after the game returns to the title screen.</summary>
        /// <param name="sender">The event sender.</param>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            _modOptionsPageHandler?.Dispose();
            _modOptionsPageHandler = null;
        }

        /// <summary>Raised after the game finishes writing data to the save file (except the initial save creation).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaved(object sender, EventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(_modDataFileName))
            {
                if (File.Exists(_modDataFileName))
                    File.Delete(_modDataFileName);
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.IndentChars = "  ";
                using (XmlWriter writer = XmlWriter.Create(File.Open(_modDataFileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), settings))
                {
                    writer.WriteStartElement("options");

                    foreach (var option in _options)
                    {
                        writer.WriteStartElement("option");
                        writer.WriteAttributeString("name", option.Key);
                        writer.WriteValue(option.Value);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.Close();
                }
            }
        }

        /// <summary>Raised after the player loads a save slot and the world is initialised.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            try
            {
                try
                {
                    _modDataFileName = Path.Combine(Helper.DirectoryPath, Game1.player.Name + "_modData.xml");
                }
                catch
                {
                    Monitor.Log("Error: Player name contains character that cannot be used in file name. Using generic file name." + Environment.NewLine +
                        "Options may not be able to be different between characters.", LogLevel.Warn);
                    _modDataFileName = Path.Combine(Helper.DirectoryPath, "default_modData.xml");
                }

                if (File.Exists(_modDataFileName))
                {
                    XmlDocument document = new XmlDocument();

                    document.Load(_modDataFileName);
                    XmlNodeList nodes = document.GetElementsByTagName("option");

                    foreach (XmlNode node in nodes)
                    {
                        String key = node.Attributes["name"]?.Value;
                        String value = node.InnerText;

                        if (key != null)
                            _options[key] = value;
                    }

                }
            }
            catch (Exception ex)
            {
                Monitor.Log("Error loading mod config. " + ex.Message + Environment.NewLine + ex.StackTrace, LogLevel.Error);
            }

            _modOptionsPageHandler = new ModOptionsPageHandler(Helper, _options);
        }

       
    }
}
