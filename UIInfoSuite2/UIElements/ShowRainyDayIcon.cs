using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private readonly IModHelper _helper;
        #endregion

        #region Lifecycle
        public ShowRainyDayIcon(IModHelper helper)
        {
            _helper = helper;
        }

        public void Dispose()
        {
            ToggleOption(false);
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
                        Game1.animations,
                        _weatherIconSpriteLocation.Value,
                        2f);
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
                    _weatherIconSpriteLocation = new Rectangle(268, 1750, 20, 20);
                    _hoverText = _helper.SafeGetString(LanguageKeys.RainNextDay);
                    break;

                case Game1.weather_lightning:
                    _IsNextDayRainy = true;
                    _weatherIconSpriteLocation = new Rectangle(272, 1641, 20, 20);
                    _hoverText = _helper.SafeGetString(LanguageKeys.ThunderstormNextDay);
                    break;

                case Game1.weather_snow:
                    _IsNextDayRainy = true;
                    _weatherIconSpriteLocation = new Rectangle(260, 680, 20, 20);
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
