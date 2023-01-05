using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace UIInfoSuite2.UIElements
{
    public class DisplayedLevelUpMessage
    {
        public void Draw(Rectangle levelUpIconRectangle, string levelUpMessage)
        {
            Vector2 playerLocalPosition = Game1.player.getLocalPosition(Game1.viewport);

            Game1.spriteBatch.Draw(
                Game1.mouseCursors,
                Utility.ModifyCoordinatesForUIScale(new Vector2(
                    playerLocalPosition.X - 74,
                    playerLocalPosition.Y - 130)), levelUpIconRectangle,
                Color.White,
                0,
                Vector2.Zero,
                Game1.pixelZoom,
                SpriteEffects.None,
                0.85f);

            Game1.drawWithBorder(
                levelUpMessage,
                Color.DarkSlateGray,
                Color.PaleTurquoise,
                Utility.ModifyCoordinatesForUIScale(new Vector2(
                    playerLocalPosition.X - 28,
                    playerLocalPosition.Y - 130)));
        }
    }
}
