using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using UIInfoSuite.Infrastructure;
using UIInfoSuite.Infrastructure.Extensions;

namespace UIInfoSuite.UIElements
{
    class ShowToolUpgradeStatus : IDisposable
    {
        #region Properties
        private Tool _toolBeingUpgraded;
        private Rectangle _toolTexturePosition;
        private ClickableTextureComponent _toolUpgradeIcon;
        private string _hoverText;

        private readonly IModHelper _helper;
        #endregion

        #region Life cycle
        public ShowToolUpgradeStatus(IModHelper helper)
        {
            _helper = helper;
        }

        public void Dispose()
        {
            ToggleOption(false);
            _toolBeingUpgraded = null;
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
        #endregion

        #region Logic
        private void UpdateToolInfo()
        {
            if (Game1.player.toolBeingUpgraded.Value != null)
            {
                _toolBeingUpgraded = Game1.player.toolBeingUpgraded.Value;
                _toolTexturePosition = new Rectangle();

                SetToolSprite();

                UpdateToolSpriteWithLevel();

                SetToolHoverText();
            }
            else
            {
                _toolBeingUpgraded = null;
            }
        }

        private void SetToolSprite()
        {
            // Height : different for watering can and trash can
            if (_toolBeingUpgraded is StardewValley.Tools.Hoe || _toolBeingUpgraded is StardewValley.Tools.Pickaxe
                || _toolBeingUpgraded is StardewValley.Tools.Axe)
            {
                _toolTexturePosition.Width = 16;
                _toolTexturePosition.Height = 16;
            }
            else if (_toolBeingUpgraded is StardewValley.Tools.WateringCan)
            {
                _toolTexturePosition.Width = 16;
                _toolTexturePosition.Height = 11;
            }
            else
            {
                _toolTexturePosition.Width = 16;
                _toolTexturePosition.Height = 16;
            }

            // Position : default generic case is trash can, because that does not have a type in StardewValley.Tools
            if (_toolBeingUpgraded is StardewValley.Tools.WateringCan)
            {
                _toolTexturePosition.X = 32;
                _toolTexturePosition.Y = 228;
            }
            else if (_toolBeingUpgraded is StardewValley.Tools.Hoe)
            {
                _toolTexturePosition.X = 81;
                _toolTexturePosition.Y = 31;
            }
            else if (_toolBeingUpgraded is StardewValley.Tools.Pickaxe)
            {
                _toolTexturePosition.X = 81;
                _toolTexturePosition.Y = 31 + 64;
            }
            else if (_toolBeingUpgraded is StardewValley.Tools.Axe)
            {
                _toolTexturePosition.X = 81;
                _toolTexturePosition.Y = 31 + 64 + 64;
            }
            else if (_toolBeingUpgraded is StardewValley.Tools.GenericTool)
            {
                _toolTexturePosition.X = 208;
                _toolTexturePosition.Y = 0;
            }
        }

        private void UpdateToolSpriteWithLevel()
        {
            // Need to handle trash can separately
            if (_toolBeingUpgraded is StardewValley.Tools.WateringCan || _toolBeingUpgraded is StardewValley.Tools.Hoe
                || _toolBeingUpgraded is StardewValley.Tools.Axe || _toolBeingUpgraded is StardewValley.Tools.Pickaxe)
            {
                _toolTexturePosition.X += (111 * _toolBeingUpgraded.UpgradeLevel);
            }
            else if (_toolBeingUpgraded is StardewValley.Tools.GenericTool)
            {
                _toolTexturePosition.X += (16 * (Game1.player.trashCanLevel + 2));
            }

            // Break into new line
            if (_toolTexturePosition.X > Game1.toolSpriteSheet.Width)
            {
                _toolTexturePosition.Y += 32;
                _toolTexturePosition.X -= 333;
            }
        }

        private void SetToolHoverText()
        {
            if (Game1.player.daysLeftForToolUpgrade.Value > 0)
            {
                _hoverText = string.Format(_helper.SafeGetString(LanguageKeys.DaysUntilToolIsUpgraded),
                    Game1.player.daysLeftForToolUpgrade.Value, _toolBeingUpgraded.DisplayName);
            }
            else
            {
                _hoverText = string.Format(_helper.SafeGetString(LanguageKeys.ToolIsFinishedBeingUpgraded),
                    _toolBeingUpgraded.DisplayName);
            }
        }
        #endregion

        #region Event subscriptions
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (e.IsOneSecond && _toolBeingUpgraded != Game1.player.toolBeingUpgraded.Value)
                UpdateToolInfo();
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            UpdateToolInfo();
        }

        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
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

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            if (_toolBeingUpgraded != null &&
                (_toolUpgradeIcon?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false))
            {
                IClickableMenu.drawHoverText(Game1.spriteBatch,
                        _hoverText, Game1.dialogueFont);
            }
        }
        #endregion
    }
}
