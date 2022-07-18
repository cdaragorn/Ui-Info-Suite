using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.UIElements
{
    internal class ShowBirthdayIcon : IDisposable
    {
        #region Properties
        private NPC _birthdayNPC;
        private readonly PerScreen<ClickableTextureComponent> _birthdayIcon = new();
        public bool HideBirthdayIfFullFriendShip { get; set; }
        private readonly IModHelper _helper;
        #endregion


        #region Life cycle
        public ShowBirthdayIcon(IModHelper helper)
        {
            _helper = helper;
        }

        public void Dispose()
        {
            ToggleOption(false);
        }

        public void ToggleOption(bool showBirthdayIcon)
        {
            _helper.Events.GameLoop.DayStarted -= OnDayStarted;
            _helper.Events.Display.RenderingHud -= OnRenderingHud;
            _helper.Events.Display.RenderedHud -= OnRenderedHud;
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

            if (showBirthdayIcon)
            {
                CheckForBirthday();
                _helper.Events.GameLoop.DayStarted += OnDayStarted;
                _helper.Events.Display.RenderingHud += OnRenderingHud;
                _helper.Events.Display.RenderedHud += OnRenderedHud;
                _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            }
        }

        public void ToggleDisableOnMaxFriendshipOption(bool hideBirthdayIfFullFriendShip)
        {
            HideBirthdayIfFullFriendShip = hideBirthdayIfFullFriendShip;
        }

        #endregion


        #region Event subscriptions
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            CheckForGiftGiven(e);
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            CheckForBirthday();
        }

        private void OnRenderingHud(object sender, EventArgs e)
        {
            if (!Game1.eventUp && _birthdayNPC != null)
            {
                DrawBithdayIcon();
            }
        }


        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            if (_birthdayNPC != null && (_birthdayIcon.Value?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false))
            {
                DrawHoverText();
            }
        }
        #endregion


        #region Logic
        private void CheckForGiftGiven(UpdateTickedEventArgs e)
        {
            if (e.IsOneSecond && _birthdayNPC != null && Game1.player?.friendshipData != null)
            {
                Friendship friendship = GetFriendshipWithNPC(_birthdayNPC.Name);
                if (friendship != null && friendship.GiftsToday == 1)
                {
                    _birthdayNPC = null;
                }
            }
        }

        private void CheckForBirthday()
        {
            _birthdayNPC = null;
            foreach (var location in Game1.locations)
            {
                foreach (var character in location.characters)
                {
                    if (character.isBirthday(Game1.currentSeason, Game1.dayOfMonth) &&
                        Game1.player.friendshipData.FieldDict.ContainsKey(character.Name))
                    {
                        if (HideBirthdayIfFullFriendShip)
                        {
                            Friendship friendship = GetFriendshipWithNPC(character.Name);
                            if (friendship != null && friendship.Points >= 2000)
                                break;
                        }

                        _birthdayNPC = character;
                        break;
                    }
                }

                if (_birthdayNPC != null)
                    break;
            }
        }

        private static Friendship GetFriendshipWithNPC(string name)
        {
            try
            {
                Game1.player.friendshipData.FieldDict.TryGetValue(name, out var netRef);
                Friendship birthdayNPCDetails = netRef;
                return birthdayNPCDetails;
            }
            catch (Exception ex)
            {
                ModEntry.MonitorObject.Log("Error while getting information about the birthday of " + name + ": " + ex.Message + Environment.NewLine + ex.StackTrace, LogLevel.Error);
            }

            return null;
        }

        private void DrawBithdayIcon()
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

            _birthdayIcon.Value =
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

            _birthdayIcon.Value.draw(Game1.spriteBatch);
        }

        private void DrawHoverText()
        {
            string hoverText = string.Format(_helper.SafeGetString(LanguageKeys.NpcBirthday), _birthdayNPC.displayName);
            IClickableMenu.drawHoverText(
                Game1.spriteBatch,
                hoverText,
                Game1.dialogueFont);
        }
        #endregion
    }
}
