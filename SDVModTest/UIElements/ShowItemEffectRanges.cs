using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
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
        private readonly PerScreen<List<Point>> _effectiveArea = new PerScreen<List<Point>>(createNewState: () => new List<Point>());
        private readonly PerScreen<bool> _usingGamepad = new PerScreen<bool>();

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
            if (Game1.lastCursorMotionWasMouse)
                _usingGamepad.Value = false;

            if (Game1.isGamePadThumbstickInMotion())
                _usingGamepad.Value = true;

            if (!e.IsMultipleOf(4))
                return;

            if (_mutex.WaitOne())
            {
                try
                {
                    // check draw tile outlines
                    _effectiveArea.Value.Clear();
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
                    var building = buildableLocation.getBuildingAt(Game1.GetPlacementGrabTile());

                    if (building is JunimoHut)
                    {
                        foreach (var nextBuilding in buildableLocation.buildings)
                        {
                            if (nextBuilding is JunimoHut nextHut)
                                ParseConfigToHighlightedArea(_junimoHutArray, nextHut.tileX.Value + 1, nextHut.tileY.Value + 1);
                        }
                    }
                }

                Item currentItem;
                if ((currentItem = Game1.player.CurrentItem) != null && currentItem.isPlaceable())
                {
                    var name = currentItem.Name.ToLower();
                    List<StardewValley.Object> objects = null;

                    int[][] arrayToUse = null;

                    var currentTile = Game1.GetPlacementGrabTile();
                    Game1.isCheckingNonMousePlacement = !Game1.IsPerformingMousePlacement();
                    var validTile = Utility.snapToInt(Utility.GetNearbyValidPlacementPosition(Game1.player, Game1.currentLocation, currentItem, (int)currentTile.X * Game1.tileSize, (int)currentTile.Y * Game1.tileSize))/Game1.tileSize;
                    Game1.isCheckingNonMousePlacement = false;

                    if (name.Contains("arecrow") && !name.Contains("sprinkler") )
                    {
                        arrayToUse = new int[17][];
                        for (var i = 0; i < 17; ++i)
                        {
                            arrayToUse[i] = new int[17];
                            for (var j = 0; j < 17; ++j)
                            {
                                arrayToUse[i][j] = (Math.Abs(i - 8) + Math.Abs(j - 8) <= 12) ? 1 : 0;
                            }
                        }
                        ParseConfigToHighlightedArea(arrayToUse, (int)validTile.X, (int)validTile.Y);
                        objects = GetObjectsInLocationOfSimilarName("arecrow");
                        if (objects != null)
                        {
                            foreach (var next in objects)
                            {
                                if (!next.name.ToLower().Contains("sprinkler"))
                                {
                                    ParseConfigToHighlightedArea(arrayToUse, (int)next.TileLocation.X, (int)next.TileLocation.Y);
                                }
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
                            ParseConfigToHighlightedArea(arrayToUse, (int)validTile.X, (int)validTile.Y);

                        objects = GetObjectsInLocationOfSimilarName("sprinkler");

                        if (objects != null)
                        {
                            foreach (var next in objects)
                            {
                                var objectName = next.name.ToLower();
                                if (objectName.Contains("iridium"))
                                {
                                    arrayToUse = _modConfig.IridiumSprinkler;
                                }
                                else if (objectName.Contains("quality"))
                                {
                                    arrayToUse = _modConfig.QualitySprinkler;
                                }
                                else if (objectName.Contains("prismatic"))
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
                        ParseConfigToHighlightedArea(_modConfig.Beehouse, (int)validTile.X, (int)validTile.Y);
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
                    foreach (var point in _effectiveArea.Value)
                    { 
                        var position = new Vector2(point.X * Utility.ModifyCoordinateFromUIScale(Game1.tileSize), point.Y * Utility.ModifyCoordinateFromUIScale(Game1.tileSize));
                        Game1.spriteBatch.Draw(
                            Game1.mouseCursors,
                            Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(Utility.ModifyCoordinatesForUIScale(position))),
                            new Rectangle(194, 388, 16, 16),
                            Color.White * 0.7f,
                            0.0f,
                            Vector2.Zero,
                            Utility.ModifyCoordinateForUIScale(Game1.pixelZoom),
                            SpriteEffects.None,
                            0.01f);
                    }
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }
            }
        }

        private void ParseConfigToHighlightedArea(int[][] highlightedLocation, int xPos, int yPos)
        {
            var xOffset = highlightedLocation.Length / 2;

            if (_mutex.WaitOne())
            {
                try
                {
                    for (var i = 0; i < highlightedLocation.Length; ++i)
                    {
                        var yOffset = highlightedLocation[i].Length / 2;
                        for (var j = 0; j < highlightedLocation[i].Length; ++j)
                        {
                            if (highlightedLocation[i][j] == 1)
                                _effectiveArea.Value.Add(new Point(xPos + i - xOffset, yPos + j - yOffset));
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

        private List<StardewValley.Object> GetObjectsInLocationOfSimilarName(string nameContains)
        {
            var result = new List<StardewValley.Object>();

            if (!string.IsNullOrEmpty(nameContains))
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
