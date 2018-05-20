using UIInfoSuite.Options;
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

namespace UIInfoSuite
{
    public class ModEntry : Mod
    {

        private readonly SkipIntro _skipIntro = new SkipIntro();

        private String _modDataFileName;
        private readonly Dictionary<String, String> _options = new Dictionary<string, string>();

        public static IMonitor MonitorObject { get; private set; }
        public static CultureInfo SpecificCulture { get; private set; }
        //public static ResourceManager Resources { get; private set; }
        //public static IModHelper Helper { get; private set; }

        private ModOptionsPageHandler _modOptionsPageHandler;

        public ModEntry()
        {
            
        }

        public override void Entry(IModHelper helper)
        {
            //Helper = helper;
            MonitorObject = Monitor;
            Monitor.Log("starting.", LogLevel.Debug);
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
            _modOptionsPageHandler.Dispose();
            _modOptionsPageHandler = null;
        }

        private void SaveModData(object sender, EventArgs e)
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
                }
            }
        }

        private void LoadModData(object sender, EventArgs e)
        {
            String playerName = Game1.player.Name;
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
