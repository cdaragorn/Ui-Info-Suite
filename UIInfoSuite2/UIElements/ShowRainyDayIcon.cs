using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using Microsoft.Xna.Framework.Graphics;
using UIInfoSuite.Infrastructure;
using UIInfoSuite.Infrastructure.Extensions;

namespace UIInfoSuite.UIElements
{
    class ShowRainyDayIcon : IDisposable
    {
        #region Properties

        private bool _IsNextDayRainy;
        Rectangle? _weatherIconSpriteLocation;
        private string _hoverText;
        private ClickableTextureComponent _rainyDayIcon;
        private Texture2D _iconSheet;

        private Color[] _weatherIconColors = null;
        private const int WeatherSheetWidth = 45;
        private const int WeatherSheetHeight = 15;
        
        private readonly IModHelper _helper;
        #endregion

        #region Lifecycle
        public ShowRainyDayIcon(IModHelper helper)
        {
            _helper = helper;
            CreateTileSheet();
        }

        public void Dispose()
        {
            ToggleOption(false);
            _iconSheet.Dispose();
        }

        public void ToggleOption(bool showTravelingMerchant)
        {
            _helper.Events.Display.RenderingHud -= OnRenderingHud;
            _helper.Events.Display.RenderedHud -= OnRenderedHud;

            if (showTravelingMerchant)
            {
                _helper.Events.Display.RenderingHud += OnRenderingHud;
                _helper.Events.Display.RenderedHud += OnRenderedHud;
            }
        }
        #endregion

        #region Event subscriptions
        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            GetWeatherIconSpriteLocation();

            // Draw icon
            if (!Game1.eventUp && _IsNextDayRainy && _weatherIconSpriteLocation.HasValue)
            {
                Point iconPosition = IconHandler.Handler.GetNewIconPosition();
                _rainyDayIcon =
                    new ClickableTextureComponent(
                        new Rectangle(iconPosition.X, iconPosition.Y, 40, 40),
                        _iconSheet,
                        _weatherIconSpriteLocation.Value,
                        8 / 3f);
                _rainyDayIcon.draw(Game1.spriteBatch);
            }
        }

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // Show text on hover
            if (_IsNextDayRainy && (_rainyDayIcon?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false) && !String.IsNullOrEmpty(_hoverText))
            {
                IClickableMenu.drawHoverText(
                    Game1.spriteBatch,
                    _hoverText,
                    Game1.dialogueFont
                );
            }
        }
        #endregion

        #region Logic

        /// <summary>
        /// Creates a custom tilesheet for weather icons.
        /// Meant to mimic the TV screen, which has a border around it, while the individual icons in the Cursors tilesheet don't have a border
        /// Extracts the border, and each individual weather icon and stitches them together into one separate sheet
        ///</summary>
        private void CreateTileSheet()
        {

            ModEntry.MonitorObject.Log("Setting up icon sheet", LogLevel.Info);
            // Setup Texture sheet as a copy, so as not to disturb existing sprites
            _iconSheet = new Texture2D(Game1.graphics.GraphicsDevice , WeatherSheetWidth, WeatherSheetHeight);
            _weatherIconColors = new Color[WeatherSheetWidth * WeatherSheetHeight];
            var cursorColors = new Color[Game1.mouseCursors.Width * Game1.mouseCursors.Height];
            var bounds = new Rectangle(0, 0, Game1.mouseCursors.Width, Game1.mouseCursors.Height);
            Game1.mouseCursors.GetData(cursorColors);
            var subTextureColors = new Color[15 * 15];
            
            // Copy over the bits we want
            // Border from TV screen
            Tools.GetSubTexture(subTextureColors, cursorColors, bounds, new Rectangle(499, 307, 15, 15));
            // Copy to each destination
            Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth, new Rectangle(0, 0, 15, 15));
            Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth, new Rectangle(15, 0, 15, 15));
            Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth, new Rectangle(30, 0, 15, 15));

            subTextureColors = new Color[13 * 13];
            // Rainy Weather
            Tools.GetSubTexture(subTextureColors, cursorColors, bounds, new Rectangle(504, 333, 13, 13));
            Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth,  new Rectangle(1, 1, 13, 13));
            
            // Stormy Weather
            Tools.GetSubTexture(subTextureColors, cursorColors, bounds, new Rectangle(426, 346, 13, 13));
            Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth,  new Rectangle(16, 1, 13, 13));
            
            // Snowy Weather
            Tools.GetSubTexture(subTextureColors, cursorColors, bounds, new Rectangle(465, 346, 13, 13));
            Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth,  new Rectangle(31, 1, 13, 13));
            
            _iconSheet.SetData(_weatherIconColors);
        }

        private void GetWeatherIconSpriteLocation()
        {
            
            switch (Game1.weatherForTomorrow)
            {
                case Game1.weather_sunny:
                case Game1.weather_debris:
                case Game1.weather_festival:
                case Game1.weather_wedding:
                    _IsNextDayRainy = false;
                    break;

                case Game1.weather_rain:
                    _IsNextDayRainy = true;
                    _weatherIconSpriteLocation = new Rectangle(0, 0, 15, 15);
                    _hoverText = _helper.SafeGetString(LanguageKeys.RainNextDay);
                    break;

                case Game1.weather_lightning:
                    _IsNextDayRainy = true;
                    _weatherIconSpriteLocation = new Rectangle(15, 0, 15, 15);
                    _hoverText = _helper.SafeGetString(LanguageKeys.ThunderstormNextDay);
                    break;

                case Game1.weather_snow:
                    _IsNextDayRainy = true;
                    _weatherIconSpriteLocation = new Rectangle(30, 0, 15, 15);
                    _hoverText = _helper.SafeGetString(LanguageKeys.SnowNextDay);
                    break;

                default:
                    _IsNextDayRainy = false;
                    break;
            }
        }
        #endregion
    }
}
