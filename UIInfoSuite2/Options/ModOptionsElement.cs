using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;

namespace UIInfoSuite.Options
{
    public class ModOptionsElement
    {
        protected const int DefaultX = 8;
        protected const int DefaultY = 4;
        protected const int DefaultPixelSize = 9;

        private Rectangle _bounds;
        private string _label;
        private int _whichOption;

        protected readonly ModOptionsElement _parent;

        public Rectangle Bounds { get { return _bounds; } }

        public ModOptionsElement(string label, int whichOption = -1, ModOptionsElement parent = null)
        {
            int x = DefaultX * Game1.pixelZoom;
            int y = DefaultY * Game1.pixelZoom;
            int width = DefaultPixelSize * Game1.pixelZoom;
            int height = DefaultPixelSize * Game1.pixelZoom;

            if (parent != null)
                x += DefaultX * 2 * Game1.pixelZoom;

            _bounds = new Rectangle(x, y, width, height);
            _label = label;
            _whichOption = whichOption;

            _parent = parent;
        }

        public virtual void ReceiveLeftClick(int x, int y)
        {

        }

        public virtual void LeftClickHeld(int x, int y)
        {

        }

        public virtual void LeftClickReleased(int x, int y)
        {

        }

        public virtual void ReceiveKeyPress(Keys key)
        {

        }

        public virtual void Draw(SpriteBatch batch, int slotX, int slotY)
        {
            if (_whichOption < 0)
            {
                SpriteText.drawString(batch, _label, slotX + _bounds.X, slotY + _bounds.Y + Game1.pixelZoom * 3, 999, -1, 999, 1, 0.1f);
            }
            else
            {
                Utility.drawTextWithShadow(batch,
                    _label,
                    Game1.dialogueFont,
                    new Vector2(slotX + _bounds.X + _bounds.Width + Game1.pixelZoom * 2, slotY + _bounds.Y),
                    Game1.textColor,
                    1f,
                    0.1f);
            }
        }
    }
}
