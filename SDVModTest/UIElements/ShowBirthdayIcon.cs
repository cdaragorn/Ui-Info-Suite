using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UIInfoSuite.Extensions;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;

namespace UIInfoSuite.UIElements {
    class ShowBirthdayIcon : IDisposable
    {
        private NPC _birthdayNPC;
        private ClickableTextureComponent _birthdayIcon;

        public void ToggleOption(bool showBirthdayIcon)
        {
            TimeEvents.AfterDayStarted -= CheckForBirthday;
            GraphicsEvents.OnPreRenderHudEvent -= DrawBirthdayIcon;
            GraphicsEvents.OnPostRenderHudEvent -= DrawHoverText;
            GameEvents.HalfSecondTick -= CheckIfGiftHasBeenGiven;

            if (showBirthdayIcon)
            {
                CheckForBirthday(null, null);
                TimeEvents.AfterDayStarted += CheckForBirthday;
                GraphicsEvents.OnPreRenderHudEvent += DrawBirthdayIcon;
                GraphicsEvents.OnPostRenderHudEvent += DrawHoverText;
                GameEvents.HalfSecondTick += CheckIfGiftHasBeenGiven;
            }
        }

        private void CheckIfGiftHasBeenGiven(object sender, EventArgs e)
        {
            if (_birthdayNPC != null &&
                Game1.player != null &&
                Game1.player.friendshipData != null)
            {
                Game1.player.friendshipData.FieldDict.TryGetValue(_birthdayNPC.Name, out var netRef);
                //var birthdayNPCDetails = Game1.player.friendshipData.SafeGet(_birthdayNPC.name);
                Friendship birthdayNPCDetails = netRef;
                if (birthdayNPCDetails != null)
                {
                    if (birthdayNPCDetails.GiftsToday == 1)
                        _birthdayNPC = null;
                }
            }
        }

        public void Dispose()
        {
            ToggleOption(false);
        }

        private void CheckForBirthday(object sender, EventArgs e)
        {
            _birthdayNPC = null;
            foreach (var location in Game1.locations)
            {
                foreach (var character in location.characters)
                {
                    if (character.isBirthday(Game1.currentSeason, Game1.dayOfMonth))
                    {
                        _birthdayNPC = character;
                        break;
                    }
                }
                
                if (_birthdayNPC != null)
                    break;
            }
        }

        private void DrawBirthdayIcon(object sender, EventArgs e)
        {
            if (!Game1.eventUp)
            {
                if (_birthdayNPC != null)
                {
                    Rectangle headShot = _birthdayNPC.GetHeadShot();
                    Point iconPosition = IconHandler.Handler.GetNewIconPosition();
                    float scale = 2.9f;

                    Game1.spriteBatch.Draw(
                        Game1.mouseCursors,
                        new Vector2(iconPosition.X, iconPosition.Y),
                        new Rectangle(228, 409, 16, 16),
                        Color.White,
                        0.0f,
                        Vector2.Zero,
                        scale,
                        SpriteEffects.None,
                        1f);

                    _birthdayIcon =
                        new ClickableTextureComponent(
                            _birthdayNPC.Name,
                            new Rectangle(
                                iconPosition.X - 7,
                                iconPosition.Y - 2,
                                (int)(16.0 * scale),
                                (int)(16.0 * scale)),
                            null,
                            _birthdayNPC.Name,
                            _birthdayNPC.Sprite.Texture,
                            headShot,
                            2f);

                    _birthdayIcon.draw(Game1.spriteBatch);
                }
            }
        }

        private void DrawHoverText(object sender, EventArgs e)
        {
            if (_birthdayNPC != null &&
                _birthdayIcon.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                String hoverText = String.Format("{0}'s Birthday", _birthdayNPC.Name);
                IClickableMenu.drawHoverText(
                    Game1.spriteBatch,
                    hoverText,
                    Game1.dialogueFont);
            }
        }
    }
}
