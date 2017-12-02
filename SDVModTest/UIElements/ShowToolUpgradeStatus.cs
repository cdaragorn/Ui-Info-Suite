using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIInfoSuite.Extensions;
using StardewConfigFramework;

namespace UIInfoSuite.UIElements
{
    class ShowToolUpgradeStatus : IDisposable
    {
        private readonly IModHelper _helper;
        private Rectangle _toolTexturePosition;
        private String _hoverText;
        private Tool _toolBeingUpgraded;
        private readonly ModOptionToggle _showToolUpgradeStatus;

        public ShowToolUpgradeStatus(ModOptions modOptions, IModHelper helper)
        {
            _helper = helper;

            _showToolUpgradeStatus = modOptions.GetOptionWithIdentifier<ModOptionToggle>(OptionKeys.ShowToolUpgradeStatus) ?? new ModOptionToggle(OptionKeys.ShowToolUpgradeStatus, "Show Traveling Merchant");
            _showToolUpgradeStatus.ValueChanged += ToggleOption;
            modOptions.AddModOption(_showToolUpgradeStatus);

            ToggleOption(_showToolUpgradeStatus.identifier, _showToolUpgradeStatus.IsOn);
        }

        public void ToggleOption(string identifier, bool showToolUpgradeStatus)
        {
            if (OptionKeys.ShowToolUpgradeStatus != identifier)
            {
                return;
            }

            GraphicsEvents.OnPreRenderHudEvent -= DrawToolUpgradeStatus;
            TimeEvents.AfterDayStarted -= DayChanged;
            GameEvents.OneSecondTick -= CheckForMidDayChanges;

            if (showToolUpgradeStatus)
            {
                DayChanged(null, new EventArgsIntChanged(0, Game1.dayOfMonth));
                GraphicsEvents.OnPreRenderHudEvent += DrawToolUpgradeStatus;
                TimeEvents.AfterDayStarted += DayChanged;
                GameEvents.OneSecondTick += CheckForMidDayChanges;
            }
        }

        private void CheckForMidDayChanges(object sender, EventArgs e)
        {
            if (_toolBeingUpgraded != Game1.player.toolBeingUpgraded)
                DayChanged(null, null);
        }

        private void DayChanged(object sender, EventArgs e)
        {
            if (Game1.player.toolBeingUpgraded != null)
            {
                _toolBeingUpgraded = Game1.player.toolBeingUpgraded;
                _toolTexturePosition = new Rectangle();

                if (_toolBeingUpgraded is StardewValley.Tools.WateringCan)
                {
                    _toolTexturePosition.X = 32;
                    _toolTexturePosition.Y = 228;
                    _toolTexturePosition.Width = 16;
                    _toolTexturePosition.Height = 11;
                }
                else
                {
                    _toolTexturePosition.Width = 16;
                    _toolTexturePosition.Height = 16;
                    _toolTexturePosition.X = 81;
                    _toolTexturePosition.Y = 31;

                    if (!(_toolBeingUpgraded is StardewValley.Tools.Hoe))
                    {
                        _toolTexturePosition.Y += 64;

                        if (!(_toolBeingUpgraded is StardewValley.Tools.Pickaxe))
                        {
                            _toolTexturePosition.Y += 64;
                        }
                    }
                }

                _toolTexturePosition.X += (111 * _toolBeingUpgraded.UpgradeLevel);

                if (_toolTexturePosition.X > Game1.toolSpriteSheet.Width)
                {
                    _toolTexturePosition.Y += 32;
                    _toolTexturePosition.X -= 333;
                }

                if (Game1.player.daysLeftForToolUpgrade > 0)
                {
                    _hoverText = String.Format(_helper.SafeGetString(LanguageKeys.DaysUntilToolIsUpgraded),
                        Game1.player.daysLeftForToolUpgrade, _toolBeingUpgraded.DisplayName);
                }
                else
                {
                    _hoverText = String.Format(_helper.SafeGetString(LanguageKeys.ToolIsFinishedBeingUpgraded),
                        _toolBeingUpgraded.DisplayName);
                }
            }
            else
            {
                _toolBeingUpgraded = null;
            }
            
        }

        private void DrawToolUpgradeStatus(object sender, EventArgs e)
        {
            if (!Game1.eventUp &&
                _toolBeingUpgraded != null)
            {
                Point iconPosition = IconHandler.Handler.GetNewIconPosition();
                ClickableTextureComponent textureComponent =
                    new ClickableTextureComponent(
                        new Rectangle(iconPosition.X, iconPosition.Y, 40, 40),
                        Game1.toolSpriteSheet,
                        _toolTexturePosition,
                        2.5f);
                textureComponent.draw(Game1.spriteBatch);

                if (textureComponent.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                {
                    IClickableMenu.drawHoverText(
                        Game1.spriteBatch,
                        _hoverText, Game1.dialogueFont);
                }
            }
        }

        public void Dispose()
        {
            ToggleOption(false);
            _toolBeingUpgraded = null;
        }
    }
}
