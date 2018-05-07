using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using UIInfoSuite.Extensions;

namespace UIInfoSuite.UIElements
{
    class ExperienceBar : IDisposable
    {
        private const int MaxBarWidth = 175;
        private int _currentLevelIndex = 4;
        private float _currentExperience = 0;
        private readonly List<ExperiencePointDisplay> _experiencePointDisplays = new List<ExperiencePointDisplay>();
        private GameLocation _currentLocation = new GameLocation();
        private readonly TimeSpan _levelUpPauseTime = TimeSpan.FromSeconds(2);
        private Color _iconColor = Color.White;
        private Color _experienceFillColor = Color.Blue;
        private Item _previousItem = null;
        private bool _experienceBarShouldBeVisible = false;
        private bool _shouldDrawLevelUp = false;
        private System.Timers.Timer _timeToDisappear = new System.Timers.Timer();
        private readonly TimeSpan _timeBeforeExperienceBarFades = TimeSpan.FromSeconds(8);
        //private SoundEffectInstance _soundEffect;
        private Rectangle _levelUpIconRectangle = new Rectangle(120, 428, 10, 10);
        private bool _allowExperienceBarToFadeOut = true;
        private bool _showExperienceGain = true;
        private bool _showLevelUpAnimation = true;
        private bool _showExperienceBar = true;
        private readonly IModHelper _helper;
        private SoundPlayer _player;

        public ExperienceBar(IModHelper helper)
        {
            _helper = helper;
            String path = string.Empty;
            try
            {
                path = Path.Combine(_helper.DirectoryPath, "LevelUp.wav");
                _player = new SoundPlayer(path);
                //path = path.Replace(Environment.CurrentDirectory, "");
                //path = path.TrimStart(Path.DirectorySeparatorChar);
                //_soundEffect = SoundEffect.FromStream(TitleContainer.OpenStream(path)).CreateInstance();
                //_soundEffect.Volume = 1f;
            }
            catch (Exception ex)
            {
                ModEntry.MonitorObject.Log("Error loading sound file from " + path + ": " + ex.Message + Environment.NewLine + ex.StackTrace, LogLevel.Error);
            }
            _timeToDisappear.Elapsed += StopTimerAndFadeBarOut;
            GraphicsEvents.OnPreRenderHudEvent += OnPreRenderHudEvent;
            PlayerEvents.Warped += RemoveAllExperiencePointDisplays;
        }

        public void Dispose()
        {
            PlayerEvents.LeveledUp -= OnLevelUp;
            GraphicsEvents.OnPreRenderHudEvent -= OnPreRenderHudEvent;
            PlayerEvents.Warped -= RemoveAllExperiencePointDisplays;
            _timeToDisappear.Stop();
            _timeToDisappear.Dispose();
        }

        public void ToggleLevelUpAnimation(bool showLevelUpAnimation)
        {
            _showLevelUpAnimation = showLevelUpAnimation;
            PlayerEvents.LeveledUp -= OnLevelUp;

            if (_showLevelUpAnimation)
            {
                PlayerEvents.LeveledUp += OnLevelUp;
            }
        }

        public void ToggleExperienceBarFade(bool allowExperienceBarToFadeOut)
        {
            _allowExperienceBarToFadeOut = allowExperienceBarToFadeOut;
        }

        public void ToggleShowExperienceGain(bool showExperienceGain)
        {
            _showExperienceGain = showExperienceGain;
        }

        public void ToggleShowExperienceBar(bool showExperienceBar)
        {
            //GraphicsEvents.OnPreRenderHudEvent -= OnPreRenderHudEvent;
            //PlayerEvents.Warped -= RemoveAllExperiencePointDisplays;
            _showExperienceBar = showExperienceBar;
            //if (showExperienceBar)
            //{
            //    GraphicsEvents.OnPreRenderHudEvent += OnPreRenderHudEvent;
            //    PlayerEvents.Warped += RemoveAllExperiencePointDisplays;
            //}
        }

        private void OnLevelUp(object sender, EventArgsLevelUp e)
        {
            if (_showLevelUpAnimation)
            {
                switch (e.Type)
                {
                    case EventArgsLevelUp.LevelType.Combat: _levelUpIconRectangle.X = 120; break;
                    case EventArgsLevelUp.LevelType.Farming: _levelUpIconRectangle.X = 10; break;
                    case EventArgsLevelUp.LevelType.Fishing: _levelUpIconRectangle.X = 20; break;
                    case EventArgsLevelUp.LevelType.Foraging: _levelUpIconRectangle.X = 60; break;
                    case EventArgsLevelUp.LevelType.Mining: _levelUpIconRectangle.X = 30; break;
                }
                _shouldDrawLevelUp = true;
                _timeToDisappear.Interval = _timeBeforeExperienceBarFades.TotalMilliseconds;
                _timeToDisappear.Start();
                _experienceBarShouldBeVisible = true;

                float previousAmbientVolume = Game1.options.ambientVolumeLevel;
                float previousMusicVolume = Game1.options.musicVolumeLevel;

                //if (_soundEffect != null)
                //    _soundEffect.Volume = previousMusicVolume <= 0.01f ? 0 : Math.Min(1, previousMusicVolume + 0.3f);

                //Task.Factory.StartNew(() =>
                //{
                //    Thread.Sleep(100);
                //    Game1.musicCategory.SetVolume((float)Math.Max(0, Game1.options.musicVolumeLevel - 0.3));
                //    Game1.ambientCategory.SetVolume((float)Math.Max(0, Game1.options.ambientVolumeLevel - 0.3));
                //    if (_soundEffect != null)
                //        _soundEffect.Play();
                //});

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(100);
                    _player.Play();
                });

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(_levelUpPauseTime);
                    _shouldDrawLevelUp = false;
                    //Game1.musicCategory.SetVolume(previousMusicVolume);
                    //Game1.ambientCategory.SetVolume(previousAmbientVolume);
                });
            }
        }

        private void StopTimerAndFadeBarOut(object sender, ElapsedEventArgs e)
        {
            _timeToDisappear.Stop();
            _experienceBarShouldBeVisible = false;
        }

        private void RemoveAllExperiencePointDisplays(object sender, EventArgsPlayerWarped e)
        {
            _experiencePointDisplays.Clear();
        }

        private void OnPreRenderHudEvent(object sender, EventArgs e)
        {
            if (!Game1.eventUp)
            {
                Item currentItem = Game1.player.CurrentItem;
                Rectangle rectangle1 = new Rectangle(10, 428, 10, 10);
                int experienceLevel;

                if (currentItem is FishingRod)
                {
                    _experienceFillColor = new Color(17, 84, 252, 0.63f);
                    _currentLevelIndex = 1;
                    rectangle1.X = 20;
                    experienceLevel = Game1.player.fishingLevel;
                }
                else if (currentItem is Pickaxe)
                {
                    _experienceFillColor = new Color(145, 104, 63, 0.63f);
                    _currentLevelIndex = 3;
                    rectangle1.X = 30;
                    experienceLevel = Game1.player.miningLevel;
                }
                else if (currentItem is MeleeWeapon &&
                    currentItem.Name != "Scythe")
                {
                    _experienceFillColor = new Color(204, 0, 3, 0.63f);
                    _currentLevelIndex = 4;
                    rectangle1.X = 120;
                    experienceLevel = Game1.player.combatLevel;
                }
                else if (Game1.currentLocation is Farm &&
                    !(currentItem is Axe))
                {
                    _experienceFillColor = new Color(255, 251, 35, 0.38f);
                    _currentLevelIndex = 0;
                    rectangle1.X = 10;
                    experienceLevel = Game1.player.farmingLevel;
                }
                else
                {
                    _experienceFillColor = new Color(0, 234, 0, 0.63f);
                    _currentLevelIndex = 2;
                    rectangle1.X = 60;
                    experienceLevel = Game1.player.foragingLevel;
                }

                if (experienceLevel <= 9)
                {
                    int experienceRequiredToLevel = GetExperienceRequiredToLevel(experienceLevel);
                    int experienceFromPreviousLevels = GetExperienceRequiredToLevel(experienceLevel - 1);
                    int experienceEarnedThisLevel = Game1.player.experiencePoints[_currentLevelIndex] - experienceFromPreviousLevels;

                    if (_previousItem != currentItem)
                    {
                        ShowExperienceBar();
                    }
                    else if (_currentExperience != experienceEarnedThisLevel)
                    {
                        ShowExperienceBar();
                        if (experienceEarnedThisLevel - _currentExperience > 0 &&
                        _showExperienceGain)
                        {
                            _experiencePointDisplays.Add(
                                new ExperiencePointDisplay(
                                    experienceEarnedThisLevel - _currentExperience,
                                    Game1.player.getLocalPosition(Game1.viewport)));
                        }
                    }
                    
                    _previousItem = currentItem;
                    _currentExperience = experienceEarnedThisLevel;

                    if (_shouldDrawLevelUp)
                    {
                        Vector2 playerLocalPosition = Game1.player.getLocalPosition(Game1.viewport);
                        Game1.spriteBatch.Draw(
                            Game1.mouseCursors,
                            new Vector2(
                                playerLocalPosition.X - 74,
                                playerLocalPosition.Y - 130),
                            _levelUpIconRectangle,
                            _iconColor,
                            0,
                            Vector2.Zero,
                            Game1.pixelZoom,
                            SpriteEffects.None,
                            0.85f);

                        Game1.drawWithBorder(
                            _helper.SafeGetString(
                                LanguageKeys.LevelUp),
                            Color.DarkSlateGray,
                            Color.PaleTurquoise,
                            new Vector2(
                                playerLocalPosition.X - 28,
                                playerLocalPosition.Y - 130));
                    }

                    for (int i = _experiencePointDisplays.Count - 1; i >= 0; --i)
                    {
                        if (_experiencePointDisplays[i].IsInvisible)
                        {
                            _experiencePointDisplays.RemoveAt(i);
                        }
                        else
                        {
                            _experiencePointDisplays[i].Draw();
                        }
                    }

                    if (_experienceBarShouldBeVisible &&
                        _showExperienceBar)
                    {
                        int barWidth = (int)(_currentExperience / (experienceRequiredToLevel - experienceFromPreviousLevels) * MaxBarWidth);
                        float leftSide = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Left;

                        if (Game1.isOutdoorMapSmallerThanViewport())
                        {
                            int num3 = Game1.currentLocation.map.Layers[0].LayerWidth * Game1.tileSize;
                            leftSide += (Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right - num3) / 2;
                        }

                        Game1.drawDialogueBox(
                            (int)leftSide,
                            Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 160,
                            240,
                            160,
                            false,
                            true);

                        Game1.spriteBatch.Draw(
                            Game1.staminaRect,
                            new Rectangle(
                                (int)leftSide + 32,
                                Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 63,
                                barWidth,
                                31),
                            _experienceFillColor);

                        Game1.spriteBatch.Draw(
                            Game1.staminaRect,
                            new Rectangle(
                                (int)leftSide + 32,
                                Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 63,
                                Math.Min(4, barWidth),
                                31),
                            _experienceFillColor);

                        Game1.spriteBatch.Draw(
                            Game1.staminaRect,
                            new Rectangle(
                                (int)leftSide + 32,
                                Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 63,
                                barWidth,
                                4),
                            _experienceFillColor);

                        Game1.spriteBatch.Draw(
                            Game1.staminaRect,
                            new Rectangle(
                                (int)leftSide + 32,
                                Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 36,
                                barWidth,
                                4),
                            _experienceFillColor);

                        ClickableTextureComponent textureComponent =
                            new ClickableTextureComponent(
                                "",
                                new Rectangle(
                                    (int)leftSide - 36,
                                    Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 80,
                                    260,
                                    100),
                                "",
                                "",
                                Game1.mouseCursors,
                                new Rectangle(0, 0, 0, 0),
                                Game1.pixelZoom);

                        if (textureComponent.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                        {
                            Game1.drawWithBorder(
                                _currentExperience + "/" + (experienceRequiredToLevel - experienceFromPreviousLevels),
                                Color.Black,
                                Color.Black,
                                new Vector2(
                                    leftSide + 33,
                                    Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 70));
                        }
                        else
                        {
                            Game1.spriteBatch.Draw(
                                Game1.mouseCursors,
                                new Vector2(
                                    leftSide + 54,
                                    Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 62),
                                rectangle1,
                                _iconColor,
                                0,
                                Vector2.Zero,
                                2.9f,
                                SpriteEffects.None,
                                0.85f);

                            Game1.drawWithBorder(
                                experienceLevel.ToString(),
                                Color.Black * 0.6f,
                                Color.Black,
                                new Vector2(
                                    leftSide + 33,
                                    Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 70));
                        }

                        
                    }
                }

            }
        }

        private int GetExperienceRequiredToLevel(int currentLevel)
        {
            int amount = 0;
            switch (currentLevel)
            {
                case 0: amount = 100; break;
                case 1: amount = 380; break;
                case 2: amount = 770; break;
                case 3: amount = 1300; break;
                case 4: amount = 2150; break;
                case 5: amount = 3300; break;
                case 6: amount = 4800; break;
                case 7: amount = 6900; break;
                case 8: amount = 10000; break;
                case 9: amount = 15000; break;
            }
            return amount;
        }

        private void ShowExperienceBar()
        {
            if (_allowExperienceBarToFadeOut)
            {
                _timeToDisappear.Interval = _timeBeforeExperienceBarFades.TotalMilliseconds;
                _timeToDisappear.Start();
            }
            else
            {
                _timeToDisappear.Stop();
            }

            _experienceBarShouldBeVisible = true;
        }

    }
}
