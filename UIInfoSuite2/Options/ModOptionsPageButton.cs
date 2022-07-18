using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using UIInfoSuite2.Infrastructure;

namespace UIInfoSuite2.Options
{
    internal class ModOptionsPageButton : IClickableMenu
    {

        public Rectangle Bounds { get; }
        //private readonly ModOptionsPageHandler _optionsPageHandler;
        //private bool _hasClicked;

        public event EventHandler OnLeftClicked;

        public ModOptionsPageButton(IModEvents events)
        {
            //_optionsPageHandler = optionsPageHandler;
            width = 64;
            height = 64;
            GameMenu activeClickableMenu = Game1.activeClickableMenu as GameMenu;

            xPositionOnScreen = activeClickableMenu.xPositionOnScreen + activeClickableMenu.width - 200;
            yPositionOnScreen = activeClickableMenu.yPositionOnScreen + 16;
            Bounds = new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height);
            events.Input.ButtonPressed += OnButtonPressed;
            events.Display.MenuChanged += OnMenuChanged;
        }

        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is GameMenu menu)
            {
                xPositionOnScreen = menu.xPositionOnScreen + menu.width - 200;
            }
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        public void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (e.Button == SButton.MouseLeft || e.Button == SButton.ControllerA)
            {
                var x = (int)Utility.ModifyCoordinateForUIScale(e.Cursor.ScreenPixels.X);
                var y = (int)Utility.ModifyCoordinateForUIScale(e.Cursor.ScreenPixels.Y);
                if (isWithinBounds(x, y))
                {
                    receiveLeftClick(x, y);
                    OnLeftClicked?.Invoke(this, null);
                }
            }
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);
            Game1.spriteBatch.Draw(Game1.mouseCursors,
                new Vector2(xPositionOnScreen, yPositionOnScreen),
                new Rectangle(16, 368, 16, 16),
                Color.White,
                0.0f,
                Vector2.Zero,
                Game1.pixelZoom,
                SpriteEffects.None,
                1f);

            b.Draw(Game1.mouseCursors,
                new Vector2(xPositionOnScreen + 8, yPositionOnScreen + 14),
                new Rectangle(32, 672, 16, 16),
                Color.White,
                0.0f,
                Vector2.Zero,
                3f,
                SpriteEffects.None,
                1f);

            if (isWithinBounds(Game1.getMouseX(), Game1.getMouseY()))
            {
                IClickableMenu.drawHoverText(Game1.spriteBatch, "UI Info Mod Options", Game1.smallFont);
            }
            Tools.DrawMouseCursor();
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
        }
    }
}
