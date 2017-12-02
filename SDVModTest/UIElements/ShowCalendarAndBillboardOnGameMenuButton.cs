using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using UIInfoSuite.Extensions;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI;
using StardewConfigFramework;

namespace UIInfoSuite.UIElements
{
    class ShowCalendarAndBillboardOnGameMenuButton : IDisposable
    {
        private ClickableTextureComponent _showBillboardButton = 
            new ClickableTextureComponent(
                new Rectangle(0, 0, 99, 60), 
                Game1.content.Load<Texture2D>(Path.Combine("Maps", "summer_town")), 
                new Rectangle(122, 291, 35, 20), 
                3f);

        private readonly IModHelper _helper;
        private readonly ModOptions _modOptions;
        private readonly ModOptionToggle _displayCalendarAndBillboard;

        private Item _hoverItem = null;

        public ShowCalendarAndBillboardOnGameMenuButton(ModOptions modOptions, IModHelper helper)
        {
            _helper = helper;
            _modOptions = modOptions;

            _displayCalendarAndBillboard = modOptions.GetOptionWithIdentifier<ModOptionToggle>(OptionKeys.DisplayCalendarAndBillboard) ?? new ModOptionToggle(OptionKeys.DisplayCalendarAndBillboard, "Show calendar/billboard button");
            _displayCalendarAndBillboard.ValueChanged += ToggleOption;
            modOptions.AddModOption(_displayCalendarAndBillboard);
            ToggleOption(_displayCalendarAndBillboard.identifier, _displayCalendarAndBillboard.IsOn);
        }

        public void ToggleOption(string identifier, bool showCalendarAndBillboard)
        {
            GraphicsEvents.OnPostRenderGuiEvent -= RenderButtons;
            ControlEvents.MouseChanged -= OnBillboardIconClick;
            ControlEvents.ControllerButtonPressed -= OnBillboardIconPressed;
            GraphicsEvents.OnPreRenderEvent -= GetHoverItem;

            if (showCalendarAndBillboard)
            {
                GraphicsEvents.OnPostRenderGuiEvent += RenderButtons;
                ControlEvents.MouseChanged += OnBillboardIconClick;
                ControlEvents.ControllerButtonPressed += OnBillboardIconPressed;
                GraphicsEvents.OnPreRenderEvent += GetHoverItem;
            }
        }

        private void GetHoverItem(object sender, EventArgs e)
        {
            _hoverItem = Tools.GetHoveredItem();
        }

        private void OnBillboardIconPressed(object sender, EventArgsControllerButtonPressed e)
        {
            if (e.ButtonPressed == Buttons.A)
                ActivateBillboard();
        }

        public void Dispose()
        {
            ToggleOption(OptionKeys.DisplayCalendarAndBillboard, false);
        }

        private void OnBillboardIconClick(object sender, EventArgsMouseStateChanged e)
        {
            if (e.NewState.LeftButton == ButtonState.Pressed)
            {
                ActivateBillboard();
            }
        }

        private void ActivateBillboard()
        {
            if (Game1.activeClickableMenu is GameMenu &&
                (Game1.activeClickableMenu as GameMenu).currentTab == 0 &&
                _showBillboardButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                if (Game1.questOfTheDay != null &&
                    String.IsNullOrEmpty(Game1.questOfTheDay.currentObjective))
                    Game1.questOfTheDay.currentObjective = "wat?";

                Game1.activeClickableMenu =
                    new Billboard(!(Game1.getMouseX() <
                    _showBillboardButton.bounds.X + _showBillboardButton.bounds.Width / 2));
            }
        }

        private void RenderButtons(object sender, EventArgs e)
        {
            if (_hoverItem == null &&
                Game1.activeClickableMenu is GameMenu &&
                (Game1.activeClickableMenu as GameMenu).currentTab == 0)
            {
                _showBillboardButton.bounds.X = Game1.activeClickableMenu.xPositionOnScreen + Game1.activeClickableMenu.width - 160;

                _showBillboardButton.bounds.Y = Game1.activeClickableMenu.yPositionOnScreen + Game1.activeClickableMenu.height - 300;
                _showBillboardButton.draw(Game1.spriteBatch);
                if (_showBillboardButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                {
                    String hoverText = Game1.getMouseX() < 
                        _showBillboardButton.bounds.X + _showBillboardButton.bounds.Width / 2 ? 
                        LanguageKeys.Calendar : LanguageKeys.Billboard;
                    IClickableMenu.drawHoverText(
                        Game1.spriteBatch,
                        _helper.SafeGetString(hoverText),
                        Game1.dialogueFont);
                }
            }
        }
    }
}
