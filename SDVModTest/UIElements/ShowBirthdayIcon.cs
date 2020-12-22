using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UIInfoSuite.Extensions;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;

namespace UIInfoSuite.UIElements
{
    class ShowBirthdayIcon : IDisposable
    {
        private NPC _birthdayNPC;
        private ClickableTextureComponent _birthdayIcon;
        private readonly IModEvents _events;

        public ShowBirthdayIcon(IModEvents events)
        {
            _events = events;
        }

        public void ToggleOption(bool showBirthdayIcon)
        {
            _events.GameLoop.DayStarted -= OnDayStarted;
            _events.Display.RenderingHud -= OnRenderingHud;
            _events.Display.RenderedHud -= OnRenderedHud;
            _events.GameLoop.UpdateTicked -= OnUpdateTicked;

            if (showBirthdayIcon)
            {
                CheckForBirthday();
                _events.GameLoop.DayStarted += OnDayStarted;
                _events.Display.RenderingHud += OnRenderingHud;
                _events.Display.RenderedHud += OnRenderedHud;
                _events.GameLoop.UpdateTicked += OnUpdateTicked;
            }
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            // check if gift has been given
            if (e.IsOneSecond && _birthdayNPC != null && Game1.player?.friendshipData != null)
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

        /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            CheckForBirthday();
        }

        private void CheckForBirthday()
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

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud(object sender, EventArgs e)
        {
            // draw birthday icon
            if (!Game1.eventUp)
            {
                if (_birthdayNPC != null)
                {
                    var headShot = _birthdayNPC.GetHeadShot();
                    var iconPosition = IconHandler.Handler.GetNewIconPosition();
                    var scale = 2.9f;

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

        /// <summary>Raised after drawing the HUD (item toolbar, clock, etc) to the sprite batch, but before it's rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // draw hover text
            if (_birthdayNPC != null && 
                (_birthdayIcon?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false))
            {
                var hoverText = string.Format("{0}'s Birthday", _birthdayNPC.Name);
                IClickableMenu.drawHoverText(
                    Game1.spriteBatch,
                    hoverText,
                    Game1.dialogueFont);
            }
        }
    }
}
