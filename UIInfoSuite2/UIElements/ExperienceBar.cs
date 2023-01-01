using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.UIElements
{
    public class ExperienceBar : IDisposable
    {
        private readonly PerScreen<int[]> _currentExperience = new(createNewState: () => new int[5]);
        private readonly PerScreen<int[]> _currentLevelExtenderExperience = new(createNewState: () => new int[5]);

        private readonly PerScreen<DisplayedExperienceBar> _displayedExperienceBar = new(createNewState: () => new DisplayedExperienceBar());
        private readonly PerScreen<List<DisplayedExperienceValue>> _displayedExperienceValues = new(createNewState: () => new List<DisplayedExperienceValue>());

        private const int LevelUpVisibleTicks = 120;
        private readonly PerScreen<int> _levelUpVisibleTimer = new();
        private const int ExperienceBarVisibleTicks = 480;
        private readonly PerScreen<int> _experienceBarVisibleTimer = new();

        private static readonly Dictionary<SkillType, Rectangle> SkillIconRectangles = new()
        {
            { SkillType.Farming , new(10, 428, 10, 10)},
            { SkillType.Fishing , new(20, 428, 10, 10)},
            { SkillType.Foraging , new(60, 428, 10, 10)},
            { SkillType.Mining , new(30, 428, 10, 10)},
            { SkillType.Combat , new(120, 428, 10, 10)}
        };
        private static readonly Dictionary<SkillType, Color> ExperienceFillColor = new()
        {
            { SkillType.Farming , new Color(255, 251, 35, 0.38f)},
            { SkillType.Fishing , new Color(17, 84, 252, 0.63f)},
            { SkillType.Foraging , new Color(0, 234, 0, 0.63f)},
            { SkillType.Mining , new Color(145, 104, 63, 0.63f)},
            { SkillType.Combat , new Color(204, 0, 3, 0.63f)}
        };

        private readonly PerScreen<Rectangle> _experienceIconRectangle = new(createNewState: () => SkillIconRectangles[SkillType.Farming]);
        private readonly PerScreen<Rectangle> _levelUpIconRectangle = new(createNewState: () => SkillIconRectangles[SkillType.Farming]);
        private readonly PerScreen<Color> _experienceFillColor = new(createNewState: () => ExperienceFillColor[SkillType.Farming]);
        private readonly PerScreen<Item> _previousItem = new();

        private SoundEffectInstance _soundEffect;
        private bool _allowExperienceBarToFadeOut = true;
        private bool _showExperienceGain = true;
        private bool _showLevelUpAnimation = true;
        private bool _showExperienceBar = true;
        private readonly IModHelper _helper;

        private readonly ILevelExtender _levelExtenderAPI;

        private readonly PerScreen<int> _currentSkillLevel = new(createNewState: () => 0);
        private readonly PerScreen<int> _experienceRequiredToLevel = new(createNewState: () => -1);
        private readonly PerScreen<int> _experienceFromPreviousLevels = new(createNewState: () => -1);
        private readonly PerScreen<int> _experienceEarnedThisLevel = new(createNewState: () => -1);

        public ExperienceBar(IModHelper helper)
        {
            _helper = helper;

            string path = string.Empty;
            try
            {
                path = Path.Combine(_helper.DirectoryPath, "assets", "LevelUp.wav");
                _soundEffect = SoundEffect.FromStream(new FileStream(path, FileMode.Open)).CreateInstance();
            }
            catch (Exception ex)
            {
                ModEntry.MonitorObject.Log("Error loading sound file from " + path + ": " + ex.Message + Environment.NewLine + ex.StackTrace, LogLevel.Error);
            }

            helper.Events.Display.RenderingHud += OnRenderingHud;
            helper.Events.Player.Warped += OnWarped_RemoveAllExperiencePointDisplays;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked_HandleTimers;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;

            if (_helper.ModRegistry.IsLoaded("DevinLematty.LevelExtender"))
            {
                _levelExtenderAPI = _helper.ModRegistry.GetApi<ILevelExtender>("DevinLematty.LevelExtender");
            }
        }

        public void Dispose()
        {
            _helper.Events.Player.LevelChanged -= OnLevelChanged;
            _helper.Events.Display.RenderingHud -= OnRenderingHud;
            _helper.Events.Player.Warped -= OnWarped_RemoveAllExperiencePointDisplays;
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked_DetermineIfExperienceHasBeenGained;
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked_HandleTimers;
            _helper.Events.GameLoop.SaveLoaded -= OnSaveLoaded;
            _soundEffect.Dispose();
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
                _currentExperience.Value[i] = Game1.player.experiencePoints[i];
            _showExperienceGain = showExperienceGain;

            if (_levelExtenderAPI != null)
            {
                for (var i = 0; i < _currentLevelExtenderExperience.Value.Length; ++i)
                    _currentLevelExtenderExperience.Value[i] = _levelExtenderAPI.CurrentXP()[i];
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

            _displayedExperienceValues.Value.Clear();
        }

        /// <summary>Raised after a player skill level changes. This happens as soon as they level up, not when the game notifies the player after their character goes to bed.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnLevelChanged(object sender, LevelChangedEventArgs e)
        {
            if (_showLevelUpAnimation && e.IsLocalPlayer)
            {
                _levelUpVisibleTimer.Value = LevelUpVisibleTicks;
                _experienceBarVisibleTimer.Value = ExperienceBarVisibleTicks;

                _levelUpIconRectangle.Value = SkillIconRectangles[e.Skill];

                PlayLevelUpSoundEffect();
            }
        }

        private void PlayLevelUpSoundEffect()
        {
            if (_soundEffect == null)
                return;

            _soundEffect.Volume = Game1.options.soundVolumeLevel;
            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(200);
                _soundEffect?.Play();
            });
        }

        /// <summary>Raised after a player warps to a new location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnWarped_RemoveAllExperiencePointDisplays(object sender, WarpedEventArgs e)
        {
            if (e.IsLocalPlayer)
                _displayedExperienceValues.Value.Clear();
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
                levelExtenderExperience = _levelExtenderAPI.CurrentXP();

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
                _experienceBarVisibleTimer.Value = ExperienceBarVisibleTicks;

                _experienceIconRectangle.Value = SkillIconRectangles[(SkillType)currentLevelIndex];
                _experienceFillColor.Value = ExperienceFillColor[(SkillType)currentLevelIndex];
                _currentSkillLevel.Value = Game1.player.GetSkillLevel(currentLevelIndex);

                _experienceRequiredToLevel.Value = GetExperienceRequiredToLevel(_currentSkillLevel.Value);
                _experienceFromPreviousLevels.Value = GetExperienceRequiredToLevel(_currentSkillLevel.Value - 1);
                _experienceEarnedThisLevel.Value = Game1.player.experiencePoints[currentLevelIndex] - _experienceFromPreviousLevels.Value;

                if (_experienceRequiredToLevel.Value <= 0 && _levelExtenderAPI != null)
                {
                    _experienceEarnedThisLevel.Value = _levelExtenderAPI.CurrentXP()[currentLevelIndex];
                    _experienceFromPreviousLevels.Value = _currentExperience.Value[currentLevelIndex] - _experienceEarnedThisLevel.Value;
                    _experienceRequiredToLevel.Value = _levelExtenderAPI.RequiredXP()[currentLevelIndex] + _experienceFromPreviousLevels.Value;
                }

                if (_showExperienceGain && _experienceRequiredToLevel.Value > 0)
                {
                    int currentExperienceToUse = Game1.player.experiencePoints[currentLevelIndex];
                    var previousExperienceToUse = _currentExperience.Value[currentLevelIndex];
                    if (_levelExtenderAPI != null && _currentSkillLevel.Value > 9)
                    {
                        currentExperienceToUse = _levelExtenderAPI.CurrentXP()[currentLevelIndex];
                        previousExperienceToUse = _currentLevelExtenderExperience.Value[currentLevelIndex];
                    }

                    int experienceGain = currentExperienceToUse - previousExperienceToUse;

                    if (experienceGain > 0)
                    {
                        _displayedExperienceValues.Value.Add(
                            new DisplayedExperienceValue(
                                experienceGain,
                                Game1.player.getLocalPosition(Game1.viewport)));
                    }
                }

                _currentExperience.Value[currentLevelIndex] = Game1.player.experiencePoints[currentLevelIndex];

                if (_levelExtenderAPI != null)
                    _currentLevelExtenderExperience.Value[currentLevelIndex] = _levelExtenderAPI.CurrentXP()[currentLevelIndex];

            }
            else if (_previousItem.Value != currentItem)
            {
                _experienceBarVisibleTimer.Value = ExperienceBarVisibleTicks;

                if (currentItem is FishingRod)
                {
                    currentLevelIndex = (int)SkillType.Fishing;
                    _experienceIconRectangle.Value = SkillIconRectangles[SkillType.Fishing];
                    _experienceFillColor.Value = ExperienceFillColor[SkillType.Fishing];
                    _currentSkillLevel.Value = Game1.player.fishingLevel.Value;
                }
                else if (currentItem is Pickaxe)
                {
                    currentLevelIndex = (int)SkillType.Mining;
                    _experienceIconRectangle.Value = SkillIconRectangles[SkillType.Mining];
                    _experienceFillColor.Value = ExperienceFillColor[SkillType.Mining];
                    _currentSkillLevel.Value = Game1.player.miningLevel.Value;
                }
                else if (currentItem is MeleeWeapon && currentItem.Name != "Scythe")
                {
                    currentLevelIndex = (int)SkillType.Combat;
                    _experienceIconRectangle.Value = SkillIconRectangles[SkillType.Combat];
                    _experienceFillColor.Value = ExperienceFillColor[SkillType.Combat];
                    _currentSkillLevel.Value = Game1.player.combatLevel.Value;
                }
                else if (Game1.currentLocation is Farm && !(currentItem is Axe))
                {
                    currentLevelIndex = (int)SkillType.Farming;
                    _experienceIconRectangle.Value = SkillIconRectangles[SkillType.Farming];
                    _experienceFillColor.Value = ExperienceFillColor[SkillType.Farming];
                    _currentSkillLevel.Value = Game1.player.farmingLevel.Value;
                }
                else
                {
                    currentLevelIndex = (int)SkillType.Foraging;
                    _experienceIconRectangle.Value = SkillIconRectangles[SkillType.Foraging];
                    _experienceFillColor.Value = ExperienceFillColor[SkillType.Foraging];
                    _currentSkillLevel.Value = Game1.player.foragingLevel.Value;
                }

                _experienceRequiredToLevel.Value = GetExperienceRequiredToLevel(_currentSkillLevel.Value);
                _experienceFromPreviousLevels.Value = GetExperienceRequiredToLevel(_currentSkillLevel.Value - 1);
                _experienceEarnedThisLevel.Value = Game1.player.experiencePoints[currentLevelIndex] - _experienceFromPreviousLevels.Value;

                if (_experienceRequiredToLevel.Value <= 0 && _levelExtenderAPI != null)
                {
                    _experienceEarnedThisLevel.Value = _levelExtenderAPI.CurrentXP()[currentLevelIndex];
                    _experienceFromPreviousLevels.Value = _currentExperience.Value[currentLevelIndex] - _experienceEarnedThisLevel.Value;
                    _experienceRequiredToLevel.Value = _levelExtenderAPI.RequiredXP()[currentLevelIndex] + _experienceFromPreviousLevels.Value;
                }

                _previousItem.Value = currentItem;
            }

        }

        public void OnUpdateTicked_HandleTimers(object sender, UpdateTickedEventArgs e)
        {
            if (_levelUpVisibleTimer.Value > 0)
            {
                _levelUpVisibleTimer.Value--;
            }

            if (_experienceBarVisibleTimer.Value > 0)
            {
                _experienceBarVisibleTimer.Value--;
            }
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            if (!Game1.eventUp)
            {
                if (_levelUpVisibleTimer.Value != 0)
                {
                    Vector2 playerLocalPosition = Game1.player.getLocalPosition(Game1.viewport);

                    Game1.spriteBatch.Draw(
                        Game1.mouseCursors,
                        Utility.ModifyCoordinatesForUIScale(new Vector2(
                            playerLocalPosition.X - 74,
                            playerLocalPosition.Y - 130)),
                        _levelUpIconRectangle.Value,
                        Color.White,
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

                DisplayExperienceValues();

                if (_experienceRequiredToLevel.Value > 0 && (_experienceBarVisibleTimer.Value != 0 || !_allowExperienceBarToFadeOut) && _showExperienceBar)
                {
                    _displayedExperienceBar.Value.Draw(_experienceFillColor.Value, _experienceIconRectangle.Value,
                        _experienceEarnedThisLevel.Value, _experienceRequiredToLevel.Value - _experienceFromPreviousLevels.Value, _currentSkillLevel.Value);
                }

            }
        }

        private void DisplayExperienceValues()
        {
            for (var i = _displayedExperienceValues.Value.Count - 1; i >= 0; --i)
            {
                if (_displayedExperienceValues.Value[i].IsInvisible)
                {
                    _displayedExperienceValues.Value.RemoveAt(i);
                }
                else
                {
                    _displayedExperienceValues.Value[i].Draw();
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
    }
}
