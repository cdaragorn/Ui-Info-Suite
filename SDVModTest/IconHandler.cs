using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace UIInfoSuite {
    class IconHandler
    {
        public static IconHandler Handler { get; private set; }

        static IconHandler()
        {
            if (Handler == null)
                Handler = new IconHandler();
        }

        private int _amountOfVisibleIcons;

        private IconHandler()
        {

        }

        public Point GetNewIconPosition()
        {
            int yPos = Game1.options.zoomButtons ? 290 : 260;
            int xPosition = (int)Tools.GetWidthInPlayArea() - 134 - 46 * _amountOfVisibleIcons;
            ++_amountOfVisibleIcons;
            return new Point(xPosition, yPos);
        }

        public void Reset(object sender, EventArgs e)
        {
            _amountOfVisibleIcons = 0;
        }


    }
}
