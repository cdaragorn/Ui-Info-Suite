using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UIInfoSuite.Extensions;
using StardewConfigFramework;

namespace UIInfoSuite.UIElements
{
    class ShowQueenOfSauceIcon: IDisposable
    {
        private Dictionary<String, String> _recipesByDescription = new Dictionary<string, string>();
        private Dictionary<String, String> _recipes = new Dictionary<String, string>();
        private String _todaysRecipe;
        private NPC _gus;
        private bool _drawQueenOfSauceIcon = false;
        private bool _drawDishOfDayIcon = false;
        private readonly IModHelper _helper;
        private readonly ModOptionToggle _showQueenOfSauceIcon;

        public void ToggleOption(string identifier, bool showQueenOfSauceIcon)
        {
            if (OptionKeys.ShowWhenNewRecipesAreAvailable != identifier)
                return;

            GraphicsEvents.OnPreRenderHudEvent -= DrawIcon;
            TimeEvents.AfterDayStarted -= CheckForNewRecipe;

            if (showQueenOfSauceIcon)
            {
                LoadRecipes();
                CheckForNewRecipe(null, null);
                TimeEvents.AfterDayStarted += CheckForNewRecipe;
                GraphicsEvents.OnPreRenderHudEvent += DrawIcon;
            }
        }

        public ShowQueenOfSauceIcon(ModOptions modOptions, IModHelper helper)
        {
            _helper = helper;

            _showQueenOfSauceIcon = modOptions.GetOptionWithIdentifier<ModOptionToggle>(OptionKeys.ShowWhenNewRecipesAreAvailable) ?? new ModOptionToggle(OptionKeys.ShowWhenNewRecipesAreAvailable, "Show when new recipes are available");
            _showQueenOfSauceIcon.ValueChanged += ToggleOption;
            modOptions.AddModOption(_showQueenOfSauceIcon);

            ToggleOption(_showQueenOfSauceIcon.identifier, _showQueenOfSauceIcon.IsOn);
        }

        private void LoadRecipes()
        {
            if (_recipes.Count == 0)
            {
                _recipes = Game1.content.Load<Dictionary<String, String>>("Data\\TV\\CookingChannel");

                foreach (var next in _recipes)
                {
                    string[] values = next.Value.Split('/');

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
                    if (npc.name == "Gus")
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
            String[] array1 = new string[2];
            int recipeNum = (int) (Game1.stats.DaysPlayed % 224 / 7);
            //var recipes = Game1.content.Load<Dictionary<String, String>>("Data\\TV\\CookingChannel");

            String recipeValue = _recipes.SafeGet(recipeNum.ToString());
            String[] splitValues = null;
            String key = null;
            bool checkCraftingRecipes = true;

            if (String.IsNullOrEmpty(recipeValue))
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
                String craftingRecipesValue = CraftingRecipe.cookingRecipes.SafeGet(key);
                if (!String.IsNullOrEmpty(craftingRecipesValue))
                    splitValues = craftingRecipesValue.Split('/');
            }

            string languageRecipeName = (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.en) ?
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

        private void DrawIcon(object sender, EventArgs e)
        {
            if (!Game1.eventUp)
            {
                if (_drawQueenOfSauceIcon)
                {
                    Point iconPosition = IconHandler.Handler.GetNewIconPosition();

                    ClickableTextureComponent texture = new ClickableTextureComponent(
                            new Rectangle(iconPosition.X, iconPosition.Y, 40, 40),
                            Game1.mouseCursors,
                            new Rectangle(609, 361, 28, 28),
                            1.3f);
                    texture.draw(Game1.spriteBatch);

                    if (texture.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                    {
                        IClickableMenu.drawHoverText(
                                Game1.spriteBatch,
                                _helper.SafeGetString(
                                        LanguageKeys.TodaysRecipe) + _todaysRecipe,
                                Game1.dialogueFont);
                    }
                }

                if (_drawDishOfDayIcon)
                {
                    Point iconLocation = IconHandler.Handler.GetNewIconPosition();
                    float scale = 2.9f;

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

                    ClickableTextureComponent texture =
                            new ClickableTextureComponent(
                                    _gus.name,
                                    new Rectangle(
                                            iconLocation.X - 7,
                                            iconLocation.Y - 2,
                                            (int) (16.0 * scale),
                                            (int) (16.0 * scale)),
                                    null,
                                    _gus.name,
                                    _gus.sprite.Texture,
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

        public void Dispose()
        {
            ToggleOption(OptionKeys.ShowWhenNewRecipesAreAvailable, false);
        }

        private void CheckForNewRecipe(object sender, EventArgs e)
        {
            TV tv = new TV();
            int numRecipesKnown = Game1.player.cookingRecipes.Count;
            String[] recipes = typeof(TV).GetMethod("getWeeklyRecipe", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(tv, null) as String[];
            //String[] recipe = GetTodaysRecipe();
            //_todaysRecipe = recipe[1];
            _todaysRecipe = _recipesByDescription.SafeGet(recipes[0]);

            if (Game1.player.cookingRecipes.Count > numRecipesKnown)
                Game1.player.cookingRecipes.Remove(_todaysRecipe);

            _drawQueenOfSauceIcon = (Game1.dayOfMonth % 7 == 0 || (Game1.dayOfMonth - 3) % 7 == 0) &&
                    Game1.stats.DaysPlayed > 5 &&
                    !Game1.player.knowsRecipe(_todaysRecipe);
            //_drawDishOfDayIcon = !Game1.player.knowsRecipe(Game1.dishOfTheDay.Name);
        }
    }
}
