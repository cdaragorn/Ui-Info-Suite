using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UIInfoSuite.Extensions;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using StardewConfigFramework;
using System.IO;
using StardewModdingAPI;

namespace UIInfoSuite.UIElements {
	class ShowItemHoverInformation: IDisposable {

		private readonly Dictionary<String, List<int>> _prunedRequiredBundles = new Dictionary<string, List<int>>();
		private readonly ClickableTextureComponent _bundleIcon =
				new ClickableTextureComponent(
						"",
						new Rectangle(0, 0, Game1.tileSize, Game1.tileSize),
						"",
						Game1.content.LoadString("Strings\\UI:GameMenu_JunimoNote_Hover", new object[0]),
						Game1.mouseCursors,
						new Rectangle(331, 374, 15, 14),
						Game1.pixelZoom);

		private Item _hoverItem;
		private CommunityCenter _communityCenter;
		private Dictionary<String, String> _bundleData;
		private readonly ModOptionToggle _showItemHoverInformation;

		Dictionary<int, string> fishData = Game1.content.Load<Dictionary<int, string>>(Path.Combine("Data", "Fish.xnb"));
		Dictionary<string, string> locationData = Game1.content.Load<Dictionary<string, string>>(Path.Combine("Data", "Locations.xnb"));
		List<string> cropData = Game1.content.Load<Dictionary<int, string>>(Path.Combine("Data", "Crops.xnb")).Values.ToList();
		Dictionary<int, string> treeData = Game1.content.Load<Dictionary<int, string>>(Path.Combine("Data", "fruitTrees.xnb"));
		Dictionary<string, string> bundleData = Game1.content.Load<Dictionary<string, string>>(Path.Combine("Data", "Bundles.xnb"));

		List<int> springForage = new List<int> { 16, 18, 20, 22, 399, 257, 404, 296 };
		List<int> summerForage = new List<int> { 396, 402, 420, 259 };
		List<int> fallForage = new List<int> { 406, 408, 410, 281, 404, 420 };
		List<int> winterForage = new List<int> { 412, 414, 416, 418, 283 };

		Components hover;

		public ShowItemHoverInformation(ModOptions modOptions, IModHelper helper) {
			hover = new Components(helper);

			_showItemHoverInformation = modOptions.GetOptionWithIdentifier<ModOptionToggle>(OptionKeys.ShowExtraItemInformation) ?? new ModOptionToggle(OptionKeys.ShowExtraItemInformation, "Show Item hover information");
			_showItemHoverInformation.ValueChanged += ToggleOption;
			modOptions.AddModOption(_showItemHoverInformation);

			ToggleOption(_showItemHoverInformation.identifier, _showItemHoverInformation.IsOn);
		}

		public void ToggleOption(string identifier, bool showItemHoverInformation) {
			if (identifier != OptionKeys.ShowExtraItemInformation)
				return;

			GraphicsEvents.OnPostRenderEvent -= DrawAdvancedTooltip;
			PlayerEvents.InventoryChanged -= PopulateRequiredBundles;
			GraphicsEvents.OnPreRenderEvent -= GetHoverItem;

			if (showItemHoverInformation) {
				_communityCenter = Game1.getLocationFromName("CommunityCenter") as CommunityCenter;
				_bundleData = Game1.content.Load<Dictionary<String, String>>("Data\\Bundles");
				PopulateRequiredBundles(null, null);
				PlayerEvents.InventoryChanged += PopulateRequiredBundles;
				GraphicsEvents.OnPostRenderEvent += DrawAdvancedTooltip;
				GraphicsEvents.OnPreRenderEvent += GetHoverItem;
			}
		}

		public void Dispose() {
			ToggleOption(OptionKeys.ShowExtraItemInformation, false);
		}

		private void GetHoverItem(object sender, EventArgs e) {
			_hoverItem = Tools.GetHoveredItem();
		}

		private void removeDefaultTooltip() {
			// Not currently working

			// Remove hovers from toolbar
			for (int j = 0; j < Game1.onScreenMenus.Count; j++) {
				if (Game1.onScreenMenus[j] is Toolbar) {
					var menu = Game1.onScreenMenus[j] as Toolbar;
					typeof(Toolbar).GetField("hoverItem", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(menu, null);
				}
			}

			// Remove hovers from inventory
			if (Game1.activeClickableMenu is GameMenu) {

				// Get pages from GameMenu            
				var pages = (List<IClickableMenu>) typeof(GameMenu).GetField("pages", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Game1.activeClickableMenu);

				// Overwrite Inventory Menu
				for (int i = 0; i < pages.Count; i++) {
					if (pages[i] is InventoryPage) {
						var inventoryPage = (InventoryPage) pages[i];
						typeof(InventoryPage).GetField("hoverText", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(inventoryPage, "");
					}
				}
			}

			// Remove hovers from chests and shipping bin
			if (Game1.activeClickableMenu is ItemGrabMenu) {
				var itemGrabMenu = (ItemGrabMenu) Game1.activeClickableMenu;
				itemGrabMenu.hoveredItem = null;
			}
		}

		private void PopulateRequiredBundles(object sender, EventArgsInventoryChanged e) {
			_prunedRequiredBundles.Clear();
			foreach (var bundle in _bundleData) {
				String[] bundleRoomInfo = bundle.Key.Split('/');
				String bundleRoom = bundleRoomInfo[0];
				int roomNum;

				switch (bundleRoom) {
				case "Pantry": roomNum = 0; break;
				case "Crafts Room": roomNum = 1; break;
				case "Fish Tank": roomNum = 2; break;
				case "Boiler Room": roomNum = 3; break;
				case "Vault": roomNum = 4; break;
				case "Bulletin Board": roomNum = 5; break;
				default: continue;
				}

				if (_communityCenter.shouldNoteAppearInArea(roomNum)) {
					int bundleNumber = bundleRoomInfo[1].SafeParseInt32();
					string[] bundleInfo = bundle.Value.Split('/');
					string bundleName = bundleInfo[0];
					string[] bundleValues = bundleInfo[2].Split(' ');
					List<int> source = new List<int>();

					for (int i = 0; i < bundleValues.Length; i += 3) {
						int bundleValue = bundleValues[i].SafeParseInt32();
						if (bundleValue != -1 &&
								!_communityCenter.bundles[bundleNumber][i / 3]) {
							source.Add(bundleValue);
						}
					}

					if (source.Count > 0)
						_prunedRequiredBundles.Add(bundleName, source);
				}
			}
		}

		private Tuple<Dictionary<string, bool>, Dictionary<string, bool>, Dictionary<string, bool>, string> GetSeasonsLocationsWeatherAndTimes(StardewValley.Object hoveredObject) {

			// Spring, Summer, Fall, Winter
			Dictionary<string, bool> seasons = new Dictionary<string, bool>() {
				{ "spring", false },
				{ "summer", false },
				{ "fall", false },
				{ "winter", false }
			};

			// Rainy, Sunny
			Dictionary<string, bool> weather = new Dictionary<string, bool>() {
				{ "sunny", false },
				{ "rainy", false }
			};

			// Locations
			Dictionary<string, bool> locations = new Dictionary<string, bool>()
			{
				{ "UndergroundMine", false }, { "Desert", false }, { "ForestRiver", false },
				{ "Town", false }, { "Mountain", false }, { "Beach", false },
				{ "Woods", false }, { "Sewer", false }, { "BugLand", false },
				{ "WitchSwamp", false }, { "Trap", false }, { "ForestPond", false },
			};

			string times = "";

			if (hoveredObject != null && fishData.ContainsKey(hoveredObject.ParentSheetIndex) && !hoveredObject.Name.Contains("Algae") && !hoveredObject.Name.Contains("Seaweed")) {
				// draw the seasons icons 
				var data = fishData[hoveredObject.ParentSheetIndex].Split('/');
				if (data[1] != "trap") {

					var weatherData = data[7].Split(' ');
					if (!weatherData.Contains("both")) { // if all weather don't draw any
						if (weatherData.Contains("rainy")) {
							weather["rainy"] = true;
						} else {
							weather["sunny"] = true;
						}
					} else {
						weather["rainy"] = true;
						weather["sunny"] = true;
					}

					var timesData = data[5].Split(' ');

					if (!(timesData[0] == "600" && timesData[1] == "2600")) {
						for (int i = 0; i < timesData.Length; i++) {
							int time = (int.Parse(timesData[i]) / 100);
							times += time - (time > 12 ? 12 * (int) (time / 12) : 0);
							if (time >= 12 && time < 24)
								times += "pm";
							else
								times += "am";

							if (i % 2 == 1 && i != timesData.Length - 1) {
								times += "\n";
							} else if (i % 2 == 0) {
								times += "-";
							}
						}
					} else {
						times = "Any Time";
					}

					// Seasons data is in Locations.xnb
					foreach (string key in locationData.Keys) {
						string[] locationDataArray = locationData[key].Split('/');

						string[] springData = locationDataArray[4].Split(' ');
						string[] summerData = locationDataArray[5].Split(' ');
						string[] fallData = locationDataArray[6].Split(' ');
						string[] winterData = locationDataArray[7].Split(' ');

						if (springData.Contains($"{hoveredObject.ParentSheetIndex}")) {
							seasons["spring"] = true;
							locations[key + "River"] = true;

							if (key == "Forest") { // forest has 2 seperate bodies of water
								if (springData[Array.IndexOf(springData, $"{hoveredObject.ParentSheetIndex}") + 1] == "-1") {
									locations[key + "Pond"] = true;
								}

							} else
								locations[key] = true;
						}

						if (summerData.Contains($"{hoveredObject.ParentSheetIndex}")) {
							seasons["summer"] = true;
							locations[key + "River"] = true;

							if (key == "Forest") { // forest has 2 seperate bodies of water
								if (summerData[Array.IndexOf(summerData, $"{hoveredObject.ParentSheetIndex}") + 1] == "-1") {
									locations[key + "Pond"] = true;
								}

							} else
								locations[key] = true;
						}

						if (fallData.Contains($"{hoveredObject.ParentSheetIndex}")) {
							seasons["fall"] = true;
							locations[key + "River"] = true;

							if (key == "Forest") { // forest has 2 seperate bodies of water
								if (fallData[Array.IndexOf(fallData, $"{hoveredObject.ParentSheetIndex}") + 1] == "-1") {
									locations[key + "Pond"] = true;
								}

							} else
								locations[key] = true;
						}

						if (winterData.Contains($"{hoveredObject.ParentSheetIndex}")) {
							seasons["winter"] = true;
							locations[key + "River"] = true;

							if (key == "Forest") { // forest has 2 seperate bodies of water
								if (winterData[Array.IndexOf(winterData, $"{hoveredObject.ParentSheetIndex}") + 1] == "-1") {
									locations[key + "Pond"] = true;
								}

							} else
								locations[key] = true;
						}
					}

				} else if (data[1] == "trap") {
					locations["Trap"] = true;
				}
			} else if (hoveredObject != null && treeData.Values.ToList().Exists(x => x.Split('/')[2] == $"{hoveredObject.ParentSheetIndex}")) {

				var data = treeData.Values.ToList().Find(x => x.Split('/')[2] == $"{hoveredObject.ParentSheetIndex}").Split('/');

				var seasonsData = data[1].Split(' ');
				if (seasonsData.Count() > 0) {
					if (seasonsData.Contains("spring"))
						seasons["spring"] = true;

					if (seasonsData.Contains("summer"))
						seasons["summer"] = true;

					if (seasonsData.Contains("fall"))
						seasons["fall"] = true;

					if (seasonsData.Contains("winter"))
						seasons["winter"] = true;
				}

			} else if (hoveredObject != null && cropData.Exists(x => { return x.Split('/')[3] == $"{hoveredObject.ParentSheetIndex}"; })) {

				var data = cropData.Find(x => { return x.Split('/')[3] == $"{hoveredObject.ParentSheetIndex}"; }).Split('/');

				var seasonsData = data[1].Split(' ');
				if (seasonsData.Count() > 0) {

					if (seasonsData.Contains("spring"))
						seasons["spring"] = true;

					if (seasonsData.Contains("summer"))
						seasons["summer"] = true;

					if (seasonsData.Contains("fall"))
						seasons["fall"] = true;

					if (seasonsData.Contains("winter"))
						seasons["winter"] = true;
				}
			} else if (hoveredObject != null
				&& ((fallForage.Contains(hoveredObject.ParentSheetIndex))
				|| (springForage.Contains(hoveredObject.ParentSheetIndex))
				|| (winterForage.Contains(hoveredObject.ParentSheetIndex))
				|| (summerForage.Contains(hoveredObject.ParentSheetIndex))
				)) { // Foraged items

				if (springForage.Contains(hoveredObject.ParentSheetIndex))
					seasons["spring"] = true;

				if (summerForage.Contains(hoveredObject.ParentSheetIndex))
					seasons["summer"] = true;

				if (fallForage.Contains(hoveredObject.ParentSheetIndex))
					seasons["fall"] = true;

				if (winterForage.Contains(hoveredObject.ParentSheetIndex))
					seasons["winter"] = true;
			}

			return new Tuple<Dictionary<string, bool>, Dictionary<string, bool>, Dictionary<string, bool>, string>(seasons, locations, weather, times);

		}

		private void DrawAdvancedTooltip(object sender, EventArgs e) {
			if (_hoverItem == null)
				return;

			//removeDefaultTooltip();


			int padding = 2 * 5 * Game1.pixelZoom;
			int itemSpacing = 2 * Game1.pixelZoom;


			var hoverObject = _hoverItem as StardewValley.Object;

			hover.Reset(); // reset components

			// set minimum background width to bottom padding
			hover.Background.Height = Game1.pixelZoom - itemSpacing;
			hover.Background.Width = Game1.pixelZoom * 25;

			foreach (var requiredBundle in _prunedRequiredBundles) {
				if (requiredBundle.Value.Contains(_hoverItem.parentSheetIndex) &&
						!_hoverItem.Name.Contains("arecrow")) {
					hover.bundleName.hidden = false;
					hover.bundleName.text = requiredBundle.Key;
					hover.Background.Height += hover.bundleIcon.Height + itemSpacing;
					hover.ExtendBackgroundWidth(hover.bundleIcon.Width + itemSpacing + hover.bundleName.Width + padding, Game1.pixelZoom * 50);
					break;
				}
			}

			if (hover.bundleName.hidden)
				hover.Background.Height += padding / 2;

			int truePrice = Tools.GetTruePrice(_hoverItem);

			if (truePrice > 0) {
				hover.price.hidden = false;
				hover.price.text = $"{truePrice}";
				hover.Background.Height += hover.price.Height + itemSpacing;
				hover.ExtendBackgroundWidth(hover.currencyIcon.Width + itemSpacing + hover.price.Width + padding);

				if (_hoverItem.getStack() > 1) {
					hover.stackPrice.hidden = false;
					hover.stackPrice.text = $"{truePrice * _hoverItem.getStack()}";
					hover.Background.Height += hover.stackPrice.Height + itemSpacing;
					hover.ExtendBackgroundWidth(hover.currencyIcon.Width + itemSpacing + hover.stackPrice.Width + padding);
				} else {
					hover.Background.Height += itemSpacing; // not sure why non stacked items arent properly vertically padded.
				}
			} else {
				// If no price, object will not have any other info of interest to display
				return;
			}

			if (hoverObject == null) {
				// all other possible info needs to be type object
				return;
			}

			Tuple<Dictionary<string, bool>, Dictionary<string, bool>, Dictionary<string, bool>, string> timeInfo = GetSeasonsLocationsWeatherAndTimes(hoverObject);

			if (hoverObject.type == "Seeds") {

				if (hoverObject.Name != "Mixed Seeds" && hoverObject.Name != "Winter Seeds" && hoverObject.Name != "Summer Seeds" && hoverObject.Name != "Fall Seeds" && hoverObject.Name != "Spring Seeds") {
					var crop = new StardewValley.Object(new Debris(new Crop(_hoverItem.parentSheetIndex, 0, 0).indexOfHarvest, Game1.player.position, Game1.player.position).chunkType, 1);
					var cropPrice = crop.Price;

					timeInfo = GetSeasonsLocationsWeatherAndTimes(crop);

					hover.cropPrice.text = $"{cropPrice}";
					hover.cropPrice.hidden = false;
					hover.ExtendBackgroundWidth(hover.currencyIcon.Width + itemSpacing + hover.price.Width + itemSpacing + (int) hover.cropPrice.font.MeasureString(">").X + itemSpacing + hover.cropPrice.Width + padding);

					if (_hoverItem.getStack() > 1) {
						hover.cropStackPrice.text = $"{cropPrice * _hoverItem.getStack()}";
						hover.cropStackPrice.hidden = false;
						hover.ExtendBackgroundWidth(hover.currencyIcon.Width + itemSpacing + hover.stackPrice.Width + itemSpacing + (int) hover.cropStackPrice.font.MeasureString(">").X + itemSpacing + hover.cropStackPrice.Width + padding);

					}
				}
			}

			if (timeInfo.Item1.Values.Contains(true)) { // if at least one season
				int num = timeInfo.Item1.Where(x => { return x.Value; }).Count();

				if (timeInfo.Item1["spring"])
					hover.springIcon.hidden = false;
				if (timeInfo.Item1["summer"])
					hover.summerIcon.hidden = false;
				if (timeInfo.Item1["fall"])
					hover.fallIcon.hidden = false;
				if (timeInfo.Item1["winter"])
					hover.winterIcon.hidden = false;

				hover.Background.Height += hover.springIcon.Height + itemSpacing;
				hover.ExtendBackgroundWidth((hover.springIcon.Width + itemSpacing) * (num != 4 ? num : 2) + padding);
			}

			if (timeInfo.Item2.Values.Contains(true)) { // if at least one season
				int num = 0;
				// count manually, dictionary countains extra locations (i.e Temp, FishGame)


				// TODO Add custom icons by 4Slice
				// Catfish and pike are special exceptions with Secret Woods
				if (timeInfo.Item2["UndergroundMine"]) {
					hover.minesIcon.hidden = false;
					num++;
				}
				if (timeInfo.Item2["Desert"]) {
					hover.desertIcon.hidden = false;
					num++;
				}
				if (timeInfo.Item2["ForestRiver"]) {
					hover.forestRiverIcon.hidden = false;
					num++;
				}
				if (timeInfo.Item2["Town"]) {
					hover.townIcon.hidden = false;
					num++;
				}
				if (timeInfo.Item2["Mountain"]) {
					hover.mountainIcon.hidden = false;
					num++;
				}
				if (timeInfo.Item2["Beach"]) {
					hover.beachIcon.hidden = false;
					num++;
				}
				if (timeInfo.Item2["Woods"]) {
					hover.secretWoodsIcon.hidden = false;
					num++;
				}
				if (timeInfo.Item2["Sewer"]) {
					hover.sewersIcon.hidden = false;
					num++;
				}
				if (timeInfo.Item2["BugLand"]) {
					hover.bugLandIcon.hidden = false;
					num++;
				}
				if (timeInfo.Item2["WitchSwamp"]) {
					hover.witchSwampIcon.hidden = false;
					num++;
				}
				if (timeInfo.Item2["Trap"]) {
					hover.trapIcon.hidden = false;
					num++;
				}
				if (timeInfo.Item2["ForestPond"]) {
					hover.forestPondIcon.hidden = false;
					num++;
				}

				/*
				{ "UndergroundMine", false }, { "Desert", false }, { "ForestRiver", false },
				{ "Town", false }, { "Mountain", false }, { "Beach", false },
				{ "Woods", false }, { "Sewer", false }, { "BugLand", false },
				{ "WitchSwamp", false }, { "Trap", false }, { "ForestPond", false },
				 */

				hover.Background.Height += hover.springIcon.Height + itemSpacing;
				// todo decide max width
				hover.ExtendBackgroundWidth((hover.springIcon.Width + itemSpacing) * num + padding);
			}


			if (timeInfo.Item3.Values.Contains(true)) { // if at least one season
				int num = timeInfo.Item3.Where(x => { return x.Value; }).Count();

				if (timeInfo.Item3["rainy"])
					hover.rainyIcon.hidden = false;
				if (timeInfo.Item3["sunny"])
					hover.sunnyIcon.hidden = false;

				hover.Background.Height += hover.rainyIcon.Height + itemSpacing;
				hover.ExtendBackgroundWidth((hover.rainyIcon.Width + itemSpacing) * num + padding);
			}

			if (timeInfo.Item4 != "") {
				hover.fishTimes.hidden = false;
				hover.fishTimes.text = timeInfo.Item4;

				hover.Background.Height += hover.fishTimes.Height + itemSpacing;
				hover.ExtendBackgroundWidth(hover.fishTimes.Width + padding);
			}

			// place window by mouse
			hover.Background.Y = Game1.getMouseY() + 12 * Game1.pixelZoom;
			hover.Background.X = Game1.getMouseX() + 6 * Game1.pixelZoom - hover.Background.Width;

			// 70 * pixelZoom is my guess for the maximum width of the default tooltip
			// if modify original tooltip to add a global Rectangle of the default tooltip, can get exact dimensions
			// Would have to revert to removing the original tooltip again.

			// ensure it doesnt go off screen
			if (hover.Background.Bottom > Game1.viewport.Height) { // bottom
				hover.Background.Y = Game1.viewport.Height - hover.Background.Height;
				hover.Background.X = Game1.getMouseX() - 4 * Game1.pixelZoom - hover.Background.Width;

			}

			if (hover.Background.X < 0) { // bottom left
				hover.Background.X = Game1.getMouseX() + 8 * Game1.pixelZoom + 70 * Game1.pixelZoom;
			} else if (hover.Background.Right + 70 * Game1.pixelZoom + 8 * Game1.pixelZoom > Game1.viewport.Width) { // bottom right
				hover.Background.X = Game1.viewport.Width - hover.Background.Width - 8 * Game1.pixelZoom - 70 * Game1.pixelZoom;
			}

			// Draw Hover Info
			IClickableMenu.drawTextureBox(Game1.spriteBatch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), hover.Background.X, hover.Background.Y, hover.Background.Width, hover.Background.Height, Color.White, 1f, true);

			// keep track of next item location as we go down
			int currentLocationY = hover.Background.Y;
			int paddedLocationX = hover.Background.X + padding / 2;

			if (!hover.bundleName.hidden) {

				int amountOfSectionsWithoutAlpha = 10;
				int amountOfSections = 36;
				int sectionWidth = (hover.Background.Width - hover.bundleIcon.Width) / amountOfSections;

				// Draw fade
				for (int i = 0; i < amountOfSections; i++) {
					float sectionAlpha;
					if (i < amountOfSectionsWithoutAlpha) {
						sectionAlpha = 0.92f;
					} else {
						sectionAlpha = 0.92f - (i - amountOfSectionsWithoutAlpha) * (1f / (amountOfSections - amountOfSectionsWithoutAlpha));
					}
					Game1.spriteBatch.Draw(Game1.staminaRect, new Rectangle(hover.Background.X + SourceRects.bundleIcon.Width * Game1.pixelZoom + (sectionWidth * i), currentLocationY, sectionWidth, SourceRects.bundleIcon.Height * Game1.pixelZoom), Color.Crimson * sectionAlpha);
				}

				// Draw Icon
				hover.bundleIcon.draw(Game1.spriteBatch, new Vector2(hover.Background.X, currentLocationY));

				// Draw Text
				hover.bundleName.draw(Game1.spriteBatch, new Vector2(hover.Background.X + hover.bundleIcon.Width + Game1.pixelZoom, currentLocationY + 3 * Game1.pixelZoom));

				currentLocationY += hover.bundleIcon.Height + itemSpacing;
			} else {
				currentLocationY += padding / 2;
			}

			if (!hover.price.hidden) {
				hover.currencyIcon.draw(Game1.spriteBatch, new Vector2(paddedLocationX, currentLocationY));
				hover.price.draw(Game1.spriteBatch, new Vector2(paddedLocationX + hover.currencyIcon.Width + itemSpacing, currentLocationY));

				if (!hover.cropPrice.hidden) {
					Game1.spriteBatch.DrawString(hover.price.font, ">", new Vector2(paddedLocationX + hover.currencyIcon.Width + itemSpacing + hover.price.Width + itemSpacing, currentLocationY), Game1.textColor);
					hover.cropPrice.draw(Game1.spriteBatch, new Vector2(paddedLocationX + hover.currencyIcon.Width + itemSpacing + hover.price.Width + itemSpacing + (int) hover.cropPrice.font.MeasureString(">").X + itemSpacing, currentLocationY));
				}

				currentLocationY += hover.currencyIcon.Height + itemSpacing;
			}

			if (!hover.stackPrice.hidden) {
				hover.currencyIcon.draw(Game1.spriteBatch, new Vector2(paddedLocationX - Game1.pixelZoom, currentLocationY - Game1.pixelZoom));
				hover.currencyIcon.draw(Game1.spriteBatch, new Vector2(paddedLocationX + Game1.pixelZoom, currentLocationY + Game1.pixelZoom));

				hover.stackPrice.draw(Game1.spriteBatch, new Vector2(paddedLocationX + hover.currencyIcon.Width + itemSpacing, currentLocationY));

				if (!hover.cropStackPrice.hidden) {
					Game1.spriteBatch.DrawString(hover.stackPrice.font, ">", new Vector2(paddedLocationX + hover.currencyIcon.Width + itemSpacing + hover.stackPrice.Width + itemSpacing, currentLocationY), Game1.textColor);
					hover.cropStackPrice.draw(Game1.spriteBatch, new Vector2(paddedLocationX + hover.currencyIcon.Width + itemSpacing + hover.stackPrice.Width + itemSpacing + (int) hover.cropStackPrice.font.MeasureString(">").X + itemSpacing, currentLocationY));
				}

				currentLocationY += hover.currencyIcon.Height + itemSpacing;
			}

			// Draw Season Icons
			if (!hover.springIcon.hidden && !hover.summerIcon.hidden && !hover.fallIcon.hidden && !hover.winterIcon.hidden) {
				// draw a custom icon because all 4 is too long
				int curX = paddedLocationX;

				Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(curX, currentLocationY), new Rectangle(SourceRects.springIcon.X, SourceRects.springIcon.Y, SourceRects.springIcon.Width / 2, SourceRects.springIcon.Height), Color.White, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.88f);
				curX += (SourceRects.springIcon.Width * Game1.pixelZoom) / 2 + Game1.pixelZoom;
				Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(curX, currentLocationY), new Rectangle(SourceRects.summerIcon.X + SourceRects.springIcon.Width / 2 - 1, SourceRects.summerIcon.Y, SourceRects.springIcon.Width / 2, SourceRects.springIcon.Height), Color.White, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.88f);
				curX += (SourceRects.springIcon.Width * Game1.pixelZoom) / 2 + Game1.pixelZoom;
				Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(curX, currentLocationY), new Rectangle(SourceRects.fallIcon.X + SourceRects.springIcon.Width / 2 - 1, SourceRects.fallIcon.Y, SourceRects.springIcon.Width / 2, SourceRects.springIcon.Height), Color.White, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.88f);
				curX += (SourceRects.springIcon.Width * Game1.pixelZoom) / 2 + Game1.pixelZoom;
				Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(curX, currentLocationY), new Rectangle(SourceRects.winterIcon.X + SourceRects.springIcon.Width / 2 - 1, SourceRects.winterIcon.Y, SourceRects.springIcon.Width / 2, SourceRects.springIcon.Height), Color.White, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.88f);

				currentLocationY += hover.springIcon.Height + itemSpacing;

			} else if (!hover.springIcon.hidden || !hover.summerIcon.hidden || !hover.fallIcon.hidden || !hover.winterIcon.hidden) {
				int curX = paddedLocationX;

				if (!hover.springIcon.hidden) {
					hover.springIcon.draw(Game1.spriteBatch, new Vector2(curX, currentLocationY));
					curX += hover.springIcon.Width + itemSpacing;
				}

				if (!hover.summerIcon.hidden) {
					hover.summerIcon.draw(Game1.spriteBatch, new Vector2(curX, currentLocationY));
					curX += hover.springIcon.Width + itemSpacing;
				}

				if (!hover.fallIcon.hidden) {
					hover.fallIcon.draw(Game1.spriteBatch, new Vector2(curX, currentLocationY));
					curX += hover.springIcon.Width + itemSpacing;
				}

				if (!hover.winterIcon.hidden) {
					hover.winterIcon.draw(Game1.spriteBatch, new Vector2(curX, currentLocationY));
					curX += hover.springIcon.Width + itemSpacing;
				}

				currentLocationY += hover.springIcon.Height + itemSpacing;
			}

			// TODO Draw Location Icon
			if (!hover.minesIcon.hidden || !hover.desertIcon.hidden || !hover.forestRiverIcon.hidden
				|| !hover.townIcon.hidden || !hover.mountainIcon.hidden || !hover.beachIcon.hidden
				|| !hover.secretWoodsIcon.hidden || !hover.sewersIcon.hidden || !hover.bugLandIcon.hidden
				|| !hover.witchSwampIcon.hidden || !hover.trapIcon.hidden || !hover.forestPondIcon.hidden) {
				int curX = paddedLocationX;

				if (!hover.minesIcon.hidden) {
					hover.minesIcon.draw(Game1.spriteBatch, new Vector2(curX, currentLocationY));
					curX += hover.townIcon.Width + itemSpacing;
				}

				if (!hover.desertIcon.hidden) {
					hover.desertIcon.draw(Game1.spriteBatch, new Vector2(curX, currentLocationY));
					curX += hover.townIcon.Width + itemSpacing;
				}

				if (!hover.forestRiverIcon.hidden) {
					hover.forestRiverIcon.draw(Game1.spriteBatch, new Vector2(curX, currentLocationY));
					curX += hover.townIcon.Width + itemSpacing;
				}

				if (!hover.townIcon.hidden) {
					hover.townIcon.draw(Game1.spriteBatch, new Vector2(curX, currentLocationY));
					curX += hover.townIcon.Width + itemSpacing;
				}

				if (!hover.mountainIcon.hidden) {
					hover.mountainIcon.draw(Game1.spriteBatch, new Vector2(curX, currentLocationY));
					curX += hover.townIcon.Width + itemSpacing;
				}

				if (!hover.beachIcon.hidden) {
					hover.beachIcon.draw(Game1.spriteBatch, new Vector2(curX, currentLocationY));
					curX += hover.townIcon.Width + itemSpacing;
				}

				if (!hover.secretWoodsIcon.hidden) {
					hover.secretWoodsIcon.draw(Game1.spriteBatch, new Vector2(curX, currentLocationY));
					curX += hover.townIcon.Width + itemSpacing;
				}

				if (!hover.sewersIcon.hidden) {
					hover.sewersIcon.draw(Game1.spriteBatch, new Vector2(curX, currentLocationY));
					curX += hover.townIcon.Width + itemSpacing;
				}

				if (!hover.bugLandIcon.hidden) {
					hover.bugLandIcon.draw(Game1.spriteBatch, new Vector2(curX, currentLocationY));
					curX += hover.townIcon.Width + itemSpacing;
				}

				if (!hover.witchSwampIcon.hidden) {
					hover.witchSwampIcon.draw(Game1.spriteBatch, new Vector2(curX, currentLocationY));
					curX += hover.townIcon.Width + itemSpacing;
				}

				if (!hover.trapIcon.hidden) {
					hover.trapIcon.draw(Game1.spriteBatch, new Vector2(curX, currentLocationY));
					curX += hover.townIcon.Width + itemSpacing;
				}

				if (!hover.forestPondIcon.hidden) {
					hover.forestPondIcon.draw(Game1.spriteBatch, new Vector2(curX, currentLocationY));
					curX += hover.townIcon.Width + itemSpacing;
				}

				currentLocationY += hover.townIcon.Height + itemSpacing;
			}

			// Draw weather Icons
			if (!hover.rainyIcon.hidden || !hover.sunnyIcon.hidden) {

				int curX = paddedLocationX;

				if (!hover.rainyIcon.hidden) {
					hover.rainyIcon.draw(Game1.spriteBatch, new Vector2(curX, currentLocationY));
					curX += hover.rainyIcon.Width + itemSpacing;
				}

				if (!hover.sunnyIcon.hidden) {
					hover.sunnyIcon.draw(Game1.spriteBatch, new Vector2(curX, currentLocationY));
					curX += hover.rainyIcon.Width + itemSpacing;
				}

				currentLocationY += hover.rainyIcon.Height + itemSpacing;
			}

			// Draw Fish Times
			if (!hover.fishTimes.hidden) {
				hover.fishTimes.draw(Game1.spriteBatch, new Vector2(paddedLocationX, currentLocationY));

				currentLocationY += hover.fishTimes.Height + itemSpacing;
			}
		}

		static Rectangle defaultTooltip = new Rectangle();

		// -------------------------------------------- COPIED METHODS FROM GAME ----------------------------------------------

		public static void drawToolTip(SpriteBatch b, string hoverText, string hoverTitle, Item hoveredItem, bool heldItem = false, int healAmountToDisplay = -1, int currencySymbol = 0, int extraItemToShowIndex = -1, int extraItemToShowAmount = -1, CraftingRecipe craftingIngredients = null, int moneyAmountToShowAtBottom = -1) {
			bool flag = hoveredItem != null && hoveredItem is StardewValley.Object && (hoveredItem as StardewValley.Object).edibility != -300;


			drawHoverText(b, hoverText, Game1.smallFont, heldItem ? (Game1.tileSize / 2 + 8) : 0, heldItem ? (Game1.tileSize / 2 + 8) : 0, moneyAmountToShowAtBottom, hoverTitle, flag ? (hoveredItem as StardewValley.Object).edibility : -1, (flag && Game1.objectInformation[(hoveredItem as StardewValley.Object).parentSheetIndex].Split(new char[] {
								'/'
						}).Length > 7) ? Game1.objectInformation[(hoveredItem as StardewValley.Object).parentSheetIndex].Split(new char[] {
								'/'
						})[7].Split(new char[] {
								' '
						}) : null, hoveredItem, currencySymbol, extraItemToShowIndex, extraItemToShowAmount, -1, -1, 1f, craftingIngredients);
		}

		// slightly modified
		public static void drawHoverText(SpriteBatch b, string text, SpriteFont font, int xOffset = 0, int yOffset = 0, int moneyAmountToDisplayAtBottom = -1, string boldTitleText = null, int healAmountToDisplay = -1, string[] buffIconsToDisplay = null, Item hoveredItem = null, int currencySymbol = 0, int extraItemToShowIndex = -1, int extraItemToShowAmount = -1, int overrideX = -1, int overrideY = -1, float alpha = 1f, CraftingRecipe craftingIngredients = null) {
			if (text == null || text.Length == 0) {
				return;
			}
			if (boldTitleText != null && boldTitleText.Length == 0) {
				boldTitleText = null;
			}
			int num = 20;
			int num2 = Math.Max((healAmountToDisplay != -1) ? ((int) font.MeasureString(healAmountToDisplay + "+ Energy" + Game1.tileSize / 2).X) : 0, Math.Max((int) font.MeasureString(text).X, (boldTitleText != null) ? ((int) Game1.dialogueFont.MeasureString(boldTitleText).X) : 0)) + Game1.tileSize / 2;
			int num3 = Math.Max(num * 3, (int) font.MeasureString(text).Y + Game1.tileSize / 2 + (int) ((moneyAmountToDisplayAtBottom > -1) ? (font.MeasureString(string.Concat(moneyAmountToDisplayAtBottom)).Y + 4f) : 0f) + (int) ((boldTitleText != null) ? (Game1.dialogueFont.MeasureString(boldTitleText).Y + (float) (Game1.tileSize / 4)) : 0f) + ((healAmountToDisplay != -1) ? 38 : 0));
			if (extraItemToShowIndex != -1) {
				string[] array = Game1.objectInformation[extraItemToShowIndex].Split(new char[] {
										'/'
								});
				string text2 = array[0];
				if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.en) {
					text2 = array[array.Length - 1];
				}
				string text3 = Game1.content.LoadString("Strings\\UI:ItemHover_Requirements", new object[] {
										extraItemToShowAmount,
										text2
								});
				int num4 = Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, extraItemToShowIndex, 16, 16).Width * 2 * Game1.pixelZoom;
				num2 = Math.Max(num2, num4 + (int) font.MeasureString(text3).X);
			}
			if (buffIconsToDisplay != null) {
				for (int i = 0; i < buffIconsToDisplay.Length; i++) {
					if (!buffIconsToDisplay[i].Equals("0")) {
						num3 += 34;
					}
				}
				num3 += 4;
			}
			string text4 = null;
			if (hoveredItem != null) {
				num3 += (Game1.tileSize + 4) * hoveredItem.attachmentSlots();
				text4 = hoveredItem.getCategoryName();
				if (text4.Length > 0) {
					num2 = Math.Max(num2, (int) font.MeasureString(text4).X + Game1.tileSize / 2);
					num3 += (int) font.MeasureString("T").Y;
				}
				int num5 = 9999;
				int num6 = 15 * Game1.pixelZoom + Game1.tileSize / 2;
				if (hoveredItem is MeleeWeapon) {
					num3 = Math.Max(num * 3, (int) ((boldTitleText != null) ? (Game1.dialogueFont.MeasureString(boldTitleText).Y + (float) (Game1.tileSize / 4)) : 0f) + Game1.tileSize / 2) + (int) font.MeasureString("T").Y + (int) ((moneyAmountToDisplayAtBottom > -1) ? (font.MeasureString(string.Concat(moneyAmountToDisplayAtBottom)).Y + 4f) : 0f);
					num3 += ((hoveredItem.Name == "Scythe") ? 0 : ((hoveredItem as MeleeWeapon).getNumberOfDescriptionCategories() * Game1.pixelZoom * 12));
					num3 += (int) font.MeasureString(Game1.parseText((hoveredItem as MeleeWeapon).description, Game1.smallFont, Game1.tileSize * 4 + Game1.tileSize / 4)).Y;
					num2 = (int) Math.Max((float) num2, Math.Max(font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_Damage", new object[] {
												num5,
												num5
										})).X + (float) num6, Math.Max(font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_Speed", new object[] {
												num5
										})).X + (float) num6, Math.Max(font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_DefenseBonus", new object[] {
												num5
										})).X + (float) num6, Math.Max(font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_CritChanceBonus", new object[] {
												num5
										})).X + (float) num6, Math.Max(font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_CritPowerBonus", new object[] {
												num5
										})).X + (float) num6, font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_Weight", new object[] {
												num5
										})).X + (float) num6))))));
				} else if (hoveredItem is Boots) {
					num3 -= (int) font.MeasureString(text).Y;
					num3 += (int) ((float) ((hoveredItem as Boots).getNumberOfDescriptionCategories() * Game1.pixelZoom * 12) + font.MeasureString(Game1.parseText((hoveredItem as Boots).description, Game1.smallFont, Game1.tileSize * 4 + Game1.tileSize / 4)).Y);
					num2 = (int) Math.Max((float) num2, Math.Max(font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_DefenseBonus", new object[] {
												num5
										})).X + (float) num6, font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_ImmunityBonus", new object[] {
												num5
										})).X + (float) num6));
				} else if (hoveredItem is StardewValley.Object && (hoveredItem as StardewValley.Object).edibility != -300) {
					if (healAmountToDisplay == -1) {
						num3 += (Game1.tileSize / 2 + Game1.pixelZoom * 2) * ((healAmountToDisplay > 0) ? 2 : 1);
					} else {
						num3 += Game1.tileSize / 2 + Game1.pixelZoom * 2;
					}
					healAmountToDisplay = (int) Math.Ceiling((double) (hoveredItem as StardewValley.Object).Edibility * 2.5) + (hoveredItem as StardewValley.Object).quality * (hoveredItem as StardewValley.Object).Edibility;
					num2 = (int) Math.Max((float) num2, Math.Max(font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_Energy", new object[] {
												num5
										})).X + (float) num6, font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_Health", new object[] {
												num5
										})).X + (float) num6));
				}
				if (buffIconsToDisplay != null) {
					for (int j = 0; j < buffIconsToDisplay.Length; j++) {
						if (!buffIconsToDisplay[j].Equals("0") && j <= 11) {
							num2 = (int) Math.Max((float) num2, font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_Buff" + j, new object[] {
																num5
														})).X + (float) num6);
						}
					}
				}
			}
			if (craftingIngredients != null) {
				num2 = Math.Max((int) Game1.dialogueFont.MeasureString(boldTitleText).X + Game1.pixelZoom * 3, Game1.tileSize * 6);
				num3 += craftingIngredients.getDescriptionHeight(num2 - Game1.pixelZoom * 2) + ((healAmountToDisplay == -1) ? (-Game1.tileSize / 2) : 0) + Game1.pixelZoom * 3;
			}
			if (hoveredItem is FishingRod && moneyAmountToDisplayAtBottom > -1) {
				num3 += (int) font.MeasureString("T").Y;
			}
			int num7 = Game1.getOldMouseX() + Game1.tileSize / 2 + xOffset;
			int num8 = Game1.getOldMouseY() + Game1.tileSize / 2 + yOffset;
			if (overrideX != -1) {
				num7 = overrideX;
			}
			if (overrideY != -1) {
				num8 = overrideY;
			}
			if (num7 + num2 > Utility.getSafeArea().Right) {
				num7 = Utility.getSafeArea().Right - num2;
				num8 += Game1.tileSize / 4;
			}
			if (num8 + num3 > Utility.getSafeArea().Bottom) {
				num7 += Game1.tileSize / 4;
				if (num7 + num2 > Utility.getSafeArea().Right) {
					num7 = Utility.getSafeArea().Right - num2;
				}
				num8 = Utility.getSafeArea().Bottom - num3;
			}

			// save the size
			defaultTooltip = new Rectangle(num7, num8, num2 + ((craftingIngredients != null) ? (Game1.tileSize / 3) : 0), num3);

			IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), num7, num8, num2 + ((craftingIngredients != null) ? (Game1.tileSize / 3) : 0), num3, Color.White * alpha, 1f, true);
			if (boldTitleText != null) {
				IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), num7, num8, num2 + ((craftingIngredients != null) ? (Game1.tileSize / 3) : 0), (int) Game1.dialogueFont.MeasureString(boldTitleText).Y + Game1.tileSize / 2 + (int) ((hoveredItem != null && text4.Length > 0) ? font.MeasureString("asd").Y : 0f) - Game1.pixelZoom, Color.White * alpha, 1f, false);
				b.Draw(Game1.menuTexture, new Rectangle(num7 + Game1.pixelZoom * 3, num8 + (int) Game1.dialogueFont.MeasureString(boldTitleText).Y + Game1.tileSize / 2 + (int) ((hoveredItem != null && text4.Length > 0) ? font.MeasureString("asd").Y : 0f) - Game1.pixelZoom, num2 - Game1.pixelZoom * ((craftingIngredients == null) ? 6 : 1), Game1.pixelZoom), new Rectangle?(new Rectangle(44, 300, 4, 4)), Color.White);
				b.DrawString(Game1.dialogueFont, boldTitleText, new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 + Game1.tileSize / 4 + 4)) + new Vector2(2f, 2f), Game1.textShadowColor);
				b.DrawString(Game1.dialogueFont, boldTitleText, new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 + Game1.tileSize / 4 + 4)) + new Vector2(0f, 2f), Game1.textShadowColor);
				b.DrawString(Game1.dialogueFont, boldTitleText, new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 + Game1.tileSize / 4 + 4)), Game1.textColor);
				num8 += (int) Game1.dialogueFont.MeasureString(boldTitleText).Y;
			}
			if (hoveredItem != null && text4.Length > 0) {
				num8 -= 4;
				Utility.drawTextWithShadow(b, text4, font, new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 + Game1.tileSize / 4 + 4)), hoveredItem.getCategoryColor(), 1f, -1f, 2, 2, 1f, 3);
				num8 += (int) font.MeasureString("T").Y + ((boldTitleText != null) ? (Game1.tileSize / 4) : 0) + Game1.pixelZoom;
			} else {
				num8 += ((boldTitleText != null) ? (Game1.tileSize / 4) : 0);
			}
			if (hoveredItem != null && hoveredItem is Boots) {
				Boots boots = hoveredItem as Boots;
				Utility.drawTextWithShadow(b, Game1.parseText(boots.description, Game1.smallFont, Game1.tileSize * 4 + Game1.tileSize / 4), font, new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 + Game1.tileSize / 4 + 4)), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
				num8 += (int) font.MeasureString(Game1.parseText(boots.description, Game1.smallFont, Game1.tileSize * 4 + Game1.tileSize / 4)).Y;
				if (boots.defenseBonus > 0) {
					Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2((float) (num7 + Game1.tileSize / 4 + Game1.pixelZoom), (float) (num8 + Game1.tileSize / 4 + 4)), new Rectangle(110, 428, 10, 10), Color.White, 0f, Vector2.Zero, (float) Game1.pixelZoom, false, 1f, -1, -1, 0.35f);
					Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:ItemHover_DefenseBonus", new object[] {
												boots.defenseBonus
										}), font, new Vector2((float) (num7 + Game1.tileSize / 4 + Game1.pixelZoom * 13), (float) (num8 + Game1.tileSize / 4 + Game1.pixelZoom * 3)), Game1.textColor * 0.9f * alpha, 1f, -1f, -1, -1, 1f, 3);
					num8 += (int) Math.Max(font.MeasureString("TT").Y, (float) (12 * Game1.pixelZoom));
				}
				if (boots.immunityBonus > 0) {
					Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2((float) (num7 + Game1.tileSize / 4 + Game1.pixelZoom), (float) (num8 + Game1.tileSize / 4 + 4)), new Rectangle(150, 428, 10, 10), Color.White, 0f, Vector2.Zero, (float) Game1.pixelZoom, false, 1f, -1, -1, 0.35f);
					Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:ItemHover_ImmunityBonus", new object[] {
												boots.immunityBonus
										}), font, new Vector2((float) (num7 + Game1.tileSize / 4 + Game1.pixelZoom * 13), (float) (num8 + Game1.tileSize / 4 + Game1.pixelZoom * 3)), Game1.textColor * 0.9f * alpha, 1f, -1f, -1, -1, 1f, 3);
					num8 += (int) Math.Max(font.MeasureString("TT").Y, (float) (12 * Game1.pixelZoom));
				}
			} else if (hoveredItem != null && hoveredItem is MeleeWeapon) {
				MeleeWeapon meleeWeapon = hoveredItem as MeleeWeapon;
				Utility.drawTextWithShadow(b, Game1.parseText(meleeWeapon.description, Game1.smallFont, Game1.tileSize * 4 + Game1.tileSize / 4), font, new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 + Game1.tileSize / 4 + 4)), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
				num8 += (int) font.MeasureString(Game1.parseText(meleeWeapon.description, Game1.smallFont, Game1.tileSize * 4 + Game1.tileSize / 4)).Y;
				if (meleeWeapon.indexOfMenuItemView != 47) {
					Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2((float) (num7 + Game1.tileSize / 4 + Game1.pixelZoom), (float) (num8 + Game1.tileSize / 4 + 4)), new Rectangle(120, 428, 10, 10), Color.White, 0f, Vector2.Zero, (float) Game1.pixelZoom, false, 1f, -1, -1, 0.35f);
					Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:ItemHover_Damage", new object[] {
												meleeWeapon.minDamage,
												meleeWeapon.maxDamage
										}), font, new Vector2((float) (num7 + Game1.tileSize / 4 + Game1.pixelZoom * 13), (float) (num8 + Game1.tileSize / 4 + Game1.pixelZoom * 3)), Game1.textColor * 0.9f * alpha, 1f, -1f, -1, -1, 1f, 3);
					num8 += (int) Math.Max(font.MeasureString("TT").Y, (float) (12 * Game1.pixelZoom));
					if (meleeWeapon.speed != ((meleeWeapon.type == 2) ? -8 : 0)) {
						Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2((float) (num7 + Game1.tileSize / 4 + Game1.pixelZoom), (float) (num8 + Game1.tileSize / 4 + 4)), new Rectangle(130, 428, 10, 10), Color.White, 0f, Vector2.Zero, (float) Game1.pixelZoom, false, 1f, -1, -1, 0.35f);
						bool flag = (meleeWeapon.type == 2 && meleeWeapon.speed < -8) || (meleeWeapon.type != 2 && meleeWeapon.speed < 0);
						Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:ItemHover_Speed", new object[] {
														((((meleeWeapon.type == 2) ? (meleeWeapon.speed - -8) : meleeWeapon.speed) > 0) ? "+" : "") + ((meleeWeapon.type == 2) ? (meleeWeapon.speed - -8) : meleeWeapon.speed) / 2
												}), font, new Vector2((float) (num7 + Game1.tileSize / 4 + Game1.pixelZoom * 13), (float) (num8 + Game1.tileSize / 4 + Game1.pixelZoom * 3)), flag ? Color.DarkRed : (Game1.textColor * 0.9f * alpha), 1f, -1f, -1, -1, 1f, 3);
						num8 += (int) Math.Max(font.MeasureString("TT").Y, (float) (12 * Game1.pixelZoom));
					}
					if (meleeWeapon.addedDefense > 0) {
						Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2((float) (num7 + Game1.tileSize / 4 + Game1.pixelZoom), (float) (num8 + Game1.tileSize / 4 + 4)), new Rectangle(110, 428, 10, 10), Color.White, 0f, Vector2.Zero, (float) Game1.pixelZoom, false, 1f, -1, -1, 0.35f);
						Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:ItemHover_DefenseBonus", new object[] {
														meleeWeapon.addedDefense
												}), font, new Vector2((float) (num7 + Game1.tileSize / 4 + Game1.pixelZoom * 13), (float) (num8 + Game1.tileSize / 4 + Game1.pixelZoom * 3)), Game1.textColor * 0.9f * alpha, 1f, -1f, -1, -1, 1f, 3);
						num8 += (int) Math.Max(font.MeasureString("TT").Y, (float) (12 * Game1.pixelZoom));
					}
					if ((double) meleeWeapon.critChance / 0.02 >= 2.0) {
						Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2((float) (num7 + Game1.tileSize / 4 + Game1.pixelZoom), (float) (num8 + Game1.tileSize / 4 + 4)), new Rectangle(40, 428, 10, 10), Color.White, 0f, Vector2.Zero, (float) Game1.pixelZoom, false, 1f, -1, -1, 0.35f);
						Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:ItemHover_CritChanceBonus", new object[] {
														(int)((double)meleeWeapon.critChance / 0.02)
												}), font, new Vector2((float) (num7 + Game1.tileSize / 4 + Game1.pixelZoom * 13), (float) (num8 + Game1.tileSize / 4 + Game1.pixelZoom * 3)), Game1.textColor * 0.9f * alpha, 1f, -1f, -1, -1, 1f, 3);
						num8 += (int) Math.Max(font.MeasureString("TT").Y, (float) (12 * Game1.pixelZoom));
					}
					if ((double) (meleeWeapon.critMultiplier - 3f) / 0.02 >= 1.0) {
						Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 + Game1.tileSize / 4 + 4)), new Rectangle(160, 428, 10, 10), Color.White, 0f, Vector2.Zero, (float) Game1.pixelZoom, false, 1f, -1, -1, 0.35f);
						Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:ItemHover_CritPowerBonus", new object[] {
														(int)((double)(meleeWeapon.critMultiplier - 3f) / 0.02)
												}), font, new Vector2((float) (num7 + Game1.tileSize / 4 + Game1.pixelZoom * 11), (float) (num8 + Game1.tileSize / 4 + Game1.pixelZoom * 3)), Game1.textColor * 0.9f * alpha, 1f, -1f, -1, -1, 1f, 3);
						num8 += (int) Math.Max(font.MeasureString("TT").Y, (float) (12 * Game1.pixelZoom));
					}
					if (meleeWeapon.knockback != meleeWeapon.defaultKnockBackForThisType(meleeWeapon.type)) {
						Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2((float) (num7 + Game1.tileSize / 4 + Game1.pixelZoom), (float) (num8 + Game1.tileSize / 4 + 4)), new Rectangle(70, 428, 10, 10), Color.White, 0f, Vector2.Zero, (float) Game1.pixelZoom, false, 1f, -1, -1, 0.35f);
						Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:ItemHover_Weight", new object[] {
														(((float)((int)Math.Ceiling ((double)(Math.Abs (meleeWeapon.knockback - meleeWeapon.defaultKnockBackForThisType (meleeWeapon.type)) * 10f))) > meleeWeapon.defaultKnockBackForThisType (meleeWeapon.type)) ? "+" : "") + (int)Math.Ceiling ((double)(Math.Abs (meleeWeapon.knockback - meleeWeapon.defaultKnockBackForThisType (meleeWeapon.type)) * 10f))
												}), font, new Vector2((float) (num7 + Game1.tileSize / 4 + Game1.pixelZoom * 13), (float) (num8 + Game1.tileSize / 4 + Game1.pixelZoom * 3)), Game1.textColor * 0.9f * alpha, 1f, -1f, -1, -1, 1f, 3);
						num8 += (int) Math.Max(font.MeasureString("TT").Y, (float) (12 * Game1.pixelZoom));
					}
				}
			} else if (!string.IsNullOrEmpty(text) && text != " ") {
				b.DrawString(font, text, new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 + Game1.tileSize / 4 + 4)) + new Vector2(2f, 2f), Game1.textShadowColor * alpha);
				b.DrawString(font, text, new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 + Game1.tileSize / 4 + 4)) + new Vector2(0f, 2f), Game1.textShadowColor * alpha);
				b.DrawString(font, text, new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 + Game1.tileSize / 4 + 4)) + new Vector2(2f, 0f), Game1.textShadowColor * alpha);
				b.DrawString(font, text, new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 + Game1.tileSize / 4 + 4)), Game1.textColor * 0.9f * alpha);
				num8 += (int) font.MeasureString(text).Y + 4;
			}
			if (craftingIngredients != null) {
				craftingIngredients.drawRecipeDescription(b, new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 - Game1.pixelZoom * 2)), num2);
				num8 += craftingIngredients.getDescriptionHeight(num2);
			}
			if (healAmountToDisplay != -1) {
				if (healAmountToDisplay > 0) {
					Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2((float) (num7 + Game1.tileSize / 4 + Game1.pixelZoom), (float) (num8 + Game1.tileSize / 4)), new Rectangle((healAmountToDisplay < 0) ? 140 : 0, 428, 10, 10), Color.White, 0f, Vector2.Zero, 3f, false, 0.95f, -1, -1, 0.35f);
					Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:ItemHover_Energy", new object[] {
												((healAmountToDisplay > 0) ? "+" : "") + healAmountToDisplay
										}), font, new Vector2((float) (num7 + Game1.tileSize / 4 + 34 + Game1.pixelZoom), (float) (num8 + Game1.tileSize / 4 + 8)), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
					num8 += 34;
					Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2((float) (num7 + Game1.tileSize / 4 + Game1.pixelZoom), (float) (num8 + Game1.tileSize / 4)), new Rectangle(0, 438, 10, 10), Color.White, 0f, Vector2.Zero, 3f, false, 0.95f, -1, -1, 0.35f);
					Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:ItemHover_Health", new object[] {
												((healAmountToDisplay > 0) ? "+" : "") + (int)((float)healAmountToDisplay * 0.4f)
										}), font, new Vector2((float) (num7 + Game1.tileSize / 4 + 34 + Game1.pixelZoom), (float) (num8 + Game1.tileSize / 4 + 8)), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
					num8 += 34;
				} else if (healAmountToDisplay != -300) {
					Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2((float) (num7 + Game1.tileSize / 4 + Game1.pixelZoom), (float) (num8 + Game1.tileSize / 4)), new Rectangle(140, 428, 10, 10), Color.White, 0f, Vector2.Zero, 3f, false, 0.95f, -1, -1, 0.35f);
					Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:ItemHover_Energy", new object[] {
												string.Concat (healAmountToDisplay)
										}), font, new Vector2((float) (num7 + Game1.tileSize / 4 + 34 + Game1.pixelZoom), (float) (num8 + Game1.tileSize / 4 + 8)), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
					num8 += 34;
				}
			}
			if (buffIconsToDisplay != null) {
				for (int k = 0; k < buffIconsToDisplay.Length; k++) {
					if (!buffIconsToDisplay[k].Equals("0")) {
						Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2((float) (num7 + Game1.tileSize / 4 + Game1.pixelZoom), (float) (num8 + Game1.tileSize / 4)), new Rectangle(10 + k * 10, 428, 10, 10), Color.White, 0f, Vector2.Zero, 3f, false, 0.95f, -1, -1, 0.35f);
						string text5 = ((Convert.ToInt32(buffIconsToDisplay[k]) > 0) ? "+" : "") + buffIconsToDisplay[k] + " ";
						if (k <= 11) {
							text5 = Game1.content.LoadString("Strings\\UI:ItemHover_Buff" + k, new object[] {
																text5
														});
						}
						Utility.drawTextWithShadow(b, text5, font, new Vector2((float) (num7 + Game1.tileSize / 4 + 34 + Game1.pixelZoom), (float) (num8 + Game1.tileSize / 4 + 8)), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
						num8 += 34;
					}
				}
			}
			if (hoveredItem != null && hoveredItem.attachmentSlots() > 0) {
				num8 += 16;
				hoveredItem.drawAttachments(b, num7 + Game1.tileSize / 4, num8);
				if (moneyAmountToDisplayAtBottom > -1) {
					num8 += Game1.tileSize * hoveredItem.attachmentSlots();
				}
			}
			if (moneyAmountToDisplayAtBottom > -1) {
				b.DrawString(font, string.Concat(moneyAmountToDisplayAtBottom), new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 + Game1.tileSize / 4 + 4)) + new Vector2(2f, 2f), Game1.textShadowColor);
				b.DrawString(font, string.Concat(moneyAmountToDisplayAtBottom), new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 + Game1.tileSize / 4 + 4)) + new Vector2(0f, 2f), Game1.textShadowColor);
				b.DrawString(font, string.Concat(moneyAmountToDisplayAtBottom), new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 + Game1.tileSize / 4 + 4)) + new Vector2(2f, 0f), Game1.textShadowColor);
				b.DrawString(font, string.Concat(moneyAmountToDisplayAtBottom), new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 + Game1.tileSize / 4 + 4)), Game1.textColor);
				if (currencySymbol == 0) {
					b.Draw(Game1.debrisSpriteSheet, new Vector2((float) (num7 + Game1.tileSize / 4) + font.MeasureString(string.Concat(moneyAmountToDisplayAtBottom)).X + 20f, (float) (num8 + Game1.tileSize / 4 + 16)), new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16)), Color.White, 0f, new Vector2(8f, 8f), (float) Game1.pixelZoom, SpriteEffects.None, 0.95f);
				} else if (currencySymbol == 1) {
					b.Draw(Game1.mouseCursors, new Vector2((float) (num7 + Game1.tileSize / 8) + font.MeasureString(string.Concat(moneyAmountToDisplayAtBottom)).X + 20f, (float) (num8 + Game1.tileSize / 4 - 5)), new Rectangle?(new Rectangle(338, 400, 8, 8)), Color.White, 0f, Vector2.Zero, (float) Game1.pixelZoom, SpriteEffects.None, 1f);
				} else if (currencySymbol == 2) {
					b.Draw(Game1.mouseCursors, new Vector2((float) (num7 + Game1.tileSize / 8) + font.MeasureString(string.Concat(moneyAmountToDisplayAtBottom)).X + 20f, (float) (num8 + Game1.tileSize / 4 - 7)), new Rectangle?(new Rectangle(211, 373, 9, 10)), Color.White, 0f, Vector2.Zero, (float) Game1.pixelZoom, SpriteEffects.None, 1f);
				}
				num8 += Game1.tileSize * 3 / 4;
			}
			if (extraItemToShowIndex != -1) {
				IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), num7, num8 + Game1.pixelZoom, num2, Game1.tileSize * 3 / 2, Color.White, 1f, true);
				num8 += Game1.pixelZoom * 5;
				string text6 = Game1.objectInformation[extraItemToShowIndex].Split(new char[] {
										'/'
								})[4];
				string text7 = Game1.content.LoadString("Strings\\UI:ItemHover_Requirements", new object[] {
										extraItemToShowAmount,
										text6
								});
				b.DrawString(font, text7, new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 + Game1.pixelZoom)) + new Vector2(2f, 2f), Game1.textShadowColor);
				b.DrawString(font, text7, new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 + Game1.pixelZoom)) + new Vector2(0f, 2f), Game1.textShadowColor);
				b.DrawString(font, text7, new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 + Game1.pixelZoom)) + new Vector2(2f, 0f), Game1.textShadowColor);
				b.DrawString(Game1.smallFont, text7, new Vector2((float) (num7 + Game1.tileSize / 4), (float) (num8 + Game1.pixelZoom)), Game1.textColor);
				b.Draw(Game1.objectSpriteSheet, new Vector2((float) (num7 + Game1.tileSize / 4 + (int) font.MeasureString(text7).X + Game1.tileSize / 3), (float) num8), new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, extraItemToShowIndex, 16, 16)), Color.White, 0f, Vector2.Zero, (float) Game1.pixelZoom, SpriteEffects.None, 1f);
			}
		}

	}
}
