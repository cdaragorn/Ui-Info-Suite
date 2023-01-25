﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
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

        private readonly PerScreen<int[]> _currentExperience = new PerScreen<int[]>(createNewState: () => new int[5]);
        private readonly PerScreen<int[]> _currentLevelExtenderExperience = new PerScreen<int[]>(createNewState: () => new int[5]);
        private readonly PerScreen<List<ExperiencePointDisplay>> _experiencePointDisplays = new PerScreen<List<ExperiencePointDisplay>>(createNewState: () => new List<ExperiencePointDisplay>());
        
        private readonly TimeSpan _levelUpPauseTime = TimeSpan.FromSeconds(2);
        private readonly PerScreen<int> _hideLevelUpTicks = new PerScreen<int>();

        private static Rectangle _farmingIconRectangle = new Rectangle(10, 428, 10, 10);
        private static Rectangle _fishingIconRectangle = new Rectangle(20, 428, 10, 10);
        private static Rectangle _foragingIconRectangle = new Rectangle(60, 428, 10, 10);
        private static Rectangle _miningIconRectangle = new Rectangle(30, 428, 10, 10);
        private static Rectangle _combatIconRectangle = new Rectangle(120, 428, 10, 10);

        private readonly Color _iconColor = Color.White;
        private readonly PerScreen<Color> _experienceFillColor = new PerScreen<Color>(createNewState: () => Color.Blue);
        private readonly PerScreen<Rectangle> _experienceIconPosition = new PerScreen<Rectangle>(createNewState: () => _farmingIconRectangle);
        private readonly PerScreen<Rectangle> _levelUpIconRectangle = new PerScreen<Rectangle>(createNewState: () => _combatIconRectangle);
        private readonly PerScreen<Item> _previousItem = new PerScreen<Item>();
        private readonly PerScreen<bool> _experienceBarShouldBeVisible = new PerScreen<bool>();
        private readonly PerScreen<bool> _shouldDrawLevelUp = new PerScreen<bool>(createNewState: () => false);

        private readonly TimeSpan _timeBeforeExperienceBarFades = TimeSpan.FromSeconds(8);
        private readonly PerScreen<int> _hideExperienceBarTicks = new PerScreen<int>();

        //private SoundEffectInstance _soundEffect;
        private bool _allowExperienceBarToFadeOut = true;
        private bool _showExperienceGain = true;
        private bool _showLevelUpAnimation = true;
        private bool _showExperienceBar = true;
        private readonly IModHelper _helper;
        private SoundPlayer _player;

        private ILevelExtenderInterface _levelExtenderAPI;

        private readonly PerScreen<int> _currentSkillLevel = new PerScreen<int>(createNewState: () => 0);
        private readonly PerScreen<int> _experienceRequiredToLevel = new PerScreen<int>(createNewState: () => -1);
        private readonly PerScreen<int> _experienceFromPreviousLevels = new PerScreen<int>(createNewState: () => -1);
        private readonly PerScreen<int> _experienceEarnedThisLevel = new PerScreen<int>(createNewState: () => -1);

        public ExperienceBar(IModHelper helper)
        {
            _helper = helper;
            var path = string.Empty;
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
            helper.Events.Display.RenderingHud += OnRenderingHud;
            helper.Events.Player.Warped += OnWarped_RemoveAllExperiencePointDisplays;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked_HandleTimers;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;

            var something = _helper.ModRegistry.GetApi("DevinLematty.LevelExtender");
            try
            {
                _levelExtenderAPI = _helper.ModRegistry.GetApi<ILevelExtenderInterface>("DevinLematty.LevelExtender");
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
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked_HandleTimers;
            _helper.Events.GameLoop.SaveLoaded -= OnSaveLoaded;
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
            for (var i = 0; i < _currentExperience.Value.Length; ++i)
            {
                _currentExperience.Value[i] = Game1.player.experiencePoints[i];
            }
            _showExperienceGain = showExperienceGain;

            if (_levelExtenderAPI != null)
            {
                for (var i = 0; i < _currentLevelExtenderExperience.Value.Length; ++i)
                    _currentLevelExtenderExperience.Value[i] = _levelExtenderAPI.CurrentXp()[i];
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

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            for (var i = 0; i < _currentExperience.Value.Length; ++i)
            {
                _currentExperience.Value[i] = Game1.player.experiencePoints[i];
            }

            _experiencePointDisplays.Value.Clear();
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
                    case SkillType.Combat: _levelUpIconRectangle.Value = _combatIconRectangle; break;
                    case SkillType.Farming: _levelUpIconRectangle.Value = _farmingIconRectangle; break;
                    case SkillType.Fishing: _levelUpIconRectangle.Value = _fishingIconRectangle; break;
                    case SkillType.Foraging: _levelUpIconRectangle.Value = _foragingIconRectangle; break;
                    case SkillType.Mining: _levelUpIconRectangle.Value = _miningIconRectangle; break;
                }
                _shouldDrawLevelUp.Value = true;
                ShowExperienceBar();

                var previousAmbientVolume = Game1.options.ambientVolumeLevel;
                var previousMusicVolume = Game1.options.musicVolumeLevel;

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

                _hideLevelUpTicks.Value = (int)(_levelUpPauseTime.TotalSeconds * 60f);
                //Task.Factory.StartNew(() =>
                //{
                //    Thread.Sleep(_levelUpPauseTime);
                //    _shouldDrawLevelUp.Value = false;
                //    //Game1.musicCategory.SetVolume(previousMusicVolume);
                //    //Game1.ambientCategory.SetVolume(previousAmbientVolume);
                //});
            }
        }

        private void FadeExperienceBarOut()
        {
            if (_allowExperienceBarToFadeOut)
            {
                _experienceBarShouldBeVisible.Value = false;
            }
        }

        /// <summary>Raised after a player warps to a new location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnWarped_RemoveAllExperiencePointDisplays(object sender, WarpedEventArgs e)
        {
            if (e.IsLocalPlayer)
                _experiencePointDisplays.Value.Clear();
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked_DetermineIfExperienceHasBeenGained(object sender, UpdateTickedEventArgs e)
        {
            if (!e.IsMultipleOf(15)) // quarter second
                return;

            var currentItem = Game1.player.CurrentItem;

            var currentLevelIndex = -1;

            int[] levelExtenderExperience = null;
            if (_levelExtenderAPI != null)
                levelExtenderExperience = _levelExtenderAPI.CurrentXp();

            for (var i = 0; i < _currentExperience.Value.Length; ++i)
            {
                if (_currentExperience.Value[i] != Game1.player.experiencePoints[i] ||
                    (_levelExtenderAPI != null &&
                    _currentLevelExtenderExperience.Value[i] != levelExtenderExperience[i]))
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
                            _experienceFillColor.Value = new Color(255, 251, 35, 0.38f);
                            _experienceIconPosition.Value = _farmingIconRectangle;
                            _currentSkillLevel.Value = Game1.player.farmingLevel.Value;
                            break;
                        }

                    case 1:
                        {
                            _experienceFillColor.Value = new Color(17, 84, 252, 0.63f);
                            _experienceIconPosition.Value = _fishingIconRectangle;
                            _currentSkillLevel.Value = Game1.player.fishingLevel.Value;
                            break;
                        }

                    case 2:
                        {
                            _experienceFillColor.Value = new Color(0, 234, 0, 0.63f);
                            _experienceIconPosition.Value = _foragingIconRectangle;
                            _currentSkillLevel.Value = Game1.player.foragingLevel.Value;
                            break;
                        }

                    case 3:
                        {
                            _experienceFillColor.Value = new Color(145, 104, 63, 0.63f);
                            _experienceIconPosition.Value = _miningIconRectangle;
                            _currentSkillLevel.Value = Game1.player.miningLevel.Value;
                            break;
                        }

                    case 4:
                        {
                            _experienceFillColor.Value = new Color(204, 0, 3, 0.63f);
                            _experienceIconPosition.Value = _combatIconRectangle;
                            _currentSkillLevel.Value = Game1.player.combatLevel.Value;
                            break;
                        }
                }

                _experienceRequiredToLevel.Value = GetExperienceRequiredToLevel(_currentSkillLevel.Value);
                _experienceFromPreviousLevels.Value = GetExperienceRequiredToLevel(_currentSkillLevel.Value - 1);
                _experienceEarnedThisLevel.Value = Game1.player.experiencePoints[currentLevelIndex] - _experienceFromPreviousLevels.Value;
                var experiencePreviouslyEarnedThisLevel = _currentExperience.Value[currentLevelIndex] - _experienceFromPreviousLevels.Value;

                if (_experienceRequiredToLevel.Value <= 0 &&
                    _levelExtenderAPI != null)
                {
                    _experienceEarnedThisLevel.Value = _levelExtenderAPI.CurrentXp()[currentLevelIndex];
                    _experienceFromPreviousLevels.Value = _currentExperience.Value[currentLevelIndex] - _experienceEarnedThisLevel.Value;
                    _experienceRequiredToLevel.Value = _levelExtenderAPI.RequiredXp()[currentLevelIndex] + _experienceFromPreviousLevels.Value;
                }

                ShowExperienceBar();
                if (_showExperienceGain &&
                    _experienceRequiredToLevel.Value > 0)
                {
                    var currentExperienceToUse = Game1.player.experiencePoints[currentLevelIndex];
                    var previousExperienceToUse = _currentExperience.Value[currentLevelIndex];
                    if (_levelExtenderAPI != null &&
                        _currentSkillLevel.Value > 9)
                    {
                        currentExperienceToUse = _levelExtenderAPI.CurrentXp()[currentLevelIndex];
                        previousExperienceToUse = _currentLevelExtenderExperience.Value[currentLevelIndex];
                    }

                    var experienceGain = currentExperienceToUse - previousExperienceToUse;

                    if (experienceGain > 0)
                    {
                        _experiencePointDisplays.Value.Add(
                            new ExperiencePointDisplay(
                                experienceGain,
                                Game1.player.getLocalPosition(Game1.viewport)));
                    }
                }

                _currentExperience.Value[currentLevelIndex] = Game1.player.experiencePoints[currentLevelIndex];

                if (_levelExtenderAPI != null)
                    _currentLevelExtenderExperience.Value[currentLevelIndex] = _levelExtenderAPI.CurrentXp()[currentLevelIndex];

            }
            else if (_previousItem.Value != currentItem)
            {
                if (currentItem is FishingRod)
                {
                    _experienceFillColor.Value = new Color(17, 84, 252, 0.63f);
                    currentLevelIndex = 1;
                    _experienceIconPosition.Value = _fishingIconRectangle;
                    _currentSkillLevel.Value = Game1.player.fishingLevel.Value;
                }
                else if (currentItem is Pickaxe)
                {
                    _experienceFillColor.Value = new Color(145, 104, 63, 0.63f);
                    currentLevelIndex = 3;
                    _experienceIconPosition.Value = _miningIconRectangle;
                    _currentSkillLevel.Value = Game1.player.miningLevel.Value;
                }
                else if (currentItem is MeleeWeapon &&
                    currentItem.Name != "Scythe")
                {
                    _experienceFillColor.Value = new Color(204, 0, 3, 0.63f);
                    currentLevelIndex = 4;
                    _experienceIconPosition.Value = _combatIconRectangle;
                    _currentSkillLevel.Value = Game1.player.combatLevel.Value;
                }
                else if (Game1.currentLocation is Farm &&
                    !(currentItem is Axe))
                {
                    _experienceFillColor.Value = new Color(255, 251, 35, 0.38f);
                    currentLevelIndex = 0;
                    _experienceIconPosition.Value = _farmingIconRectangle;
                    _currentSkillLevel.Value = Game1.player.farmingLevel.Value;
                }
                else
                {
                    _experienceFillColor.Value = new Color(0, 234, 0, 0.63f);
                    currentLevelIndex = 2;
                    _experienceIconPosition.Value = _foragingIconRectangle;
                    _currentSkillLevel.Value = Game1.player.foragingLevel.Value;
                }

                _experienceRequiredToLevel.Value = GetExperienceRequiredToLevel(_currentSkillLevel.Value);
                _experienceFromPreviousLevels.Value = GetExperienceRequiredToLevel(_currentSkillLevel.Value - 1);
                _experienceEarnedThisLevel.Value = Game1.player.experiencePoints[currentLevelIndex] - _experienceFromPreviousLevels.Value;

                if (_experienceRequiredToLevel.Value <= 0 &&
                    _levelExtenderAPI != null)
                {
                    _experienceEarnedThisLevel.Value = _levelExtenderAPI.CurrentXp()[currentLevelIndex];
                    _experienceFromPreviousLevels.Value = _currentExperience.Value[currentLevelIndex] - _experienceEarnedThisLevel.Value;
                    _experienceRequiredToLevel.Value = _levelExtenderAPI.RequiredXp()[currentLevelIndex] + _experienceFromPreviousLevels.Value;
                }

                ShowExperienceBar();
                _previousItem.Value = currentItem;
            }

        }

        public void OnUpdateTicked_HandleTimers(object sender, UpdateTickedEventArgs e)
        {
            if (_hideLevelUpTicks.Value > 0)
            {
                _hideLevelUpTicks.Value--;

                if (_hideLevelUpTicks.Value == 0)
                {
                    _shouldDrawLevelUp.Value = false;
                }
            }

            if (_hideExperienceBarTicks.Value > 0)
            {
                _hideExperienceBarTicks.Value--;

                if (_hideExperienceBarTicks.Value == 0)
                {
                    FadeExperienceBarOut();
                }
            }
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            if (!Game1.eventUp)
            {
                if (_shouldDrawLevelUp.Value)
                {
                    var playerLocalPosition = Game1.player.getLocalPosition(Game1.viewport);
                    Game1.spriteBatch.Draw(
                        Game1.mouseCursors,
                        Utility.ModifyCoordinatesForUIScale(new Vector2(
                            playerLocalPosition.X - 74,
                            playerLocalPosition.Y - 130)),
                        _levelUpIconRectangle.Value,
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
                        Utility.ModifyCoordinatesForUIScale(new Vector2(
                            playerLocalPosition.X - 28,
                            playerLocalPosition.Y - 130)));
                }

                for (var i = _experiencePointDisplays.Value.Count - 1; i >= 0; --i)
                {
                    if (_experiencePointDisplays.Value[i].IsInvisible)
                    {
                        _experiencePointDisplays.Value.RemoveAt(i);
                    }
                    else
                    {
                        _experiencePointDisplays.Value[i].Draw();
                    }
                }

                if (_experienceRequiredToLevel.Value > 0 &&
                    _experienceBarShouldBeVisible.Value &&
                    _showExperienceBar)
                {
                    var experienceDifferenceBetweenLevels = _experienceRequiredToLevel.Value - _experienceFromPreviousLevels.Value;
                    var barWidth = (int)((double)_experienceEarnedThisLevel.Value / experienceDifferenceBetweenLevels * MaxBarWidth);

                    DrawExperienceBar(barWidth, _experienceEarnedThisLevel.Value, experienceDifferenceBetweenLevels, _currentSkillLevel.Value);

                }

            }
        }

        private int GetExperienceRequiredToLevel(int currentLevel)
        {
            var amount = 0;

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
            _hideExperienceBarTicks.Value = (int)(_timeBeforeExperienceBarFades.TotalMilliseconds / 1000f * 60f);

            _experienceBarShouldBeVisible.Value = true;
        }

        private void DrawExperienceBar(int barWidth, int experienceGainedThisLevel, int experienceRequiredForNextLevel, int currentLevel)
        {
            float leftSide = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Left;

            if (Game1.isOutdoorMapSmallerThanViewport())
            {
                var num3 = Game1.currentLocation.map.Layers[0].LayerWidth * Game1.tileSize;
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
                _experienceFillColor.Value);

            Game1.spriteBatch.Draw(
                Game1.staminaRect,
                new Rectangle(
                    (int)leftSide + 32,
                    Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 63,
                    Math.Min(4, barWidth),
                    31),
                _experienceFillColor.Value);

            Game1.spriteBatch.Draw(
                Game1.staminaRect,
                new Rectangle(
                    (int)leftSide + 32,
                    Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 63,
                    barWidth,
                    4),
                _experienceFillColor.Value);

            Game1.spriteBatch.Draw(
                Game1.staminaRect,
                new Rectangle(
                    (int)leftSide + 32,
                    Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 36,
                    barWidth,
                    4),
                _experienceFillColor.Value);

            var textureComponent =
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
                    _experienceIconPosition.Value,
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
