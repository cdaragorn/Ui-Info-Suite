﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using StardewConfigFramework;

namespace UIInfoSuite.UIElements
{
    class ShowAccurateHearts: IDisposable
    {
        private List<ClickableTextureComponent> _friendNames;
        private SocialPage _socialPage;

        private readonly ModOptionToggle _showHeartFills;

        public ShowAccurateHearts(ModOptions modOptions)
        {

            _showHeartFills = modOptions.GetOptionWithIdentifier<ModOptionToggle>(OptionKeys.ShowHeartFills) ?? new ModOptionToggle(OptionKeys.ShowHeartFills, "Show heart fills");
            _showHeartFills.ValueChanged += ToggleOption;
            modOptions.AddModOption(_showHeartFills);

            ToggleOption(_showHeartFills.identifier, _showHeartFills.IsOn);
        }

        private readonly int[][] _numArray = new int[][]
        {
                        new int[] { 1, 1, 0, 1, 1 },
                        new int[] { 1, 1, 1, 1, 1 },
                        new int[] { 0, 1, 1, 1, 0 },
                        new int[] { 0, 0, 1, 0, 0 }
        };

        public void ToggleOption(string identifier, bool showAccurateHearts)
        {
            if (identifier != OptionKeys.ShowHeartFills)
                return;

            MenuEvents.MenuChanged -= OnMenuChange;
            GraphicsEvents.OnPostRenderGuiEvent -= DrawHeartFills;

            if (showAccurateHearts)
            {
                MenuEvents.MenuChanged += OnMenuChange;
                GraphicsEvents.OnPostRenderGuiEvent += DrawHeartFills;
            }
        }

        public void Dispose()
        {
            ToggleOption(OptionKeys.ShowHeartFills, false);
        }

        private void DrawHeartFills(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu)
            {
                GameMenu gameMenu = Game1.activeClickableMenu as GameMenu;

                if (gameMenu.currentTab == 2)
                {
                    int slotPosition = (int) typeof(SocialPage)
                            .GetField(
                                    "slotPosition",
                                    BindingFlags.Instance | BindingFlags.NonPublic)
                                    .GetValue(_socialPage);
                    int yOffset = 0;

                    for (int i = slotPosition; i < slotPosition + 5 && i <= _friendNames.Count; ++i)
                    {
                        int yPosition = Game1.activeClickableMenu.yPositionOnScreen + 130 + yOffset;
                        yOffset += 112;
                        int[] friendshipValues;
                        if (Game1.player.friendships.TryGetValue(_friendNames[i].name, out friendshipValues))
                        {
                            int friendshipRawValue = friendshipValues[0];

                            if (friendshipRawValue > 0)
                            {
                                int pointsToNextHeart = friendshipRawValue % 250;
                                int numHearts = friendshipRawValue / 250;

                                if (friendshipRawValue < 3000 &&
                                        _friendNames[i].name == Game1.player.spouse ||
                                        friendshipRawValue < 2500)
                                {
                                    DrawEachIndividualSquare(numHearts, pointsToNextHeart, yPosition);
                                    if (!Game1.options.hardwareCursor)
                                        Game1.spriteBatch.Draw(
                                                Game1.mouseCursors,
                                                new Vector2(Game1.getMouseX(), Game1.getMouseY()),
                                                Game1.getSourceRectForStandardTileSheet(
                                                        Game1.mouseCursors, Game1.mouseCursor,
                                                        16,
                                                        16),
                                                Color.White,
                                                0.0f,
                                                Vector2.Zero,
                                                Game1.pixelZoom + (float) (Game1.dialogueButtonScale / 150.0),
                                                SpriteEffects.None,
                                                1f);
                                }
                            }
                        }
                    }

                    String hoverText = typeof(GameMenu).GetField("hoverText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gameMenu) as String;
                    IClickableMenu.drawHoverText(
                            Game1.spriteBatch,
                            hoverText,
                            Game1.smallFont);
                }
            }
        }

        private void OnMenuChange(object sender, EventArgsClickableMenuChanged e)
        {
            if (Game1.activeClickableMenu is GameMenu)
            {
                List<IClickableMenu> menuList = typeof(GameMenu).GetField("pages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Game1.activeClickableMenu) as List<IClickableMenu>;

                foreach (var menu in menuList)
                {
                    if (menu is SocialPage)
                    {
                        _socialPage = menu as SocialPage;
                        _friendNames = typeof(SocialPage).GetField("friendNames", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_socialPage) as List<ClickableTextureComponent>;
                        break;
                    }
                }
            }
        }

        private void DrawEachIndividualSquare(int friendshipLevel, int friendshipPoints, int yPosition)
        {
            int numberOfPointsToDraw = friendshipPoints / 20;
            int num2;

            if (friendshipLevel > 10)
            {
                num2 = 32 * (friendshipLevel - 10);
                yPosition += 28;
            }
            else
            {
                num2 = 32 * friendshipLevel;
            }

            for (int i = 3; i >= 0 && numberOfPointsToDraw > 0; --i)
            {
                for (int j = 0; j < 5 && numberOfPointsToDraw > 0; ++j, --numberOfPointsToDraw)
                {
                    if (_numArray[i][j] == 1)
                    {
                        Game1.spriteBatch.Draw(
                                Game1.staminaRect,
                                new Rectangle(
                                        Game1.activeClickableMenu.xPositionOnScreen + 316 + num2 + j * 4,
                                        yPosition + 14 + i * 4,
                                        4,
                                        4),
                                Color.Crimson);
                    }
                }
            }
        }
    }
}
