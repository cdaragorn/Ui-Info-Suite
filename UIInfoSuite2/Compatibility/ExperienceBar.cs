using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;

// ReSharper disable once CheckNamespace
namespace UIInfoSuite2.UIElements
{
    // This part of the class is only user for compatibility reasons
    public partial class ExperienceBar
    {
        // For providing compatibility for mods that patch this method
        // TODO: When refactoring is done, mark this as deprecated and provide a long-term new method
        // Notable dependents: MARGO -- Modular Gameplay Overhaul
        private void DrawExperienceBar(int barWidth, int experienceGainedThisLevel, int experienceRequiredForNextLevel, int currentLevel)
        {
            float leftSide = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Left;

            if (Game1.isOutdoorMapSmallerThanViewport())
            {
                int num3 = Game1.currentLocation.map.Layers[0].LayerWidth * Game1.tileSize;
                leftSide += (Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right - num3) / 2;
            }

            Game1.drawDialogueBox(
                (int)leftSide,
                Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 160,
                240,
                160,
                false,
                true);

            Game1.spriteBatch.Draw(
                Game1.staminaRect,
                new Rectangle(
                    (int)leftSide + 32,
                    Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 63,
                    barWidth,
                    31),
                _experienceFillColor.Value);

            Game1.spriteBatch.Draw(
                Game1.staminaRect,
                new Rectangle(
                    (int)leftSide + 32,
                    Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 63,
                    Math.Min(4, barWidth),
                    31),
                _experienceFillColor.Value);

            Game1.spriteBatch.Draw(
                Game1.staminaRect,
                new Rectangle(
                    (int)leftSide + 32,
                    Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 63,
                    barWidth,
                    4),
                _experienceFillColor.Value);

            Game1.spriteBatch.Draw(
                Game1.staminaRect,
                new Rectangle(
                    (int)leftSide + 32,
                    Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 36,
                    barWidth,
                    4),
                _experienceFillColor.Value);

            ClickableTextureComponent textureComponent =
                new ClickableTextureComponent(
                    "",
                    new Rectangle(
                        (int)leftSide - 36,
                        Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 80,
                        260,
                        100),
                    "",
                    "",
                    Game1.mouseCursors,
                    new Rectangle(0, 0, 0, 0),
                    Game1.pixelZoom);

            if (textureComponent.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                Game1.drawWithBorder(
                    experienceGainedThisLevel + "/" + experienceRequiredForNextLevel,
                    Color.Black,
                    Color.Black,
                    new Vector2(
                        leftSide + 33,
                        Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 70));
            }
            else
            {
                Game1.spriteBatch.Draw(
                    Game1.mouseCursors,
                    new Vector2(
                        leftSide + 54,
                        Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 62),
                    _levelUpIconRectangle.Value,
                    _experienceFillColor.Value,
                    0,
                    Vector2.Zero,
                    2.9f,
                    SpriteEffects.None,
                    0.85f);

                Game1.drawWithBorder(
                    currentLevel.ToString(),
                    Color.Black * 0.6f,
                    Color.Black,
                    new Vector2(
                        leftSide + 33,
                        Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 70));
            }
        }
    }
}