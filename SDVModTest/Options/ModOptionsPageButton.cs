using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace UIInfoSuite.Options
{
    class ModOptionsPageButton : IClickableMenu
    {

        public Rectangle Bounds { get; }
        //private readonly ModOptionsPageHandler _optionsPageHandler;
        //private bool _hasClicked;

        public event EventHandler OnLeftClicked;

        public ModOptionsPageButton()
        {
            //_optionsPageHandler = optionsPageHandler;
            width = 64;
            height = 64;
            GameMenu activeClickableMenu = Game1.activeClickableMenu as GameMenu;

            xPositionOnScreen = activeClickableMenu.xPositionOnScreen + activeClickableMenu.width - 200;
            yPositionOnScreen = activeClickableMenu.yPositionOnScreen + 16;
            Bounds = new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height);
            ControlEvents.MouseChanged += OnMouseChanged;
            ControlEvents.ControllerButtonPressed += ControlEvents_ControllerButtonPressed;
            MenuEvents.MenuChanged += MenuEvents_MenuChanged;
        }

        private void MenuEvents_MenuChanged(object sender, EventArgsClickableMenuChanged e)
        {
            if (Game1.activeClickableMenu is GameMenu)
            {
                GameMenu menu = Game1.activeClickableMenu as GameMenu;
                xPositionOnScreen = menu.xPositionOnScreen + menu.width - 200;
            }
        }

        private void ControlEvents_ControllerButtonPressed(object sender, EventArgsControllerButtonPressed e)
        {
            if (e.ButtonPressed == Buttons.A &&
                isWithinBounds(Game1.getMouseX(), Game1.getMouseY()))
            {
                receiveLeftClick(Game1.getMouseX(), Game1.getMouseY());
                OnLeftClicked?.Invoke(this, null);
            }
        }

        public void OnMouseChanged(object sender, EventArgsMouseStateChanged e)
        {
            if (e.PriorState.LeftButton != ButtonState.Pressed &&
                e.NewState.LeftButton == ButtonState.Pressed &&
                isWithinBounds(e.NewPosition.X, e.NewPosition.Y))
            {
                receiveLeftClick(e.NewPosition.X, e.NewPosition.Y);
                OnLeftClicked?.Invoke(this, null);
            }
            //if (e.NewState.LeftButton != ButtonState.Pressed || !(Game1.activeClickableMenu is GameMenu))
            //{
            //    _hasClicked = false;
            //}
            //else if ((Game1.activeClickableMenu as GameMenu).currentTab != 3 && 
            //    isWithinBounds(e.NewPosition.X, e.NewPosition.Y) && 
            //    !_hasClicked)
            //{
            //    receiveLeftClick(e.NewPosition.X, e.NewPosition.Y);
                
            //}
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
