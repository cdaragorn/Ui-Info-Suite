using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System;
using UIInfoSuite.Extensions;

namespace UIInfoSuite.UIElements
{
    class ShowToolUpgradeStatus : IDisposable
    {
        private readonly IModHelper _helper;
        private readonly PerScreen<Rectangle> _toolTexturePosition = new PerScreen<Rectangle>();
        private readonly PerScreen<string> _hoverText = new PerScreen<string>();
        private readonly PerScreen<Tool> _toolBeingUpgraded = new PerScreen<Tool>();
        private readonly PerScreen<ClickableTextureComponent> _toolUpgradeIcon = new PerScreen<ClickableTextureComponent>();

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
            if (e.IsOneSecond && _toolBeingUpgraded.Value != Game1.player.toolBeingUpgraded.Value)
                UpdateToolInfo();
        }

        /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            UpdateToolInfo();
        }

        private void UpdateToolInfo()
        {
            // 
            if (Game1.player.toolBeingUpgraded.Value != null)
            {
                var toolBeingUpgraded = _toolBeingUpgraded.Value = Game1.player.toolBeingUpgraded.Value;
                var toolTexturePosition = new Rectangle();
                // default hoe
                toolTexturePosition.Width = 16;
                toolTexturePosition.Height = 16;
                toolTexturePosition.X = 81 + (111 * toolBeingUpgraded.UpgradeLevel);
                toolTexturePosition.Y = 31;                 
                    
                if (toolBeingUpgraded is StardewValley.Tools.WateringCan)
                {
                    toolTexturePosition.X = 32 + (111 * toolBeingUpgraded.UpgradeLevel);
                    toolTexturePosition.Y = 228;
                    toolTexturePosition.Height = 11;
                }
                else if (toolBeingUpgraded is StardewValley.Tools.GenericTool)
                {
                    toolTexturePosition.X = 208 (16 * Game1.player.trashCanLevel);
                    toolTexturePosition.Y = 0;
                }
                else
                {
                    if (toolBeingUpgraded is StardewValley.Tools.Pickaxe)
                    {
                        toolTexturePosition.Y += 64;
                    }
                    else if (toolBeingUpgraded is StardewValley.Tools.Axe)
                    {
                        toolTexturePosition.Y += 128;
                    }              
                }
                
                if (toolTexturePosition.X > Game1.toolSpriteSheet.Width)
                {
                    toolTexturePosition.Y += 32;
                    toolTexturePosition.X -= 333;
                }

                if (Game1.player.daysLeftForToolUpgrade.Value > 0)
                {
                    _hoverText.Value = string.Format(_helper.SafeGetString(LanguageKeys.DaysUntilToolIsUpgraded),
                        Game1.player.daysLeftForToolUpgrade.Value, toolBeingUpgraded.DisplayName);
                }
                else
                {
                    _hoverText.Value = string.Format(_helper.SafeGetString(LanguageKeys.ToolIsFinishedBeingUpgraded),
                        toolBeingUpgraded.DisplayName);
                }

                _toolTexturePosition.Value = toolTexturePosition;
            }
            else
            {
                _toolBeingUpgraded.Value = null;
            }
            
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open). Content drawn to the sprite batch at this point will appear under the HUD.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            // draw tool upgrade status
            if (!Game1.eventUp && _toolBeingUpgraded.Value != null)
            {
                var iconPosition = IconHandler.Handler.GetNewIconPosition();
                _toolUpgradeIcon.Value =
                    new ClickableTextureComponent(
                        new Rectangle(iconPosition.X, iconPosition.Y, 40, 40),
                        Game1.toolSpriteSheet,
                        _toolTexturePosition.Value,
                        2.5f);
                _toolUpgradeIcon.Value.draw(Game1.spriteBatch);
            }
        }

        /// <summary>Raised after drawing the HUD (item toolbar, clock, etc) to the sprite batch, but before it's rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // draw hover text
            if (_toolBeingUpgraded.Value != null && 
                (_toolUpgradeIcon.Value?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false))
            {
                IClickableMenu.drawHoverText(
                        Game1.spriteBatch,
                        _hoverText.Value, Game1.dialogueFont);
            }
        }

        public void Dispose()
        {
            ToggleOption(false);
            _toolBeingUpgraded.Value = null;
        }
    }
}
