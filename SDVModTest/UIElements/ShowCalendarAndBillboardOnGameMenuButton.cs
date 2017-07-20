using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using UIInfoSuite.Extensions;
using UIInfoSuite.Options;
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
        private String _hoverText;
        private readonly IDictionary<String, String> _options;
        private readonly IModHelper _helper;

        public ShowCalendarAndBillboardOnGameMenuButton(IDictionary<String, String> options,
            IModHelper helper)
        {
            _options = options;
            _helper = helper;
        }

        public void ToggleOption(bool showCalendarAndBillboard)
        {
            GraphicsEvents.OnPostRenderGuiEvent -= RenderButtons;
            GraphicsEvents.OnPreRenderGuiEvent -= RemoveDefaultTooltips;
            ControlEvents.MouseChanged -= OnBillboardIconClick;
            ControlEvents.ControllerButtonPressed -= OnBillboardIconPressed;

            if (showCalendarAndBillboard)
            {
                GraphicsEvents.OnPostRenderGuiEvent += RenderButtons;
                GraphicsEvents.OnPreRenderGuiEvent += RemoveDefaultTooltips;
                ControlEvents.MouseChanged += OnBillboardIconClick;
                ControlEvents.ControllerButtonPressed += OnBillboardIconPressed;
            }
        }

        private void OnBillboardIconPressed(object sender, EventArgsControllerButtonPressed e)
        {
            if (e.ButtonPressed == Buttons.A)
                ActivateBillboard();
        }

        public void Dispose()
        {
            ToggleOption(false);
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
                Game1.activeClickableMenu =
                    new Billboard(!(Game1.getMouseX() <
                    _showBillboardButton.bounds.X + _showBillboardButton.bounds.Width / 2));
            }
        }

        private void RemoveDefaultTooltips(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu &&
                !_options.SafeGet(OptionKeys.ShowExtraItemInformation).SafeParseBool())
            {
                InventoryPage inventoryPage = (typeof(GameMenu).GetField("pages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Game1.activeClickableMenu) as List<IClickableMenu>)[0] as InventoryPage;
                _hoverText = typeof(InventoryPage).GetField("hoverText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(inventoryPage) as String;
                typeof(InventoryPage).GetField("hoverText", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(inventoryPage, "");
            }
        }

        private void RenderButtons(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu &&
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

                if (!_options.SafeGet(OptionKeys.ShowExtraItemInformation).SafeParseBool())
                {
                    InventoryPage inventoryPage = (typeof(GameMenu).GetField("pages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Game1.activeClickableMenu) as List<IClickableMenu>)[0] as InventoryPage;
                    String hoverTitle = typeof(InventoryPage).GetField("hoverTitle", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(inventoryPage) as String;
                    Item hoveredItem = typeof(InventoryPage).GetField("hoveredItem", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(inventoryPage) as Item;
                    Item heldItem = typeof(InventoryPage).GetField("heldItem", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(inventoryPage) as Item;
                    IClickableMenu.drawToolTip(
                        Game1.spriteBatch,
                        _hoverText,
                        hoverTitle,
                        hoveredItem,
                        heldItem != null);
                }
            }
        }
    }
}
