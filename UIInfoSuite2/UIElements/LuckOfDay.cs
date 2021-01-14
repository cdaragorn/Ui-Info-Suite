using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewModdingAPI.Utilities;
using System;
using UIInfoSuite.Infrastructure;
using UIInfoSuite.Infrastructure.Extensions;

namespace UIInfoSuite.UIElements
{
    class LuckOfDay : IDisposable
    {
        private readonly PerScreen<string> _hoverText = new PerScreen<string>(createNewState: () => string.Empty);
        private readonly PerScreen<Color> _color = new PerScreen<Color>(createNewState: () => new Color(Color.White.ToVector4()));
        private readonly PerScreen<ClickableTextureComponent> _icon = new PerScreen<ClickableTextureComponent>(createNewState: () => new ClickableTextureComponent("",
                new Rectangle(Tools.GetWidthInPlayArea() - 134,
                    290,
                    10 * Game1.pixelZoom,
                    10 * Game1.pixelZoom),
                "",
                "",
                Game1.mouseCursors,
                new Rectangle(50, 428, 10, 14),
                Game1.pixelZoom,
                false));
        private readonly IModHelper _helper;

        private readonly static Color _maybeStayHomeColor = new Color(255, 155, 155, 255);
        private readonly static Color _notFeelingLuckyAtAllColor = new Color(132, 132, 132, 255);
        private readonly static Color _luckyButNotTooLuckyColor = new Color(255, 255, 255, 255);
        private readonly static Color _feelingLuckyColor = new Color(155, 255, 155, 255);

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
                if (Game1.player.DailyLuck < -0.04)
                {
                    _hoverText.Value = _helper.SafeGetString(LanguageKeys.MaybeStayHome);
                    _color.Value = _maybeStayHomeColor;
                }
                else if (Game1.player.DailyLuck < 0)
                {
                    _hoverText.Value = _helper.SafeGetString(LanguageKeys.NotFeelingLuckyAtAll);
                    _color.Value = _notFeelingLuckyAtAllColor;
                }
                else if (Game1.player.DailyLuck <= 0.04)
                {
                    _hoverText.Value = _helper.SafeGetString(LanguageKeys.LuckyButNotTooLucky);
                    _color.Value = _luckyButNotTooLuckyColor;
                }
                else
                {
                    _hoverText.Value = _helper.SafeGetString(LanguageKeys.FeelingLucky);
                    _color.Value = _feelingLuckyColor;
                }
            }
        }

        /// <summary>Raised after drawing the HUD (item toolbar, clock, etc) to the sprite batch, but before it's rendered to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // draw hover text
            if (_icon.Value.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                IClickableMenu.drawHoverText(Game1.spriteBatch, _hoverText.Value, Game1.dialogueFont);
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
                var icon = _icon.Value;
                icon.bounds.X = iconPosition.X;
                icon.bounds.Y = iconPosition.Y;
                _icon.Value = icon;
                _icon.Value.draw(Game1.spriteBatch, _color.Value, 1f);
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
            _icon.Value = new ClickableTextureComponent("",
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
