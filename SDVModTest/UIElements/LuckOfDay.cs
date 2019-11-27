using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using UIInfoSuite.Extensions;

namespace UIInfoSuite.UIElements
{
    class LuckOfDay : IDisposable
    {
        private String _hoverText = string.Empty;
        private Color _color = new Color(Color.White.ToVector4());
        private ClickableTextureComponent _icon;
        private readonly IModHelper _helper;

        public void Toggle(bool showLuckOfDay)
        {
            _helper.Events.Player.Warped -= OnWarped;
            _helper.Events.Display.RenderingHud -= OnRenderingHud;
            _helper.Events.Display.RenderedHud -= OnRenderedHud;
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

            if (showLuckOfDay)
            {
                AdjustIconXToBlackBorder();
                _helper.Events.Player.Warped += OnWarped;
                _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
                _helper.Events.Display.RenderingHud += OnRenderingHud;
                _helper.Events.Display.RenderedHud += OnRenderedHud;
            }
        }

        public LuckOfDay(IModHelper helper)
        {
            _helper = helper;
        }

        public void Dispose()
        {
            Toggle(false);
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            // calculate luck
            if (e.IsMultipleOf(30)) // half second
            {
                _color = new Color(Color.White.ToVector4());

                if (Game1.player.DailyLuck < -0.04)
                {
                    _hoverText = _helper.SafeGetString(LanguageKeys.MaybeStayHome);
                    _color.B = 155;
                    _color.G = 155;
                }
                else if (Game1.player.DailyLuck < 0)
                {
                    _hoverText = _helper.SafeGetString(LanguageKeys.NotFeelingLuckyAtAll);
                    _color.B = 165;
                    _color.G = 165;
                    _color.R = 165;
                    _color *= 0.8f;
                }
                else if (Game1.player.DailyLuck <= 0.04)
                {
                    _hoverText = _helper.SafeGetString(LanguageKeys.LuckyButNotTooLucky);
                }
                else
                {
                    _hoverText = _helper.SafeGetString(LanguageKeys.FeelingLucky);
                    _color.B = 155;
                    _color.R = 155;
                }
            }
        }

        /// <summary>Raised after drawing the HUD (item toolbar, clock, etc) to the sprite batch, but before it's rendered to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // draw hover text
            if (_icon.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                IClickableMenu.drawHoverText(Game1.spriteBatch, _hoverText, Game1.dialogueFont);
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            // draw dice icon
            if (!Game1.eventUp)
            {
                Point iconPosition = IconHandler.Handler.GetNewIconPosition();
                _icon.bounds.X = iconPosition.X;
                _icon.bounds.Y = iconPosition.Y;
                _icon.draw(Game1.spriteBatch, _color, 1f);
            }
        }

        /// <summary>Raised after a player warps to a new location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnWarped(object sender, WarpedEventArgs e)
        {
            // adjust icon X to black border
            if (e.IsLocalPlayer)
            {
                AdjustIconXToBlackBorder();
            }
        }

        private void AdjustIconXToBlackBorder()
        {
            _icon = new ClickableTextureComponent("",
                new Rectangle(Tools.GetWidthInPlayArea() - 134,
                    290,
                    10 * Game1.pixelZoom,
                    10 * Game1.pixelZoom),
                "",
                "",
                Game1.mouseCursors,
                new Rectangle(50, 428, 10, 14),
                Game1.pixelZoom,
                false);
        }
    }
}
