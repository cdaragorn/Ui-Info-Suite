using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using UIInfoSuite.Extensions;

namespace UIInfoSuite.UIElements
{
    class ShowToolUpgradeStatus : IDisposable
    {
        private readonly IModHelper _helper;
        private Rectangle _toolTexturePosition;
        private String _hoverText;
        private Tool _toolBeingUpgraded;
        private ClickableTextureComponent _toolUpgradeIcon;

        public ShowToolUpgradeStatus(IModHelper helper)
        {
            _helper = helper;
        }

        public void ToggleOption(bool showToolUpgradeStatus)
        {
            _helper.Events.Display.RenderingHud -= OnRenderingHud;
            _helper.Events.Display.RenderedHud -= OnRenderedHud;
            _helper.Events.GameLoop.DayStarted -= OnDayStarted;
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

            if (showToolUpgradeStatus)
            {
                UpdateToolInfo();
                _helper.Events.Display.RenderingHud += OnRenderingHud;
                _helper.Events.Display.RenderedHud += OnRenderedHud;
                _helper.Events.GameLoop.DayStarted += OnDayStarted;
                _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            }
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (e.IsOneSecond && _toolBeingUpgraded != Game1.player.toolBeingUpgraded.Value)
                UpdateToolInfo();
        }

        /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            this.UpdateToolInfo();
        }

        private void UpdateToolInfo()
        {
            if (Game1.player.toolBeingUpgraded.Value == null)
            {
                _toolBeingUpgraded = null;
                return;
            }

            Tool toolBeingUpgraded = _toolBeingUpgraded = Game1.player.toolBeingUpgraded.Value;
            Rectangle toolTexturePosition = new Rectangle();

            if (toolBeingUpgraded is StardewValley.Tools.WateringCan)
            {
                toolTexturePosition.X = 32;
                toolTexturePosition.Y = 228;
                toolTexturePosition.Width = 16;
                toolTexturePosition.Height = 11;
                toolTexturePosition.X += (111 * toolBeingUpgraded.UpgradeLevel);
            }
            else
            {
                toolTexturePosition.Width = 16;
                toolTexturePosition.Height = 16;

                if (toolBeingUpgraded is StardewValley.Tools.Hoe)
                {
                    toolTexturePosition.X = 81;
                    toolTexturePosition.Y = 31;
                    toolTexturePosition.X += (111 * toolBeingUpgraded.UpgradeLevel);
                }
                else if (toolBeingUpgraded is StardewValley.Tools.Pickaxe)
                {
                    toolTexturePosition.X = 81;
                    toolTexturePosition.Y = 31 + 64;
                    toolTexturePosition.X += (111 * toolBeingUpgraded.UpgradeLevel);
                }
                else if (toolBeingUpgraded is StardewValley.Tools.Axe)
                {
                    toolTexturePosition.X = 81;
                    toolTexturePosition.Y = 31 + 64 + 64;
                    toolTexturePosition.X += (111 * toolBeingUpgraded.UpgradeLevel);
                }
                else if (toolBeingUpgraded is StardewValley.Tools.GenericTool)
                {
                    toolTexturePosition.X = 208;
                    toolTexturePosition.Y = 0;
                    toolTexturePosition.X += (16 * toolBeingUpgraded.UpgradeLevel);
                }
            }

            if (toolTexturePosition.X > Game1.toolSpriteSheet.Width)
            {
                toolTexturePosition.Y += 32;
                toolTexturePosition.X -= 333;
            }

            if (Game1.player.daysLeftForToolUpgrade.Value > 0)
            {
                _hoverText = string.Format(_helper.SafeGetString(LanguageKeys.DaysUntilToolIsUpgraded),
                    Game1.player.daysLeftForToolUpgrade.Value, toolBeingUpgraded.DisplayName);
            }
            else
            {
                _hoverText = string.Format(_helper.SafeGetString(LanguageKeys.ToolIsFinishedBeingUpgraded),
                    toolBeingUpgraded.DisplayName);
            }

            _toolTexturePosition = toolTexturePosition;
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open). Content drawn to the sprite batch at this point will appear under the HUD.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            // draw tool upgrade status
            if (!Game1.eventUp && _toolBeingUpgraded != null)
            {
                Point iconPosition = IconHandler.Handler.GetNewIconPosition();
                _toolUpgradeIcon =
                    new ClickableTextureComponent(
                        new Rectangle(iconPosition.X, iconPosition.Y, 40, 40),
                        Game1.toolSpriteSheet,
                        _toolTexturePosition,
                        2.5f);
                _toolUpgradeIcon.draw(Game1.spriteBatch);
            }
        }

        /// <summary>Raised after drawing the HUD (item toolbar, clock, etc) to the sprite batch, but before it's rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // draw hover text
            if (_toolBeingUpgraded != null && 
                (_toolUpgradeIcon?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false))
            {
                IClickableMenu.drawHoverText(
                        Game1.spriteBatch,
                        _hoverText, Game1.dialogueFont);
            }
        }

        public void Dispose()
        {
            ToggleOption(false);
            _toolBeingUpgraded = null;
        }
    }
}
