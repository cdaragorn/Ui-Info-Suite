using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
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
        #region Properties
        private readonly List<Point> _effectiveArea = new List<Point>();
        private readonly Mutex _mutex = new Mutex();

        private readonly IModHelper _helper;
        #endregion


        #region Lifecycle
        public ShowItemEffectRanges(IModHelper helper)
        {
            _helper = helper;
        }

        public void Dispose()
        {
            ToggleOption(false);
        }

        public void ToggleOption(bool showItemEffectRanges)
        {
            _helper.Events.Display.Rendered -= OnRendered;
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

            if (showItemEffectRanges)
            {
                _helper.Events.Display.Rendered += OnRendered;
                _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            }
        }
        #endregion


        #region Event subscriptions
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!e.IsMultipleOf(4))
                return;

            if (_mutex.WaitOne())
            {
                try
                {
                    _effectiveArea.Clear();
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }

            }

            if (Game1.activeClickableMenu != null || Game1.eventUp)
                return;

            UpdateEffectiveArea();
        }

        private void OnRendered(object sender, RenderedEventArgs e)
        {
            if (_mutex.WaitOne(0))
            {
                try
                {
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
        #endregion


        #region Logic
        private void UpdateEffectiveArea()
        {
            int[][] arrayToUse;
            List<StardewValley.Object> similarObjects;

            // Junimo Hut is handled differently, because it is a building
            if (Game1.currentLocation is BuildableGameLocation buildableLocation)
            {
                Building building = buildableLocation.getBuildingAt(Game1.currentCursorTile);

                if (building is JunimoHut)
                {
                    arrayToUse = GetDistanceArray(ObjectsWithDistance.JunimoHut);
                    foreach (var nextBuilding in buildableLocation.buildings)
                    {
                        if (nextBuilding is JunimoHut nextHut)
                        {
                            ParseConfigToHighlightedArea(arrayToUse, nextHut.tileX.Value + 1, nextHut.tileY.Value + 1);
                        }
                    }
                }
            }

            // Every other item is here
            if (Game1.player.CurrentItem != null)
            {
                string itemName = Game1.player.CurrentItem.Name;

                if (itemName.IndexOf("arecrow", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    arrayToUse = itemName.Contains("eluxe") ? GetDistanceArray(ObjectsWithDistance.DeluxeScarecrow) : GetDistanceArray(ObjectsWithDistance.Scarecrow);
                    ParseConfigToHighlightedArea(arrayToUse, TileUnderMouseX, TileUnderMouseY);

                    similarObjects = GetSimilarObjectsInLocation("arecrow");
                    foreach (StardewValley.Object next in similarObjects)
                    {
                        arrayToUse = next.Name.IndexOf("eluxe", StringComparison.OrdinalIgnoreCase) >= 0 ? GetDistanceArray(ObjectsWithDistance.DeluxeScarecrow) : GetDistanceArray(ObjectsWithDistance.Scarecrow);
                        ParseConfigToHighlightedArea(arrayToUse, (int)next.TileLocation.X, (int)next.TileLocation.Y);
                    }
                }
                else if (itemName.IndexOf("sprinkler", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    arrayToUse = itemName.IndexOf("iridium", StringComparison.OrdinalIgnoreCase) >= 0 ? GetDistanceArray(ObjectsWithDistance.IridiumSprinkler) :
                        itemName.IndexOf("quality", StringComparison.OrdinalIgnoreCase) >= 0 ? GetDistanceArray(ObjectsWithDistance.QualitySprinkler) :
                        itemName.IndexOf("prismatic", StringComparison.OrdinalIgnoreCase) >= 0 ? GetDistanceArray(ObjectsWithDistance.PrismaticSprinkler) :
                            GetDistanceArray(ObjectsWithDistance.Sprinkler);

                    ParseConfigToHighlightedArea(arrayToUse, TileUnderMouseX, TileUnderMouseY);

                    similarObjects = GetSimilarObjectsInLocation("sprinkler");
                    foreach (StardewValley.Object next in similarObjects)
                    {
                        arrayToUse = next.name.IndexOf("iridium", StringComparison.OrdinalIgnoreCase) >= 0 ? GetDistanceArray(ObjectsWithDistance.IridiumSprinkler) :
                        next.name.IndexOf("quality", StringComparison.OrdinalIgnoreCase) >= 0 ? GetDistanceArray(ObjectsWithDistance.QualitySprinkler) :
                        next.name.IndexOf("prismatic", StringComparison.OrdinalIgnoreCase) >= 0 ? GetDistanceArray(ObjectsWithDistance.PrismaticSprinkler) :
                            GetDistanceArray(ObjectsWithDistance.Sprinkler);

                        ParseConfigToHighlightedArea(arrayToUse, (int)next.TileLocation.X, (int)next.TileLocation.Y);
                    }
                }
                else if (itemName.IndexOf("bee house", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    arrayToUse = GetDistanceArray(ObjectsWithDistance.Beehouse);
                    ParseConfigToHighlightedArea(arrayToUse, TileUnderMouseX, TileUnderMouseY);
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

        private List<StardewValley.Object> GetSimilarObjectsInLocation(string nameContains)
        {
            List<StardewValley.Object> result = new List<StardewValley.Object>();

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

        #region Distance map
        private enum ObjectsWithDistance
        {
            JunimoHut,
            Beehouse,
            Scarecrow,
            DeluxeScarecrow,
            Sprinkler,
            QualitySprinkler,
            IridiumSprinkler,
            PrismaticSprinkler
        }

        private int[][] GetDistanceArray(ObjectsWithDistance type)
        {
            switch (type)
            {
                case ObjectsWithDistance.JunimoHut:
                    return GetCircularMask(100, maxRadius: 8);
                case ObjectsWithDistance.Beehouse:
                    return GetCircularMask(4.19, exceptionalDistance: 5, onlyClearExceptions: true);
                case ObjectsWithDistance.Scarecrow:
                    return GetCircularMask(8.99);
                case ObjectsWithDistance.DeluxeScarecrow:
                    return GetCircularMask(16.99);
                case ObjectsWithDistance.Sprinkler:
                    return GetCircularMask(1);
                case ObjectsWithDistance.QualitySprinkler:
                    return GetCircularMask(1.5);
                case ObjectsWithDistance.IridiumSprinkler:
                    return GetCircularMask(2.89);
                case ObjectsWithDistance.PrismaticSprinkler:
                    return GetCircularMask(3.69, exceptionalDistance: Math.Sqrt(18), onlyClearExceptions: false);
                default:
                    return null;
            }
        }

        private static int[][] GetCircularMask(double maxDistance, double? exceptionalDistance = null, bool? onlyClearExceptions = null, int? maxRadius = null)
        {
            int radius = Math.Max((int)Math.Ceiling(maxDistance), exceptionalDistance.HasValue ? (int)Math.Ceiling(exceptionalDistance.Value) : 0);
            radius = Math.Min(radius, maxRadius.HasValue ? maxRadius.Value : radius);
            int size = 2 * radius + 1;

            int[][] result = new int[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = new int[size];
                for (int j = 0; j < size; j++)
                {
                    double distance = GetDistance(i, j, radius);
                    int val = (IsInDistance(maxDistance, distance)
                        || (IsDistanceDirectionOK(i, j, radius, onlyClearExceptions) && IsExceptionalDistanceOK(exceptionalDistance, distance)))
                        ? 1 : 0;
                    result[i][j] = val;
                }
            }
            return result;
        }

        private static bool IsDistanceDirectionOK(int i, int j, int radius, bool? onlyClearExceptions)
        {
            return onlyClearExceptions.HasValue && onlyClearExceptions.Value ? (radius - j) == 0 || (radius - i) == 0 : true;
        }

        private static bool IsExceptionalDistanceOK(double? exceptionalDistance, double distance)
        {
            return exceptionalDistance.HasValue && exceptionalDistance.Value == distance;
        }

        private static bool IsInDistance(double maxDistance, double distance)
        {
            return distance <= maxDistance;
        }

        private static double GetDistance(int i, int j, int radius)
        {
            return Math.Sqrt((radius - i) * (radius - i) + (radius - j) * (radius - j));
        }
        #endregion
        #endregion
    }
}
