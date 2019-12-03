using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using UIInfoSuite.Extensions;

namespace UIInfoSuite.UIElements
{
    class ExperienceBar : IDisposable
    {

        public interface LevelExtenderEvents
        {
            event EventHandler OnXPChanged;
        }

        private const int MaxBarWidth = 175;

        private int[] _currentExperience = new int[5];
        private int[] _currentLevelExtenderExperience = new int[5];
        private readonly List<ExperiencePointDisplay> _experiencePointDisplays = new List<ExperiencePointDisplay>();
        private readonly TimeSpan _levelUpPauseTime = TimeSpan.FromSeconds(2);
        private readonly Color _iconColor = Color.White;
        private Color _experienceFillColor = Color.Blue;
        private Rectangle _experienceIconPosition = new Rectangle(10, 428, 10, 10);
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

        private LevelExtenderInterface _levelExtenderAPI;

        private int _currentSkillLevel = 0;
        private int _experienceRequiredToLevel = -1;
        private int _experienceFromPreviousLevels = -1;
        private int _experienceEarnedThisLevel = -1;

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
            helper.Events.Display.RenderingHud += OnRenderingHud;
            helper.Events.Player.Warped += OnWarped_RemoveAllExperiencePointDisplays;

            var something = _helper.ModRegistry.GetApi("DevinLematty.LevelExtender");
            try
            {
                _levelExtenderAPI = _helper.ModRegistry.GetApi<LevelExtenderInterface>("DevinLematty.LevelExtender");
            }
            catch
            {

            }


            //if (something != null)
            //{
            //    try
            //    {
            //        var methods = something.GetType().GetMethods();
            //        var currentXPMethod = something.GetType().GetMethod("currentXP");

            //        foreach (var method in methods)
            //        {

            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        int f = 3;
            //    }
            //}
        }

        private void LoadModApis(object sender, EventArgs e)
        {

        }

        public void Dispose()
        {
            _helper.Events.Player.LevelChanged -= OnLevelChanged;
            _helper.Events.Display.RenderingHud -= OnRenderingHud;
            _helper.Events.Player.Warped -= OnWarped_RemoveAllExperiencePointDisplays;
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked_DetermineIfExperienceHasBeenGained;
            _timeToDisappear.Elapsed -= StopTimerAndFadeBarOut;
            _timeToDisappear.Stop();
            _timeToDisappear.Dispose();
            _timeToDisappear = null;
        }

        public void ToggleLevelUpAnimation(bool showLevelUpAnimation)
        {
            _showLevelUpAnimation = showLevelUpAnimation;
            _helper.Events.Player.LevelChanged -= OnLevelChanged;

            if (_showLevelUpAnimation)
            {
                _helper.Events.Player.LevelChanged += OnLevelChanged;
            }
        }

        public void ToggleExperienceBarFade(bool allowExperienceBarToFadeOut)
        {
            _allowExperienceBarToFadeOut = allowExperienceBarToFadeOut;
        }

        public void ToggleShowExperienceGain(bool showExperienceGain)
        {
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked_DetermineIfExperienceHasBeenGained;
            for (int i = 0; i < _currentExperience.Length; ++i)
                _currentExperience[i] = Game1.player.experiencePoints[i];
            _showExperienceGain = showExperienceGain;

            if (_levelExtenderAPI != null)
            {
                for (int i = 0; i < _currentLevelExtenderExperience.Length; ++i)
                    _currentLevelExtenderExperience[i] = _levelExtenderAPI.currentXP()[i];
            }

            if (showExperienceGain)
            {
                _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked_DetermineIfExperienceHasBeenGained;
            }
        }


        public void ToggleShowExperienceBar(bool showExperienceBar)
        {
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked_DetermineIfExperienceHasBeenGained;
            //GraphicsEvents.OnPreRenderHudEvent -= OnPreRenderHudEvent;
            //PlayerEvents.Warped -= RemoveAllExperiencePointDisplays;
            _showExperienceBar = showExperienceBar;
            if (showExperienceBar)
            {
                //GraphicsEvents.OnPreRenderHudEvent += OnPreRenderHudEvent;
                //PlayerEvents.Warped += RemoveAllExperiencePointDisplays;
                _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked_DetermineIfExperienceHasBeenGained;
            }
        }

        /// <summary>Raised after a player skill level changes. This happens as soon as they level up, not when the game notifies the player after their character goes to bed.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnLevelChanged(object sender, LevelChangedEventArgs e)
        {
            if (_showLevelUpAnimation && e.IsLocalPlayer)
            {
                switch (e.Skill)
                {
                    case SkillType.Combat: _levelUpIconRectangle.X = 120; break;
                    case SkillType.Farming: _levelUpIconRectangle.X = 10; break;
                    case SkillType.Fishing: _levelUpIconRectangle.X = 20; break;
                    case SkillType.Foraging: _levelUpIconRectangle.X = 60; break;
                    case SkillType.Mining: _levelUpIconRectangle.X = 30; break;
                }
                _shouldDrawLevelUp = true;
                ShowExperienceBar();

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
            _timeToDisappear?.Stop();
            _experienceBarShouldBeVisible = false;
        }

        /// <summary>Raised after a player warps to a new location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnWarped_RemoveAllExperiencePointDisplays(object sender, WarpedEventArgs e)
        {
            if (e.IsLocalPlayer)
                _experiencePointDisplays.Clear();
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked_DetermineIfExperienceHasBeenGained(object sender, UpdateTickedEventArgs e)
        {
            if (!e.IsMultipleOf(15)) // quarter second
                return;

            Item currentItem = Game1.player.CurrentItem;

            int currentLevelIndex = -1;

            int[] levelExtenderExperience = null;
            if (_levelExtenderAPI != null)
                levelExtenderExperience = _levelExtenderAPI.currentXP();

            for (int i = 0; i < _currentExperience.Length; ++i)
            {
                if (_currentExperience[i] != Game1.player.experiencePoints[i] ||
                    (_levelExtenderAPI != null &&
                    _currentLevelExtenderExperience[i] != levelExtenderExperience[i]))
                {
                    currentLevelIndex = i;
                    break;
                }
            }

            if (currentLevelIndex > -1)
            {
                switch (currentLevelIndex)
                {
                    case 0:
                        {
                            _experienceFillColor = new Color(255, 251, 35, 0.38f);
                            _experienceIconPosition.X = 10;
                            _currentSkillLevel = Game1.player.farmingLevel.Value;
                            break;
                        }

                    case 1:
                        {
                            _experienceFillColor = new Color(17, 84, 252, 0.63f);
                            _experienceIconPosition.X = 20;
                            _currentSkillLevel = Game1.player.fishingLevel.Value;
                            break;
                        }

                    case 2:
                        {
                            _experienceFillColor = new Color(0, 234, 0, 0.63f);
                            _experienceIconPosition.X = 60;
                            _currentSkillLevel = Game1.player.foragingLevel.Value;
                            break;
                        }

                    case 3:
                        {
                            _experienceFillColor = new Color(145, 104, 63, 0.63f);
                            _experienceIconPosition.X = 30;
                            _currentSkillLevel = Game1.player.miningLevel.Value;
                            break;
                        }

                    case 4:
                        {
                            _experienceFillColor = new Color(204, 0, 3, 0.63f);
                            _experienceIconPosition.X = 120;
                            _currentSkillLevel = Game1.player.combatLevel.Value;
                            break;
                        }
                }

                _experienceRequiredToLevel = GetExperienceRequiredToLevel(_currentSkillLevel);
                _experienceFromPreviousLevels = GetExperienceRequiredToLevel(_currentSkillLevel - 1);
                _experienceEarnedThisLevel = Game1.player.experiencePoints[currentLevelIndex] - _experienceFromPreviousLevels;
                int experiencePreviouslyEarnedThisLevel = _currentExperience[currentLevelIndex] - _experienceFromPreviousLevels;

                if (_experienceRequiredToLevel <= 0 &&
                    _levelExtenderAPI != null)
                {
                    _experienceEarnedThisLevel = _levelExtenderAPI.currentXP()[currentLevelIndex];
                    _experienceFromPreviousLevels = _currentExperience[currentLevelIndex] - _experienceEarnedThisLevel;
                    _experienceRequiredToLevel = _levelExtenderAPI.requiredXP()[currentLevelIndex] + _experienceFromPreviousLevels;
                }

                ShowExperienceBar();
                if (_showExperienceGain &&
                    _experienceRequiredToLevel > 0)
                {
                    int currentExperienceToUse = Game1.player.experiencePoints[currentLevelIndex];
                    int previousExperienceToUse = _currentExperience[currentLevelIndex];
                    if (_levelExtenderAPI != null &&
                        _currentSkillLevel > 9)
                    {
                        currentExperienceToUse = _levelExtenderAPI.currentXP()[currentLevelIndex];
                        previousExperienceToUse = _currentLevelExtenderExperience[currentLevelIndex];
                    }

                    int experienceGain = currentExperienceToUse - previousExperienceToUse;

                    if (experienceGain > 0)
                    {
                        _experiencePointDisplays.Add(
                            new ExperiencePointDisplay(
                                experienceGain,
                                Game1.player.getLocalPosition(Game1.viewport)));
                    }
                }

                _currentExperience[currentLevelIndex] = Game1.player.experiencePoints[currentLevelIndex];

                if (_levelExtenderAPI != null)
                    _currentLevelExtenderExperience[currentLevelIndex] = _levelExtenderAPI.currentXP()[currentLevelIndex];

            }
            else if (_previousItem != currentItem)
            {
                if (currentItem is FishingRod)
                {
                    _experienceFillColor = new Color(17, 84, 252, 0.63f);
                    currentLevelIndex = 1;
                    _experienceIconPosition.X = 20;
                    _currentSkillLevel = Game1.player.fishingLevel.Value;
                }
                else if (currentItem is Pickaxe)
                {
                    _experienceFillColor = new Color(145, 104, 63, 0.63f);
                    currentLevelIndex = 3;
                    _experienceIconPosition.X = 30;
                    _currentSkillLevel = Game1.player.miningLevel.Value;
                }
                else if (currentItem is MeleeWeapon &&
                    currentItem.Name != "Scythe")
                {
                    _experienceFillColor = new Color(204, 0, 3, 0.63f);
                    currentLevelIndex = 4;
                    _experienceIconPosition.X = 120;
                    _currentSkillLevel = Game1.player.combatLevel.Value;
                }
                else if (Game1.currentLocation is Farm &&
                    !(currentItem is Axe))
                {
                    _experienceFillColor = new Color(255, 251, 35, 0.38f);
                    currentLevelIndex = 0;
                    _experienceIconPosition.X = 10;
                    _currentSkillLevel = Game1.player.farmingLevel.Value;
                }
                else
                {
                    _experienceFillColor = new Color(0, 234, 0, 0.63f);
                    currentLevelIndex = 2;
                    _experienceIconPosition.X = 60;
                    _currentSkillLevel = Game1.player.foragingLevel.Value;
                }

                _experienceRequiredToLevel = GetExperienceRequiredToLevel(_currentSkillLevel);
                _experienceFromPreviousLevels = GetExperienceRequiredToLevel(_currentSkillLevel - 1);
                _experienceEarnedThisLevel = Game1.player.experiencePoints[currentLevelIndex] - _experienceFromPreviousLevels;

                if (_experienceRequiredToLevel <= 0 &&
                    _levelExtenderAPI != null)
                {
                    _experienceEarnedThisLevel = _levelExtenderAPI.currentXP()[currentLevelIndex];
                    _experienceFromPreviousLevels = _currentExperience[currentLevelIndex] - _experienceEarnedThisLevel;
                    _experienceRequiredToLevel = _levelExtenderAPI.requiredXP()[currentLevelIndex] + _experienceFromPreviousLevels;
                }

                ShowExperienceBar();
                _previousItem = currentItem;
            }

        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            if (!Game1.eventUp)
            {
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

                if (_experienceRequiredToLevel > 0 &&
                    _experienceBarShouldBeVisible &&
                    _showExperienceBar)
                {
                    int experienceDifferenceBetweenLevels = _experienceRequiredToLevel - _experienceFromPreviousLevels;
                    int barWidth = (int)((double)_experienceEarnedThisLevel / experienceDifferenceBetweenLevels * MaxBarWidth);

                    DrawExperienceBar(barWidth, _experienceEarnedThisLevel, experienceDifferenceBetweenLevels, _currentSkillLevel);

                }

            }
        }

        private int GetExperienceRequiredToLevel(int currentLevel)
        {
            int amount = 0;

            //if (currentLevel < 10)
            //{
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
            //}
            //else if (_levelExtenderAPI != null &&
            //    currentLevel < 100)
            //{
            //    var requiredXP = _levelExtenderAPI.requiredXP();
            //    amount = requiredXP[currentLevel];
            //}
            return amount;
        }

        private void ShowExperienceBar()
        {
            if (_timeToDisappear != null)
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
            }

            _experienceBarShouldBeVisible = true;
        }

        private void DrawExperienceBar(int barWidth, int experienceGainedThisLevel, int experienceRequiredForNextLevel, int currentLevel)
        {
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
                    experienceGainedThisLevel + "/" + experienceRequiredForNextLevel,
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
                    _experienceIconPosition,
                    _iconColor,
                    0,
                    Vector2.Zero,
                    2.9f,
                    SpriteEffects.None,
                    0.85f);

                Game1.drawWithBorder(
                    currentLevel.ToString(),
                    Color.Black * 0.6f,
                    Color.Black,
                    new Vector2(
                        leftSide + 33,
                        Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 70));
            }
        }

    }
}
