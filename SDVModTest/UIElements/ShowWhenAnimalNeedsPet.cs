using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Network;
using System;
using System.Linq;
using System.Timers;

namespace UIInfoSuite.UIElements
{
    class ShowWhenAnimalNeedsPet : IDisposable
    {
        private readonly Timer _timer = new Timer();
        private float _yMovementPerDraw;
        private float _alpha;
        private readonly IModHelper _helper;

        public ShowWhenAnimalNeedsPet(IModHelper helper)
        {
            _timer.Elapsed += StartDrawingPetNeeds;
            _helper = helper;
        }

        public void ToggleOption(bool showWhenAnimalNeedsPet)
        {
            _timer.Stop();
            _helper.Events.Player.Warped -= OnWarped;
            _helper.Events.Display.RenderingHud -= OnRenderingHud_DrawAnimalHasProduct;

            if (showWhenAnimalNeedsPet)
            {
                _timer.Start();
                _helper.Events.Player.Warped += OnWarped;
                _helper.Events.Display.RenderingHud += OnRenderingHud_DrawAnimalHasProduct;
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
                            Vector2 positionAboveAnimal = GetPetPositionAboveAnimal(animal.Value);
                            positionAboveAnimal.Y += (float)(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 300.0 + (double)animal.Value.Name.GetHashCode()) * 5.0);
                            Game1.spriteBatch.Draw(
                                Game1.emoteSpriteSheet,
                                new Vector2(positionAboveAnimal.X + 14f, positionAboveAnimal.Y),
                                new Rectangle(3 * (Game1.tileSize / 4) % Game1.emoteSpriteSheet.Width, 3 * (Game1.tileSize / 4) / Game1.emoteSpriteSheet.Width * (Game1.tileSize / 4), Game1.tileSize / 4, Game1.tileSize / 4),
                                Color.White * 0.9f,
                                0.0f,
                                Vector2.Zero,
                                4f,
                                SpriteEffects.None,
                                1f);

                            Rectangle sourceRectangle = GameLocation.getSourceRectForObject(animal.Value.currentProduce.Value);
                            Game1.spriteBatch.Draw(
                                Game1.objectSpriteSheet,
                                new Vector2(positionAboveAnimal.X + 28f, positionAboveAnimal.Y + 8f),
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
                if (e.NewLocation is AnimalHouse || e.NewLocation is Farm)
                {
                    _timer.Interval = 1000;
                    _timer.Start();
                }
                else
                {
                    _timer.Stop();
                    StopDrawingPetNeeds();
                }
            }
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud_DrawNeedsPetTooltip(object sender, RenderingHudEventArgs e)
        {
            if (!Game1.eventUp && Game1.activeClickableMenu == null)
            {
                DrawIconForFarmAnimals();
                DrawIconForPets();
            }
        }

        private void StartDrawingPetNeeds(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            _helper.Events.Display.RenderingHud += OnRenderingHud_DrawNeedsPetTooltip;
            _helper.Events.GameLoop.UpdateTicked += UpdateTicked;
            _yMovementPerDraw = -3f;
            _alpha = 1f;
        }

        private void StopDrawingPetNeeds()
        {
            _helper.Events.Display.RenderingHud -= OnRenderingHud_DrawNeedsPetTooltip;
            _helper.Events.GameLoop.UpdateTicked -= UpdateTicked;
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            // update pet draw
            if (e.IsMultipleOf(2))
            {
                _yMovementPerDraw += 0.3f;
                _alpha -= 0.014f;
                if (_alpha < 0.1f)
                {
                    StopDrawingPetNeeds();
                    _timer.Start();
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
                        Vector2 positionAboveAnimal = GetPetPositionAboveAnimal(animal.Value);
                        String animalType = animal.Value.type.Value.ToLower();

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
                            new Vector2(positionAboveAnimal.X, positionAboveAnimal.Y + _yMovementPerDraw),
                            new Rectangle(32, 0, 16, 16),
                            Color.White * _alpha,
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
                    Vector2 positionAboveAnimal = GetPetPositionAboveAnimal(character);
                    positionAboveAnimal.X += 50f;
                    positionAboveAnimal.Y -= 20f;
                    Game1.spriteBatch.Draw(
                        Game1.mouseCursors,
                        new Vector2(positionAboveAnimal.X, positionAboveAnimal.Y + _yMovementPerDraw),
                        new Rectangle(32, 0, 16, 16),
                        Color.White * _alpha,
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
