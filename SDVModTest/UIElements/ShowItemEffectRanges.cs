using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using System;
using System.Collections.Generic;
using System.Threading;

namespace UIInfoSuite.UIElements
{
    class ShowItemEffectRanges : IDisposable
    {
        private readonly List<Point> _effectiveArea = new List<Point>();
        private readonly ModConfig _modConfig;
        private readonly IModEvents _events;

        private readonly Mutex _mutex = new Mutex();

        private static readonly int[][] _junimoHutArray = new int[17][]
        {
            new int[17] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[17] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[17] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[17] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[17] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[17] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[17] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[17] { 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[17] { 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[17] { 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[17] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[17] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[17] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[17] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[17] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[17] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[17] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
        };

        public ShowItemEffectRanges(ModConfig modConfig, IModEvents events)
        {
            _modConfig = modConfig;
            _events = events;
        }

        public void ToggleOption(bool showItemEffectRanges)
        {
            _events.Display.Rendered -= OnRendered;
            _events.GameLoop.UpdateTicked -= OnUpdateTicked;

            if (showItemEffectRanges)
            {
                _events.Display.Rendered += OnRendered;
                _events.GameLoop.UpdateTicked += OnUpdateTicked;
            }
        }

        public void Dispose()
        {
            ToggleOption(false);
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!e.IsMultipleOf(4))
                return;

            if (_mutex.WaitOne())
            {
                try
                {
                    // check draw tile outlines
                    _effectiveArea.Clear();
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }

            }
            if (Game1.activeClickableMenu == null &&
                        !Game1.eventUp)
            {
                if (Game1.currentLocation is BuildableGameLocation buildableLocation)
                {
                    Building building = buildableLocation.getBuildingAt(Game1.currentCursorTile);

                    if (building is JunimoHut)
                    {
                        foreach (var nextBuilding in buildableLocation.buildings)
                        {
                            if (nextBuilding is JunimoHut nextHut)
                                ParseConfigToHighlightedArea(_junimoHutArray, nextHut.tileX.Value + 1, nextHut.tileY.Value + 1);
                        }
                    }
                }

                if (Game1.player.CurrentItem != null)
                {
                    String name = Game1.player.CurrentItem.Name.ToLower();
                    Item currentItem = Game1.player.CurrentItem;
                    List<StardewValley.Object> objects = null;

                    int[][] arrayToUse = null;

                    if (name.Contains("arecrow"))
                    {
                        arrayToUse = new int[17][];
                        for (int i = 0; i < 17; ++i)
                        {
                            arrayToUse[i] = new int[17];
                            for (int j = 0; j < 17; ++j)
                            {
                                arrayToUse[i][j] = (Math.Abs(i - 8) + Math.Abs(j - 8) <= 12) ? 1 : 0;
                            }
                        }
                        ParseConfigToHighlightedArea(arrayToUse, TileUnderMouseX, TileUnderMouseY);
                        objects = GetObjectsInLocationOfSimilarName("arecrow");
                        if (objects != null)
                        {
                            foreach (StardewValley.Object next in objects)
                            {
                                ParseConfigToHighlightedArea(arrayToUse, (int)next.TileLocation.X, (int)next.TileLocation.Y);
                            }
                        }

                    }
                    else if (name.Contains("sprinkler"))
                    {
                        if (name.Contains("iridium"))
                        {
                            arrayToUse = _modConfig.IridiumSprinkler;
                        }
                        else if (name.Contains("quality"))
                        {
                            arrayToUse = _modConfig.QualitySprinkler;
                        }
                        else if (name.Contains("prismatic"))
                        {
                            arrayToUse = _modConfig.PrismaticSprinkler;
                        }
                        else
                        {
                            arrayToUse = _modConfig.Sprinkler;
                        }

                        if (arrayToUse != null)
                            ParseConfigToHighlightedArea(arrayToUse, TileUnderMouseX, TileUnderMouseY);

                        objects = GetObjectsInLocationOfSimilarName("sprinkler");

                        if (objects != null)
                        {
                            foreach (StardewValley.Object next in objects)
                            {
                                string objectName = next.name.ToLower();
                                if (objectName.Contains("iridium"))
                                {
                                    arrayToUse = _modConfig.IridiumSprinkler;
                                }
                                else if (objectName.Contains("quality"))
                                {
                                    arrayToUse = _modConfig.QualitySprinkler;
                                }
                                else if (name.Contains("prismatic"))
                                {
                                    arrayToUse = _modConfig.PrismaticSprinkler;
                                }
                                else
                                {
                                    arrayToUse = _modConfig.Sprinkler;
                                }

                                if (arrayToUse != null)
                                    ParseConfigToHighlightedArea(arrayToUse, (int)next.TileLocation.X, (int)next.TileLocation.Y);
                            }
                        }
                    }
                    else if (name.Contains("bee house"))
                    {
                        ParseConfigToHighlightedArea(_modConfig.Beehouse, TileUnderMouseX, TileUnderMouseY);
                    }

                }
            }

        }

        /// <summary>Raised after the game draws to the sprite patch in a draw tick, just before the final sprite batch is rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRendered(object sender, RenderedEventArgs e)
        {
            if (_mutex.WaitOne(0))
            {
                try
                {
                    // draw tile outlines
                    foreach (Point point in _effectiveArea)
                        Game1.spriteBatch.Draw(
                            Game1.mouseCursors,
                            Game1.GlobalToLocal(new Vector2(point.X * Game1.tileSize, point.Y * Game1.tileSize)),
                            new Rectangle(194, 388, 16, 16),
                            Color.White * 0.7f,
                            0.0f,
                            Vector2.Zero,
                            Game1.pixelZoom,
                            SpriteEffects.None,
                            0.01f);
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }
            }
        }

        private void ParseConfigToHighlightedArea(int[][] highlightedLocation, int xPos, int yPos)
        {
            int xOffset = highlightedLocation.Length / 2;

            if (_mutex.WaitOne())
            {
                try
                {
                    for (int i = 0; i < highlightedLocation.Length; ++i)
                    {
                        int yOffset = highlightedLocation[i].Length / 2;
                        for (int j = 0; j < highlightedLocation[i].Length; ++j)
                        {
                            if (highlightedLocation[i][j] == 1)
                                _effectiveArea.Add(new Point(xPos + i - xOffset, yPos + j - yOffset));
                        }
                    }
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }
            }
        }

        private int TileUnderMouseX
        {
            get { return (Game1.getMouseX() + Game1.viewport.X) / Game1.tileSize; }
        }

        private int TileUnderMouseY
        {
            get { return (Game1.getMouseY() + Game1.viewport.Y) / Game1.tileSize; }
        }

        private List<StardewValley.Object> GetObjectsInLocationOfSimilarName(String nameContains)
        {
            List<StardewValley.Object> result = new List<StardewValley.Object>();

            if (!String.IsNullOrEmpty(nameContains))
            {
                nameContains = nameContains.ToLower();
                var objects = Game1.currentLocation.Objects;

                foreach (var nextThing in objects.Values)
                {
                    if (nextThing.name.ToLower().Contains(nameContains))
                        result.Add(nextThing);
                }
            }
            return result;
        }
    }
}
