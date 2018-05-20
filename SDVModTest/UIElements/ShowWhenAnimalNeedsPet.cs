using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace UIInfoSuite.UIElements
{
    class ShowWhenAnimalNeedsPet : IDisposable
    {
        private readonly StardewValley.Object _wool = new StardewValley.Object(440, 1);
        private readonly Timer _timer = new Timer();
        private float _scale;
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
            LocationEvents.LocationsChanged -= OnLocationChange;
            GraphicsEvents.OnPreRenderHudEvent -= DrawAnimalHasProduct;

            if (showWhenAnimalNeedsPet)
            {
                _timer.Start();
                LocationEvents.LocationsChanged += OnLocationChange;
                GraphicsEvents.OnPreRenderHudEvent += DrawAnimalHasProduct;
            }
        }

        public void Dispose()
        {
            ToggleOption(false);
        }

        private void DrawAnimalHasProduct(object sender, EventArgs e)
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

        private void OnLocationChange(object sender, EventArgsGameLocationsChanged e)
        {
            if (e.NewLocation is AnimalHouse ||
                e.NewLocation is Farm)
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

        private void DrawNeedsPetTooltip(object sender, EventArgs e)
        {
            if (!Game1.eventUp &&
                Game1.activeClickableMenu == null)
            {
                DrawIconForFarmAnimals();
                DrawIconForPets();
            }
        }

        private void StartDrawingPetNeeds(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            GraphicsEvents.OnPreRenderHudEvent += DrawNeedsPetTooltip;
            GameEvents.SecondUpdateTick += UpdatePetDraw;
            _scale = 4f;
            _yMovementPerDraw = -3f;
            _alpha = 1f;
        }

        private void StopDrawingPetNeeds()
        {
            GraphicsEvents.OnPreRenderHudEvent -= DrawNeedsPetTooltip;
            GameEvents.SecondUpdateTick -= UpdatePetDraw;
        }

        private void UpdatePetDraw(object sender, EventArgs e)
        {
            _scale += 0.01f;
            _yMovementPerDraw += 0.3f;
            _alpha -= 0.014f;
            if (_alpha < 0.1f)
            {
                StopDrawingPetNeeds();
                _timer.Start();
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
                if (character is Pet &&
                    !_helper.Reflection.GetField<bool>(character, "wasPetToday").GetValue())
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
