﻿using Microsoft.Xna.Framework;
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
    class LuckOfDay: IDisposable
    {
        private String _hoverText = string.Empty;
        private Color _color = new Color(Color.White.ToVector4());
        private ClickableTextureComponent _icon;
        private readonly IModHelper _helper;
        private readonly ModOptionToggle _luckIcon;

        public void Toggle(string identifier, bool showLuckOfDay)
        {
            if (identifier != OptionKeys.ShowLuckIcon)
                return;

            LocationEvents.CurrentLocationChanged -= AdjustIconXToBlackBorder;
            GraphicsEvents.OnPreRenderHudEvent -= DrawDiceIcon;
            GraphicsEvents.OnPostRenderHudEvent -= DrawHoverTextOverEverything;
            GameEvents.HalfSecondTick -= CalculateLuck;

            if (showLuckOfDay)
            {
                AdjustIconXToBlackBorder(null, null);
                LocationEvents.CurrentLocationChanged += AdjustIconXToBlackBorder;
                GameEvents.HalfSecondTick += CalculateLuck;
                GraphicsEvents.OnPreRenderHudEvent += DrawDiceIcon;
                GraphicsEvents.OnPostRenderHudEvent += DrawHoverTextOverEverything;
            }
        }

        public LuckOfDay(ModOptions modOptions, IModHelper helper)
        {
            _helper = helper;

            // Add the option to the menu
            _luckIcon = modOptions.GetOptionWithIdentifier<ModOptionToggle>(OptionKeys.ShowLuckIcon) ?? new ModOptionToggle(OptionKeys.ShowLuckIcon, "Show luck icon");
            _luckIcon.ValueChanged += Toggle;
            modOptions.AddModOption(_luckIcon);

            // initialize it
            Toggle(_luckIcon.identifier, _luckIcon.IsOn);
        }

        public void Dispose()
        {
            Toggle(OptionKeys.ShowLuckIcon, false);
        }

        private void CalculateLuck(object sender, EventArgs e)
        {
            _color = new Color(Color.White.ToVector4());

            if (Game1.dailyLuck < -0.04)
            {
                _hoverText = _helper.SafeGetString(LanguageKeys.MaybeStayHome);
                _color.B = 155;
                _color.G = 155;
            }
            else if (Game1.dailyLuck < 0)
            {
                _hoverText = _helper.SafeGetString(LanguageKeys.NotFeelingLuckyAtAll);
                _color.B = 165;
                _color.G = 165;
                _color.R = 165;
                _color *= 0.8f;
            }
            else if (Game1.dailyLuck <= 0.04)
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

        private void DrawHoverTextOverEverything(object sender, EventArgs e)
        {
            if (_icon.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                IClickableMenu.drawHoverText(Game1.spriteBatch, _hoverText, Game1.dialogueFont);
        }

        private void DrawDiceIcon(object sender, EventArgs e)
        {
            if (!Game1.eventUp)
            {
                Point iconPosition = IconHandler.Handler.GetNewIconPosition();
                _icon.bounds.X = iconPosition.X;
                _icon.bounds.Y = iconPosition.Y;
                _icon.draw(Game1.spriteBatch, _color, 1f);
            }
        }

        private void AdjustIconXToBlackBorder(object sender, EventArgsCurrentLocationChanged e)
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
