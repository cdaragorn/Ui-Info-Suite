using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using UIInfoSuite.Extensions;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Quests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UIInfoSuite.UIElements
{
    class LocationOfTownsfolk : IDisposable
    {
        #region Members
        private List<NPC> _townsfolk = new List<NPC>();
        private List<OptionsCheckbox> _checkboxes = new List<OptionsCheckbox>();
        private const int SocialPanelWidth = 190;
        private const int SocialPanelXOffset = 160;
        private SocialPage _socialPage;
        private List<ClickableTextureComponent> _friendNames;
        private readonly IDictionary<String, String> _options;
        private readonly IModHelper _helper;

        private static readonly Dictionary<String, KeyValuePair<int, int>> _mapLocations = new Dictionary<string, KeyValuePair<int, int>>()
        {
            { "HarveyRoom", new KeyValuePair<int, int>(677, 304) },
            { "BathHouse_Pool", new KeyValuePair<int, int>(576, 60) },
            { "WizardHouseBasement", new KeyValuePair<int, int>(196, 352) },
            { "BugLand", new KeyValuePair<int, int>(0, 0) },
            { "Desert", new KeyValuePair<int, int>(60, 92) },
            { "Cellar", new KeyValuePair<int, int>(0, 0) },
            { "JojaMart", new KeyValuePair<int, int>(872, 280) },
            { "Tent", new KeyValuePair<int, int>(784, 128) },
            { "HaleyHouse", new KeyValuePair<int, int>(652, 408) },
            { "Hospital", new KeyValuePair<int, int>(677, 304) },
            { "FarmHouse", new KeyValuePair<int, int>(470, 260) },
            { "ScienceHouse", new KeyValuePair<int, int>(732, 148) },
            { "ManorHouse", new KeyValuePair<int, int>(768, 395) },
            { "AdventureGuild", new KeyValuePair<int, int>(0, 0) },
            { "SeedShop", new KeyValuePair<int, int>(696, 296) },
            { "Blacksmith", new KeyValuePair<int, int>(852, 388) },
            { "JoshHouse", new KeyValuePair<int, int>(740, 320) },
            { "SandyHouse", new KeyValuePair<int, int>(40, 40) },
            { "Tunnel", new KeyValuePair<int, int>(0, 0) },
            { "CommunityCenter", new KeyValuePair<int, int>(692, 204) },
            { "Backwoods", new KeyValuePair<int, int>(460, 156) },
            { "ElliottHouse", new KeyValuePair<int, int>(826, 550) },
            { "SebastianRoom", new KeyValuePair<int, int>(732, 148) },
            { "BathHouse_Entry", new KeyValuePair<int, int>(576, 60) },
            { "Greenhouse", new KeyValuePair<int, int>(0, 0) },
            { "Sewer", new KeyValuePair<int, int>(380, 596) },
            { "WizardHouse", new KeyValuePair<int, int>(196, 352) },
            { "Trailer", new KeyValuePair<int, int>(780, 360) },
            { "Forest", new KeyValuePair<int, int>(80, 272) },
            { "Woods", new KeyValuePair<int, int>(100, 272) },
            { "WitchSwamp", new KeyValuePair<int, int>(0, 0) },
            { "ArchaeologyHouse", new KeyValuePair<int, int>(892, 416) },
            { "FishShop", new KeyValuePair<int, int>(844, 608) },
            { "Saloon", new KeyValuePair<int, int>(714, 354) },
            { "LeahHouse", new KeyValuePair<int, int>(452, 436) },
            { "Town", new KeyValuePair<int, int>(680, 360) },
            { "Mountain", new KeyValuePair<int, int>(762, 154) },
            { "BusStop", new KeyValuePair<int, int>(516, 224) },
            { "Railroad", new KeyValuePair<int, int>(644, 64) },
            { "SkullCave", new KeyValuePair<int, int>(0, 0) },
            { "BathHouse_WomensLocker", new KeyValuePair<int, int>(576, 60) },
            { "Beach", new KeyValuePair<int, int>(790, 550) },
            { "BathHouse_MensLocker", new KeyValuePair<int, int>(576, 60) },
            { "Mine", new KeyValuePair<int, int>(880, 100) },
            { "WitchHut", new KeyValuePair<int, int>(0, 0) },
            { "AnimalShop", new KeyValuePair<int, int>(420, 392) },
            { "SamHouse", new KeyValuePair<int, int>(612, 396) },
            { "WitchWarpCave", new KeyValuePair<int, int>(0, 0) },
            { "Club", new KeyValuePair<int, int>(60, 92) }
        };

#endregion

        public LocationOfTownsfolk(IModHelper helper, IDictionary<String, String> options)
        {
            _helper = helper;
            _options = options;
        }

        public void Dispose()
        {
            ToggleShowNPCLocationsOnMap(false);
        }

        public void ToggleShowNPCLocationsOnMap(bool showLocations)
        {
            OnMenuChange(null, null);
            GraphicsEvents.OnPostRenderGuiEvent -= DrawSocialPageOptions;
            GraphicsEvents.OnPostRenderGuiEvent -= DrawNPCLocationsOnMap;
            ControlEvents.MouseChanged -= HandleClickForSocialPage;
            ControlEvents.ControllerButtonPressed -= HandleGamepadPressForSocialPage;
            MenuEvents.MenuChanged -= OnMenuChange;

            if (showLocations)
            {
                GraphicsEvents.OnPostRenderGuiEvent += DrawSocialPageOptions;
                GraphicsEvents.OnPostRenderGuiEvent += DrawNPCLocationsOnMap;
                ControlEvents.MouseChanged += HandleClickForSocialPage;
                ControlEvents.ControllerButtonPressed += HandleGamepadPressForSocialPage;
                MenuEvents.MenuChanged += OnMenuChange;
            }
        }

        private void HandleGamepadPressForSocialPage(object sender, EventArgsControllerButtonPressed e)
        {
            if (e.ButtonPressed == Buttons.A)
                CheckSelectedBox();
        }

        private void OnMenuChange(object sender, EventArgsClickableMenuChanged e)
        {
            if (Game1.activeClickableMenu is GameMenu)
            {
                List<IClickableMenu> clickableMenuList = typeof(GameMenu)
                    .GetField("pages", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(Game1.activeClickableMenu) as List<IClickableMenu>;

                foreach (var menu in clickableMenuList)
                {
                    if (menu is SocialPage)
                    {
                        _socialPage = menu as SocialPage;
                        _friendNames = typeof(SocialPage)
                            .GetField("friendNames", BindingFlags.Instance | BindingFlags.NonPublic)
                            .GetValue(menu) as List<ClickableTextureComponent>;
                        break;
                    }
                }
                _townsfolk.Clear();
                foreach (var location in Game1.locations)
                {
                    foreach (var npc in location.characters)
                    {
                        if (Game1.player.friendships.ContainsKey(npc.name))
                            _townsfolk.Add(npc);
                    }
                }
                _checkboxes.Clear();
                foreach (var friendName in _friendNames)
                {
                    int hashCode = friendName.name.GetHashCode();
                    OptionsCheckbox checkbox = new OptionsCheckbox("", hashCode);
                    _checkboxes.Add(checkbox);
                    if (!Game1.player.friendships.ContainsKey(friendName.name))
                    {
                        checkbox.greyedOut = true;
                        checkbox.isChecked = false;
                    }
                    checkbox.isChecked = _options.SafeGet(hashCode.ToString()).SafeParseBool();
                }
            }
        }

        private void HandleClickForSocialPage(object sender, EventArgsMouseStateChanged e)
        {
            if (Game1.activeClickableMenu is GameMenu &&
                e.PriorState.LeftButton != ButtonState.Pressed &&
                e.NewState.LeftButton == ButtonState.Pressed)
            {
                CheckSelectedBox();
            }
        }

        private void CheckSelectedBox()
        {
            if (Game1.activeClickableMenu is GameMenu)
            {
                int slotPosition = (int)typeof(SocialPage)
                    .GetField("slotPosition", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(_socialPage);

                for (int i = slotPosition; i < slotPosition + 5; ++i)
                {
                    OptionsCheckbox checkbox = _checkboxes[i];
                    if (checkbox.bounds.Contains(Game1.getMouseX(), Game1.getMouseY()) &&
                        !checkbox.greyedOut)
                    {
                        checkbox.isChecked = !checkbox.isChecked;
                        _options[checkbox.whichOption.ToString()] = checkbox.isChecked.ToString();
                        Game1.playSound("drumkit6");
                    }
                }
            }
        }

        private void DrawNPCLocationsOnMap(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu)
            {
                GameMenu gameMenu = Game1.activeClickableMenu as GameMenu;
                if (gameMenu.currentTab == 3)
                {
                    foreach (var character in _townsfolk)
                    {
                        try
                        {
                            int hashCode = character.name.GetHashCode();
                            bool drawCharacter = _options.SafeGet(hashCode.ToString()).SafeParseBool();

                            if (drawCharacter)
                            {
                                KeyValuePair<int, int> location;
                                location = new KeyValuePair<int, int>((int)character.Position.X, (int)character.position.Y);

                                if (character.currentLocation.Name == "Town")
                                {
                                    String locationName = character.currentLocation.Name;
                                    xTile.Map map = character.currentLocation.Map;
                                    int xStart = 595;
                                    int yStart = 163;
                                    int townWidth = 345;
                                    int townHeight = 330;

                                    float xScale = (float)townWidth / (float)map.DisplayWidth;
                                    float yScale = (float)townHeight / (float)map.DisplayHeight;

                                    float scaledX = character.position.X * xScale;
                                    float scaledY = character.position.Y * yScale;
                                    int xPos = (int)scaledX + xStart;
                                    int yPos = (int)scaledY + yStart;
                                    location = new KeyValuePair<int, int>(xPos, yPos);
                                }
                                else
                                {
                                    _mapLocations.TryGetValue(character.currentLocation.name, out location);
                                }
                                Rectangle headShot = character.GetHeadShot();
                                int xBase = Game1.activeClickableMenu.xPositionOnScreen - 158;
                                int yBase = Game1.activeClickableMenu.yPositionOnScreen - 40;

                                int x = xBase + location.Key;
                                int y = yBase + location.Value;

                                Color color = character.CurrentDialogue.Count <= 0 ?
                                    Color.Gray : Color.White;
                                ClickableTextureComponent textureComponent =
                                    new ClickableTextureComponent(
                                        character.name,
                                        new Rectangle(x, y, 0, 0),
                                        null,
                                        character.name,
                                        character.sprite.Texture,
                                        headShot,
                                        2.3f);
                                Game1.spriteBatch.Draw(
                                    character.sprite.Texture,
                                    new Vector2(x, y),
                                    new Rectangle?(headShot),
                                    color,
                                    0.0f,
                                    Vector2.Zero,
                                    2f,
                                    SpriteEffects.None,
                                    1f);

                                foreach (var quest in Game1.player.questLog)
                                {
                                    if (quest.accepted && quest.dailyQuest && !quest.completed)
                                    {
                                        bool isQuestTarget = false;
                                        switch (quest.questType)
                                        {
                                            case 3: isQuestTarget = (quest as ItemDeliveryQuest).target == character.name; break;
                                            case 4: isQuestTarget = (quest as SlayMonsterQuest).target == character.name; break;
                                            case 7: isQuestTarget = (quest as FishingQuest).target == character.name; break;
                                            case 10: isQuestTarget = (quest as ResourceCollectionQuest).target == character.name; break;
                                        }

                                        if (isQuestTarget)
                                            Game1.spriteBatch.Draw(
                                                Game1.mouseCursors,
                                                new Vector2(x + 10, y - 12),
                                                new Rectangle(394, 495, 4, 10),
                                                Color.White,
                                                0.0f,
                                                Vector2.Zero,
                                                3f,
                                                SpriteEffects.None,
                                                1f);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ModEntry.MonitorObject.Log(ex.Message + Environment.NewLine + ex.StackTrace, LogLevel.Error);
                        }
                    }

                    if (!Game1.options.hardwareCursor)
                        Game1.spriteBatch.Draw(
                            Game1.mouseCursors,
                            new Vector2(Game1.getMouseX(), Game1.getMouseY()),
                            new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, Game1.mouseCursor, 16, 16)),
                            Color.White,
                            0.0f,
                            Vector2.Zero,
                            Game1.pixelZoom + (Game1.dialogueButtonScale / 150.0f),
                            SpriteEffects.None,
                            1f);

                    String hoverText = (String)typeof(MapPage)
                        .GetField(
                            "hoverText",
                            BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(((List<IClickableMenu>)typeof(GameMenu)
                            .GetField("pages", BindingFlags.Instance | BindingFlags.NonPublic)
                            .GetValue(gameMenu))[gameMenu.currentTab]);

                    IClickableMenu.drawHoverText(
                        Game1.spriteBatch,
                        hoverText,
                        Game1.smallFont);
                }
            }
        }

        private void DrawSocialPageOptions(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu &&
                (Game1.activeClickableMenu as GameMenu).currentTab == 2)
            {
                Game1.drawDialogueBox(
                    Game1.activeClickableMenu.xPositionOnScreen - SocialPanelXOffset, 
                    Game1.activeClickableMenu.yPositionOnScreen, 
                    SocialPanelWidth, 
                    Game1.activeClickableMenu.height, 
                    false, 
                    true);

                int slotPosition = (int)typeof(SocialPage)
                    .GetField("slotPosition", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(_socialPage);
                int yOffset = 0;

                for (int i = slotPosition; i < slotPosition + 5 && i <= _friendNames.Count; ++i)
                {
                    OptionsCheckbox checkbox = _checkboxes[i];
                    checkbox.bounds.X = Game1.activeClickableMenu.xPositionOnScreen - 60;

                    checkbox.bounds.Y = Game1.activeClickableMenu.yPositionOnScreen + 130 + yOffset;

                    checkbox.draw(Game1.spriteBatch, 0, 0);
                    yOffset += 112;
                    Color color = checkbox.isChecked ? Color.White : Color.Gray;

                    Game1.spriteBatch.Draw(
                        Game1.mouseCursors, 
                        new Vector2(checkbox.bounds.X - 50, checkbox.bounds.Y), 
                        new Rectangle(80, 0, 16, 16), 
                        color,
                        0.0f, 
                        Vector2.Zero, 
                        3f, 
                        SpriteEffects.None, 
                        1f);

                    if (yOffset != 560)
                    {
                        Game1.spriteBatch.Draw(
                            Game1.staminaRect, 
                            new Rectangle(
                                checkbox.bounds.X - 50, 
                                checkbox.bounds.Y + 72, 
                                SocialPanelWidth / 2 - 6, 
                                4), 
                            Color.SaddleBrown);

                        Game1.spriteBatch.Draw(
                            Game1.staminaRect,
                            new Rectangle(
                                checkbox.bounds.X - 50,
                                checkbox.bounds.Y + 76,
                                SocialPanelWidth / 2 - 6,
                                4),
                            Color.BurlyWood);
                    }
                    Game1.spriteBatch.Draw(
                        Game1.mouseCursors,
                        new Vector2(
                            Game1.getMouseX(), 
                            Game1.getMouseY()),
                        Game1.getSourceRectForStandardTileSheet(
                            Game1.mouseCursors, 
                            Game1.mouseCursor, 
                            16, 
                            16),
                        Color.White,
                        0.0f,
                        Vector2.Zero,
                        Game1.pixelZoom + (Game1.dialogueButtonScale / 150.0f),
                        SpriteEffects.None,
                        1f);

                    if (checkbox.bounds.Contains(Game1.getMouseX(), Game1.getMouseY()))
                        IClickableMenu.drawHoverText(
                            Game1.spriteBatch,
                            "Track on map",
                            Game1.dialogueFont);
                }
            }
        }
    }
}
