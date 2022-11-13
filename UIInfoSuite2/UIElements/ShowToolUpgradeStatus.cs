using Microsoft.Xna.Framework;
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
    internal class ShowToolUpgradeStatus : IDisposable
    {
        #region Properties
        private readonly PerScreen<Rectangle> _toolTexturePosition = new();
        private readonly PerScreen<string> _hoverText = new();
        private readonly PerScreen<Tool> _toolBeingUpgraded = new();
        private readonly PerScreen<ClickableTextureComponent> _toolUpgradeIcon = new();

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
            _toolBeingUpgraded.Value = null;
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


        #region Event subscriptions
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (e.IsOneSecond && _toolBeingUpgraded.Value != Game1.player.toolBeingUpgraded.Value)
                UpdateToolInfo();
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            UpdateToolInfo();
        }

        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            // Draw icon
            if (!Game1.eventUp && _toolBeingUpgraded.Value != null)
            {
                Point iconPosition = IconHandler.Handler.GetNewIconPosition();
                _toolUpgradeIcon.Value =
                    new ClickableTextureComponent(
                        new Rectangle(iconPosition.X, iconPosition.Y, 40, 40),
                        Game1.toolSpriteSheet,
                        _toolTexturePosition.Value,
                        2.5f);

                if (_toolBeingUpgraded.Value.GetType().FullName == "LoveOfCooking.Objects.CookingTool")
                {
                    // Special case for the Love of Cooking mod's frying pan
                    _toolBeingUpgraded.Value.drawInMenu(e.SpriteBatch, iconPosition.ToVector2() - new Vector2(16) / 2, 2.5f / Game1.pixelZoom);
                }
                else
                {
                    _toolUpgradeIcon.Value.draw(e.SpriteBatch);
                }
            }
        }

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // Show text on hover
            if (_toolBeingUpgraded.Value != null && (_toolUpgradeIcon.Value?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false))
            {
                IClickableMenu.drawHoverText(
                    Game1.spriteBatch,
                    _hoverText.Value,
                    Game1.dialogueFont
                );
            }
        }
        #endregion


        #region Logic
        private void UpdateToolInfo()
        {
            if (Game1.player.toolBeingUpgraded.Value == null)
            {
                _toolBeingUpgraded.Value = null;
                return;
            }

            Tool toolBeingUpgraded = _toolBeingUpgraded.Value = Game1.player.toolBeingUpgraded.Value;
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
        #endregion
    }
}
