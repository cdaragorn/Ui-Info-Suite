using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Reflection;
using UIInfoSuite.Extensions;

namespace UIInfoSuite.UIElements
{
    class ShowQueenOfSauceIcon : IDisposable
    {
        private Dictionary<string, string> _recipesByDescription = new Dictionary<string, string>();
        private Dictionary<string, string> _recipes = new Dictionary<string, string>();
        private string _todaysRecipe;
        private NPC _gus;
        private bool _drawQueenOfSauceIcon = false;
        private bool _drawDishOfDayIcon = false;
        private ClickableTextureComponent _queenOfSauceIcon;
        private readonly IModHelper _helper;

        public void ToggleOption(bool showQueenOfSauceIcon)
        {
            _helper.Events.Display.RenderingHud -= OnRenderingHud;
            _helper.Events.Display.RenderedHud -= OnRenderedHud;
            _helper.Events.GameLoop.DayStarted -= OnDayStarted;
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

            if (showQueenOfSauceIcon)
            {
                LoadRecipes();
                CheckForNewRecipe();
                _helper.Events.GameLoop.DayStarted += OnDayStarted;
                _helper.Events.Display.RenderingHud += OnRenderingHud;
                _helper.Events.Display.RenderedHud += OnRenderedHud;
                _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            }
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            // check if learned recipe
            if (e.IsOneSecond && _drawQueenOfSauceIcon && Game1.player.knowsRecipe(_todaysRecipe))
                _drawQueenOfSauceIcon = false;
        }

        public ShowQueenOfSauceIcon(IModHelper helper)
        {
            _helper = helper;
        }

        private void LoadRecipes()
        {
            if (_recipes.Count == 0)
            {
                _recipes = Game1.content.Load<Dictionary<string, string>>("Data\\TV\\CookingChannel");

                foreach (var next in _recipes)
                {
                    var values = next.Value.Split('/');

                    if (values.Length > 1)
                    {
                        _recipesByDescription[values[1]] = values[0];
                    }
                }
            }
        }

        private void FindGus()
        {
            foreach (var location in Game1.locations)
            {
                foreach (var npc in location.characters)
                {
                    if (npc.Name == "Gus")
                    {
                        _gus = npc;
                        break;
                    }
                }
                if (_gus != null)
                    break;
            }
        }

        private string[] GetTodaysRecipe()
        {
            var array1 = new string[2];
            var recipeNum = (int)(Game1.stats.DaysPlayed % 224 / 7);
            //var recipes = Game1.content.Load<Dictionary<String, String>>("Data\\TV\\CookingChannel");

            var recipeValue = _recipes.SafeGet(recipeNum.ToString());
            string[] splitValues = null;
            string key = null;
            var checkCraftingRecipes = true;
            
            if (string.IsNullOrEmpty(recipeValue))
            {
                recipeValue = _recipes["1"];
                checkCraftingRecipes = false;
            }
            splitValues = recipeValue.Split('/');
            key = splitValues[0];

            ///Game code sets this to splitValues[1] to display the language specific
            ///recipe name. We are skipping a bunch of their steps to just get the
            ///english name needed to tell if the player knows the recipe or not
            array1[0] = key;
            if (checkCraftingRecipes)
            {
                var craftingRecipesValue = CraftingRecipe.cookingRecipes.SafeGet(key);
                if (!string.IsNullOrEmpty(craftingRecipesValue))
                    splitValues = craftingRecipesValue.Split('/');
            }

            var languageRecipeName = (_helper.Content.CurrentLocaleConstant == LocalizedContentManager.LanguageCode.en) ?
                key : splitValues[splitValues.Length - 1];

            array1[1] = languageRecipeName;

            //String str = null;
            //if (!Game1.player.cookingRecipes.ContainsKey(key))
            //{
            //    str = Game1.content.LoadString(@"Strings\StringsFromCSFiles:TV.cs.13153", languageRecipeName);
            //}
            //else
            //{
            //    str = Game1.content.LoadString(@"Strings\StringsFromCSFiles:TV.cs.13151", languageRecipeName);
            //}
            //array1[1] = str;

            return array1;
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            // draw icon
            if (!Game1.eventUp)
            {
                if (_drawQueenOfSauceIcon)
                {
                    var iconPosition = IconHandler.Handler.GetNewIconPosition();

                    _queenOfSauceIcon = new ClickableTextureComponent(
                        new Rectangle(iconPosition.X, iconPosition.Y, 40, 40),
                        Game1.mouseCursors,
                        new Rectangle(609, 361, 28, 28),
                        1.3f);
                    _queenOfSauceIcon.draw(Game1.spriteBatch);
                }

                if (_drawDishOfDayIcon)
                {
                    var iconLocation = IconHandler.Handler.GetNewIconPosition();
                    var scale = 2.9f;

                    Game1.spriteBatch.Draw(
                        Game1.objectSpriteSheet,
                        new Vector2(iconLocation.X, iconLocation.Y),
                        new Rectangle(306, 291, 14, 14),
                        Color.White,
                        0,
                        Vector2.Zero,
                        scale,
                        SpriteEffects.None,
                        1f);

                    var texture =
                        new ClickableTextureComponent(
                            _gus.Name,
                            new Rectangle(
                                iconLocation.X - 7,
                                iconLocation.Y - 2,
                                (int)(16.0 * scale),
                                (int)(16.0 * scale)),
                            null,
                            _gus.Name,
                            _gus.Sprite.Texture,
                            _gus.GetHeadShot(),
                            2f);

                    texture.draw(Game1.spriteBatch);

                    if (texture.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                    {
                        IClickableMenu.drawHoverText(
                            Game1.spriteBatch,
                            "Gus is selling " + Game1.dishOfTheDay.DisplayName + " recipe today!",
                            Game1.dialogueFont);
                    }
                }
            }
        }

        /// <summary>Raised after drawing the HUD (item toolbar, clock, etc) to the sprite batch, but before it's rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // draw hover text
            if (_drawQueenOfSauceIcon &&
                _queenOfSauceIcon.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                IClickableMenu.drawHoverText(
                    Game1.spriteBatch,
                    _helper.SafeGetString(
                        LanguageKeys.TodaysRecipe) + _todaysRecipe,
                    Game1.dialogueFont);
            }
        }

        public void Dispose()
        {
            ToggleOption(false);
        }

        /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            CheckForNewRecipe();
        }

        private void CheckForNewRecipe()
        {
            var tv = new TV();
            var numRecipesKnown = Game1.player.cookingRecipes.Count();
            var recipes = typeof(TV).GetMethod("getWeeklyRecipe", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(tv, null) as string[];
            //String[] recipe = GetTodaysRecipe();
            //_todaysRecipe = recipe[1];
            _todaysRecipe = _recipesByDescription.SafeGet(recipes[0]);

            if (Game1.player.cookingRecipes.Count() > numRecipesKnown)
                Game1.player.cookingRecipes.Remove(_todaysRecipe);

            _drawQueenOfSauceIcon = (Game1.dayOfMonth % 7 == 0 || (Game1.dayOfMonth - 3) % 7 == 0) &&
                Game1.stats.DaysPlayed > 5 && 
                !Game1.player.knowsRecipe(_todaysRecipe);
            //_drawDishOfDayIcon = !Game1.player.knowsRecipe(Game1.dishOfTheDay.Name);
        }
    }
}
