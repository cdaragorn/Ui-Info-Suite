﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Network;
using System;
using System.Linq;

namespace UIInfoSuite.UIElements
{
    class ShowWhenAnimalNeedsPet : IDisposable
    {
        private readonly PerScreen<float> _yMovementPerDraw = new PerScreen<float>();
        private readonly PerScreen<float> _alpha = new PerScreen<float>();
        private readonly PerScreen<int> _pauseTicks = new PerScreen<int>(createNewState:()=>60);

        private readonly IModHelper _helper;

        public ShowWhenAnimalNeedsPet(IModHelper helper)
        {
            _helper = helper;
        }

        public void ToggleOption(bool showWhenAnimalNeedsPet)
        {
            _helper.Events.Player.Warped -= OnWarped;
            _helper.Events.Display.RenderingHud -= OnRenderingHud_DrawAnimalHasProduct;
            _helper.Events.Display.RenderingHud -= OnRenderingHud_DrawNeedsPetTooltip;
            _helper.Events.GameLoop.UpdateTicked -= UpdateTicked;

            if (showWhenAnimalNeedsPet)
            {
                _helper.Events.Player.Warped += OnWarped;
                _helper.Events.Display.RenderingHud += OnRenderingHud_DrawAnimalHasProduct;
                _helper.Events.Display.RenderingHud += OnRenderingHud_DrawNeedsPetTooltip;
                _helper.Events.GameLoop.UpdateTicked += UpdateTicked;
            }
        }

        public void Dispose()
        {
            ToggleOption(false);
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud_DrawAnimalHasProduct(object sender, RenderingHudEventArgs e)
        {
            if (!Game1.eventUp &&
                Game1.activeClickableMenu == null &&
                Game1.currentLocation != null)
            {
                var animalsInCurrentLocation = GetAnimalsInCurrentLocation();
                if (animalsInCurrentLocation != null)
                {
                    foreach (var animal in animalsInCurrentLocation.Pairs)
                    {
                        if (!animal.Value.IsEmoting &&
                            animal.Value.currentProduce.Value != 430 &&
                            animal.Value.currentProduce.Value > 0 &&
                            animal.Value.age.Value >= animal.Value.ageWhenMature.Value)
                        {
                            var positionAboveAnimal = GetPetPositionAboveAnimal(animal.Value);
                            positionAboveAnimal.Y += (float)(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 300.0 + animal.Value.Name.GetHashCode()) * 5.0);
                            Game1.spriteBatch.Draw(
                                Game1.emoteSpriteSheet,
                                Utility.ModifyCoordinatesForUIScale(new Vector2(positionAboveAnimal.X + 14f, positionAboveAnimal.Y)),
                                new Rectangle(3 * (Game1.tileSize / 4) % Game1.emoteSpriteSheet.Width, 3 * (Game1.tileSize / 4) / Game1.emoteSpriteSheet.Width * (Game1.tileSize / 4), Game1.tileSize / 4, Game1.tileSize / 4),
                                Color.White * 0.9f,
                                0.0f,
                                Vector2.Zero,
                                4f,
                                SpriteEffects.None,
                                1f);

                            var sourceRectangle = GameLocation.getSourceRectForObject(animal.Value.currentProduce.Value);
                            Game1.spriteBatch.Draw(
                                Game1.objectSpriteSheet,
                                Utility.ModifyCoordinatesForUIScale(new Vector2(positionAboveAnimal.X + 28f, positionAboveAnimal.Y + 8f)),
                                sourceRectangle,
                                Color.White * 0.9f,
                                0.0f,
                                Vector2.Zero,
                                2.2f,
                                SpriteEffects.None,
                                1f);
                        }
                    }
                }
            }
        }

        /// <summary>Raised after a player warps to a new location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnWarped(object sender, WarpedEventArgs e)
        {
            if (e.IsLocalPlayer)
            {
                _pauseTicks.Value = 60;
            }
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud_DrawNeedsPetTooltip(object sender, RenderingHudEventArgs e)
        {
            // If _pauseTicks is positive then don't render.
            if (!Game1.eventUp && Game1.activeClickableMenu == null && (Game1.currentLocation is AnimalHouse || Game1.currentLocation is Farm) && _pauseTicks.Value <= 0)
            {
                DrawIconForFarmAnimals();
                DrawIconForPets();
            }
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (Game1.eventUp || Game1.activeClickableMenu != null || !(Game1.currentLocation is AnimalHouse || Game1.currentLocation is Farm))
                return;

            --_pauseTicks.Value;

            // update pet draw. If _pauseTicks is positive then don't render.
            if (e.IsMultipleOf(2) && _pauseTicks.Value <= 0)
            {
                _yMovementPerDraw.Value += 0.3f;
                _alpha.Value -= 0.014f;
                if (_alpha.Value < 0.1f)
                {
                    _pauseTicks.Value = 60;
                    _yMovementPerDraw.Value = -3f;
                    _alpha.Value = 1f;
                }
            }
        }

        private void DrawIconForFarmAnimals()
        {
            var animalsInCurrentLocation = GetAnimalsInCurrentLocation();

            if (animalsInCurrentLocation != null)
            {
                foreach (var animal in animalsInCurrentLocation.Pairs)
                {
                    if (!animal.Value.IsEmoting &&
                        !animal.Value.wasPet.Value)
                    {
                        var positionAboveAnimal = GetPetPositionAboveAnimal(animal.Value);
                        var animalType = animal.Value.type.Value.ToLower();

                        if (animalType.Contains("cow") ||
                            animalType.Contains("sheep") ||
                            animalType.Contains("goat") ||
                            animalType.Contains("pig"))
                        {
                            positionAboveAnimal.X += 50f;
                            positionAboveAnimal.Y += 50f;
                        }
                        Game1.spriteBatch.Draw(
                            Game1.mouseCursors,
                            Utility.ModifyCoordinatesForUIScale(new Vector2(positionAboveAnimal.X, positionAboveAnimal.Y + _yMovementPerDraw.Value)),
                            new Rectangle(32, 0, 16, 16),
                            Color.White * _alpha.Value,
                            0.0f,
                            Vector2.Zero,
                            4f,
                            SpriteEffects.None,
                            1f);
                    }
                }
            }
        }

        private void DrawIconForPets()
        {
            foreach (var character in Game1.currentLocation.characters)
            {
                if (character is Pet pet &&
                    !pet.lastPetDay.Values.Any(day => day == Game1.Date.TotalDays))
                {
                    var positionAboveAnimal = GetPetPositionAboveAnimal(character);
                    positionAboveAnimal.X += 50f;
                    positionAboveAnimal.Y -= 20f;
                    Game1.spriteBatch.Draw(
                        Game1.mouseCursors,
                        Utility.ModifyCoordinatesForUIScale(new Vector2(positionAboveAnimal.X, positionAboveAnimal.Y + _yMovementPerDraw.Value)),
                        new Rectangle(32, 0, 16, 16),
                        Color.White * _alpha.Value,
                        0.0f,
                        Vector2.Zero,
                        4f,
                        SpriteEffects.None,
                        1f);
                }
            }
        }

        private Vector2 GetPetPositionAboveAnimal(Character animal)
        {
            return new Vector2(Game1.viewport.Width <= Game1.currentLocation.map.DisplayWidth ? animal.position.X - Game1.viewport.X + 16 : animal.position.X + ((Game1.viewport.Width - Game1.currentLocation.map.DisplayWidth) / 2 + 18),
                Game1.viewport.Height <= Game1.currentLocation.map.DisplayHeight ? animal.position.Y - Game1.viewport.Y - 34 : animal.position.Y + ((Game1.viewport.Height - Game1.currentLocation.map.DisplayHeight) / 2 - 50));
        }

        private NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>> GetAnimalsInCurrentLocation()
        {
            NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>> animals = null;

            if (Game1.currentLocation is AnimalHouse)
            {
                animals = (Game1.currentLocation as AnimalHouse).animals;
            }
            else if (Game1.currentLocation is Farm)
            {
                animals = (Game1.currentLocation as Farm).animals;
            }

            return animals;
        }
    }
}
