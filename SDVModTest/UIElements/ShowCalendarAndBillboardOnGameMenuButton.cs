using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UIInfoSuite.Extensions;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System;
using System.IO;
using StardewModdingAPI;

namespace UIInfoSuite.UIElements
{
    class ShowCalendarAndBillboardOnGameMenuButton : IDisposable
    {
        private readonly PerScreen<ClickableTextureComponent> _showBillboardButton = new PerScreen<ClickableTextureComponent>(createNewState: () =>
            new ClickableTextureComponent(
                new Rectangle(0, 0, 99, 60), 
                Game1.content.Load<Texture2D>(Path.Combine("Maps", "summer_town")), 
                new Rectangle(122, 291, 35, 20), 
                3f));

        private readonly IModHelper _helper;

        private readonly PerScreen<Item> _hoverItem = new PerScreen<Item>();
        private readonly PerScreen<Item> _heldItem = new PerScreen<Item>();

        public ShowCalendarAndBillboardOnGameMenuButton(IModHelper helper)
        {
            _helper = helper;
        }

        public void ToggleOption(bool showCalendarAndBillboard)
        {
            _helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu;
            _helper.Events.Input.ButtonPressed -= OnButtonPressed;
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

            if (showCalendarAndBillboard)
            {
                _helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
                _helper.Events.Input.ButtonPressed += OnButtonPressed;
                _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            }
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, EventArgs e)
        {
            // get hover item
            _hoverItem.Value = Tools.GetHoveredItem();
            if (Game1.activeClickableMenu is GameMenu gameMenu)
            {
                var menuList = gameMenu.pages;

                if (menuList[0] is InventoryPage inventory)
                {
                    _heldItem.Value = Game1.player.CursorSlotItem;
                }
            }
        }

        public void Dispose()
        {
            ToggleOption(false);
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (e.Button == SButton.MouseLeft)
                ActivateBillboard();
            else if (e.Button == SButton.ControllerA)
                ActivateBillboard();
        }

        private void ActivateBillboard()
        {
            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0
                && _heldItem.Value == null
                && _showBillboardButton.Value.containsPoint((int)Utility.ModifyCoordinateForUIScale(Game1.getMouseX()), (int)Utility.ModifyCoordinateForUIScale(Game1.getMouseY())))
            {
                if (Game1.questOfTheDay != null &&
                    string.IsNullOrEmpty(Game1.questOfTheDay.currentObjective))
                    Game1.questOfTheDay.currentObjective = "wat?";

                Game1.activeClickableMenu =
                    new Billboard(!(Game1.getMouseX() <
                    _showBillboardButton.Value.bounds.X + _showBillboardButton.Value.bounds.Width / 2));
            }
        }

        /// <summary>When a menu is open (<see cref="Game1.activeClickableMenu"/> isn't null), raised after that menu is drawn to the sprite batch but before it's rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedActiveMenu(object sender, EventArgs e)
        {
            if (_hoverItem.Value == null
                && Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0
                && _heldItem.Value == null)
            {
                var billboardButton = _showBillboardButton.Value;
                billboardButton.bounds.X = Game1.activeClickableMenu.xPositionOnScreen + Game1.activeClickableMenu.width - 160;

                billboardButton.bounds.Y = Game1.activeClickableMenu.yPositionOnScreen + Game1.activeClickableMenu.height - 300;
                _showBillboardButton.Value = billboardButton;
                _showBillboardButton.Value.draw(Game1.spriteBatch);
                if (_showBillboardButton.Value.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                {
                    var hoverText = Game1.getMouseX() < 
                                    _showBillboardButton.Value.bounds.X + _showBillboardButton.Value.bounds.Width / 2 ? 
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
