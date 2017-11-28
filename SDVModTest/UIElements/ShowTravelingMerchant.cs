using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIInfoSuite.Extensions;

namespace UIInfoSuite.UIElements
{
    class ShowTravelingMerchant : IDisposable
    {
        private bool _travelingMerchantIsHere = false;
        private readonly IModHelper _helper;

        public void ToggleOption(bool showTravelingMerchant)
        {
            GraphicsEvents.OnPreRenderHudEvent -= DrawTravelingMerchant;
            TimeEvents.AfterDayStarted -= DayChanged;

            if (showTravelingMerchant)
            {
                DayChanged(null, new EventArgsIntChanged(0, Game1.dayOfMonth));
                GraphicsEvents.OnPreRenderHudEvent += DrawTravelingMerchant;
                TimeEvents.AfterDayStarted += DayChanged;
            }
        }

        public ShowTravelingMerchant(IModHelper helper)
        {
            _helper = helper;
        }

        public void Dispose()
        {
            ToggleOption(false);
        }

        private void DayChanged(object sender, EventArgs e)
        {

            int dayOfWeek = Game1.dayOfMonth % 7;
            _travelingMerchantIsHere = dayOfWeek == 0 || dayOfWeek == 5;
        }

        private void DrawTravelingMerchant(object sender, EventArgs e)
        {

            if (!Game1.eventUp &&
                _travelingMerchantIsHere)
            {
                Point iconPosition = IconHandler.Handler.GetNewIconPosition();
                ClickableTextureComponent textureComponent = 
                    new ClickableTextureComponent(
                        new Rectangle(iconPosition.X, iconPosition.Y, 40, 40), 
                        Game1.mouseCursors, 
                        new Rectangle(192, 1411, 20, 20), 
                        2f);
                textureComponent.draw(Game1.spriteBatch);
                if (textureComponent.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                {
                    string hoverText = _helper.SafeGetString(
                        LanguageKeys.TravelingMerchantIsInTown);
                    IClickableMenu.drawHoverText(
                        Game1.spriteBatch, 
                        hoverText, Game1.dialogueFont);
                }
            }
        }
    }
}
