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
		List<string> cropData = Game1.content.Load<Dictionary<int, string>>(Path.Combine("Data", "Crops.xnb")).Values.ToList();
		Dictionary<int, string> treeData = Game1.content.Load<Dictionary<int, string>>(Path.Combine("Data", "fruitTrees.xnb"));
		Dictionary<string, string> bundleData = Game1.content.Load<Dictionary<String, String>>(Path.Combine("Data", "Bundles.xnb"));

		List<int> springForage = new List<int> { 16, 18, 20, 22, 399, 257, 404, 296 };
		List<int> summerForage = new List<int> { 396, 402, 420, 259 };
		List<int> fallForage = new List<int> { 406, 408, 410, 281, 404, 420 };
		List<int> winterForage = new List<int> { 412, 414, 416, 418, 283 };

		public ShowItemHoverInformation(ModOptions modOptions) {
			_showItemHoverInformation = modOptions.GetOptionWithIdentifier<ModOptionToggle>(OptionKeys.ShowExtraItemInformation) ?? new ModOptionToggle(OptionKeys.ShowExtraItemInformation, "Show Item hover information");
			_showItemHoverInformation.ValueChanged += ToggleOption;
			modOptions.AddModOption(_showItemHoverInformation);

			ToggleOption(_showItemHoverInformation.identifier, _showItemHoverInformation.IsOn);
		}

		public void ToggleOption(string identifier, bool showItemHoverInformation) {
			if (identifier != OptionKeys.ShowExtraItemInformation)
				return;

			PlayerEvents.InventoryChanged -= PopulateRequiredBundles;
			GraphicsEvents.OnPostRenderEvent -= DrawAdvancedTooltipForMenu;
			GraphicsEvents.OnPostRenderHudEvent -= DrawAdvancedTooltipForToolbar;
			GraphicsEvents.OnPreRenderEvent -= GetHoverItem;

			if (showItemHoverInformation) {
				_communityCenter = Game1.getLocationFromName("CommunityCenter") as CommunityCenter;
				_bundleData = Game1.content.Load<Dictionary<String, String>>("Data\\Bundles");
				PopulateRequiredBundles(null, null);
				PlayerEvents.InventoryChanged += PopulateRequiredBundles;
				GraphicsEvents.OnPostRenderEvent += DrawAdvancedTooltipForMenu;
				GraphicsEvents.OnPostRenderHudEvent += DrawAdvancedTooltipForToolbar;
				GraphicsEvents.OnPreRenderEvent += GetHoverItem;
			}
		}

		public void Dispose() {
			ToggleOption(OptionKeys.ShowExtraItemInformation, false);
		}

		private void GetHoverItem(object sender, EventArgs e) {
			_hoverItem = Tools.GetHoveredItem();
		}

		private void DrawAdvancedTooltipForToolbar(object sender, EventArgs e) {
			if (Game1.activeClickableMenu == null) {
				DrawAdvancedTooltip(sender, e);
			}
		}

		private void DrawAdvancedTooltipForMenu(object sender, EventArgs e) {
			if (Game1.activeClickableMenu != null) {
				DrawAdvancedTooltip(sender, e);
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

		private Tuple<bool[], bool[], string> GetSeasonsTimesAndWeather(StardewValley.Object hoveredObject) {

			// Spring, Summer, Fall, Winter
			bool[] seasons = { false, false, false, false };

			// Rainy, Sunny
			bool[] weather = { false, false };
			string times = "";

			if (hoveredObject != null && fishData.ContainsKey(hoveredObject.ParentSheetIndex) && !hoveredObject.Name.Contains("Algae") && !hoveredObject.Name.Contains("Seaweed")) {
				// draw the seasons icons 
				var data = fishData[hoveredObject.ParentSheetIndex].Split('/');
				if (data[1] != "trap") {

					var weatherData = data[7].Split(' ');
					if (!weatherData.Contains("both")) { // if all weather don't draw any
						if (weatherData.Contains("rainy")) {
							weather[0] = true;
						} else {
							weather[1] = true;
						}
					} else {
						weather[0] = true;
						weather[1] = true;
					}

					var timesData = data[5].Split(' ');

					if (!(timesData[0] == "600" && timesData[1] == "2600")) {
						for (int i = 0; i < times.Length; i++) {
							int time = (int.Parse(timesData[i]) / 100);
							times += time - (time > 12 ? 12 * (int) (time / 12) : 0);
							if (time >= 12 && time < 24)
								times += "pm";
							else
								times += "am";

							if (i % 2 == 1 && i != times.Length - 1) {
								times += ", ";
							} else if (i % 2 == 0) {
								times += "-";
							}
						}
					} else {
						times = "Any Time";
					}

					// show seasons
					var seasonsData = data[6].Split(' ');
					if (seasonsData.Count() > 0) { // if not all seasons

						if (seasonsData.Contains("spring"))
							seasons[0] = true;

						if (seasonsData.Contains("summer"))
							seasons[1] = true;

						if (seasonsData.Contains("fall"))
							seasons[2] = true;

						if (seasonsData.Contains("winter"))
							seasons[3] = true;
					}
				}
			} else if (hoveredObject != null && treeData.Values.ToList().Exists(x => x.Split('/')[2] == $"{hoveredObject.ParentSheetIndex}")) {

				var data = treeData.Values.ToList().Find(x => x.Split('/')[2] == $"{hoveredObject.ParentSheetIndex}").Split('/');

				var seasonsData = data[1].Split(' ');
				if (seasonsData.Count() > 0) {
					if (seasonsData.Contains("spring"))
						seasons[0] = true;

					if (seasonsData.Contains("summer"))
						seasons[1] = true;

					if (seasonsData.Contains("fall"))
						seasons[2] = true;

					if (seasonsData.Contains("winter"))
						seasons[3] = true;
				}

			} else if (hoveredObject != null && cropData.Exists(x => { return x.Split('/')[3] == $"{hoveredObject.ParentSheetIndex}"; })) {

				var data = cropData.Find(x => { return x.Split('/')[3] == $"{hoveredObject.ParentSheetIndex}"; }).Split('/');

				var seasonsData = data[1].Split(' ');
				if (seasonsData.Count() > 0) {

					if (seasonsData.Contains("spring"))
						seasons[0] = true;

					if (seasonsData.Contains("summer"))
						seasons[1] = true;

					if (seasonsData.Contains("fall"))
						seasons[2] = true;

					if (seasonsData.Contains("winter"))
						seasons[3] = true;
				}
			} else if (hoveredObject != null
				&& ((fallForage.Contains(hoveredObject.ParentSheetIndex))
				|| (springForage.Contains(hoveredObject.ParentSheetIndex))
				|| (winterForage.Contains(hoveredObject.ParentSheetIndex))
				|| (summerForage.Contains(hoveredObject.ParentSheetIndex))
				)) { // Foraged items

				if (springForage.Contains(hoveredObject.ParentSheetIndex))
					seasons[0] = true;

				if (summerForage.Contains(hoveredObject.ParentSheetIndex))
					seasons[1] = true;

				if (fallForage.Contains(hoveredObject.ParentSheetIndex))
					seasons[2] = true;

				if (winterForage.Contains(hoveredObject.ParentSheetIndex))
					seasons[3] = true;
			}

			return new Tuple<bool[], bool[], string>(seasons, weather, times);

		}

		Components hover = new Components();

		private void DrawAdvancedTooltip(object sender, EventArgs e) {
			if (_hoverItem == null)
				return;

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

			Tuple<bool[], bool[], string> timeInfo = GetSeasonsTimesAndWeather(hoverObject);

			if (hoverObject.type == "Seeds") {

				if (hoverObject.Name != "Mixed Seeds" && hoverObject.Name != "Winter Seeds" && hoverObject.Name != "Summer Seeds" && hoverObject.Name != "Fall Seeds" && hoverObject.Name != "Spring Seeds") {
					var crop = new StardewValley.Object(new Debris(new Crop(_hoverItem.parentSheetIndex, 0, 0).indexOfHarvest, Game1.player.position, Game1.player.position).chunkType, 1);
					var cropPrice = crop.Price;

					timeInfo = GetSeasonsTimesAndWeather(crop);

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

			if (timeInfo.Item1.Contains(true)) { // if at least one season
				int num = timeInfo.Item1.Where(x => { return x; }).Count();

				if (timeInfo.Item1[0])
					hover.springIcon.hidden = false;
				if (timeInfo.Item1[1])
					hover.summerIcon.hidden = false;
				if (timeInfo.Item1[2])
					hover.fallIcon.hidden = false;
				if (timeInfo.Item1[3])
					hover.winterIcon.hidden = false;

				hover.Background.Height += hover.springIcon.Height + itemSpacing;
				hover.ExtendBackgroundWidth((hover.springIcon.Width + itemSpacing) * num + padding);
			}

			if (timeInfo.Item2.Contains(true)) { // if at least one season
				int num = timeInfo.Item2.Where(x => { return x; }).Count();

				if (timeInfo.Item2[0])
					hover.rainyIcon.hidden = false;
				if (timeInfo.Item2[1])
					hover.sunnyIcon.hidden = false;

				hover.Background.Height += hover.rainyIcon.Height + itemSpacing;
				hover.ExtendBackgroundWidth((hover.rainyIcon.Width + itemSpacing) * num + padding);
			}

			if (timeInfo.Item3 != "") {
				hover.fishTimes.hidden = false;
				hover.fishTimes.text = timeInfo.Item3;

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
			int paddedLocationX = hover.Background.X + padding / 2 ;

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

			if (!hover.springIcon.hidden || !hover.summerIcon.hidden || !hover.fallIcon.hidden || !hover.winterIcon.hidden) {
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

			if (!hover.fishTimes.hidden) {
				hover.fishTimes.draw(Game1.spriteBatch, new Vector2(paddedLocationX, currentLocationY));

				currentLocationY += hover.fishTimes.Height + itemSpacing;
			}
		}

		private void RestoreMenuState() {
			if (Game1.activeClickableMenu is ItemGrabMenu) {
				(Game1.activeClickableMenu as MenuWithInventory).hoveredItem = _hoverItem;
			}
		}


		private static Vector2 DrawTooltip(SpriteBatch batch, String hoverText, String hoverTitle, Item hoveredItem) {
			bool flag = hoveredItem != null &&
					hoveredItem is StardewValley.Object &&
					(hoveredItem as StardewValley.Object).edibility != -300;

			int healAmmountToDisplay = flag ? (hoveredItem as StardewValley.Object).edibility : -1;
			string[] buffIconsToDisplay = null;
			if (flag) {
				String objectInfo = Game1.objectInformation[(hoveredItem as StardewValley.Object).ParentSheetIndex];
				if (Game1.objectInformation[(hoveredItem as StardewValley.Object).parentSheetIndex].Split('/').Length >= 7) {
					buffIconsToDisplay = Game1.objectInformation[(hoveredItem as StardewValley.Object).parentSheetIndex].Split('/')[6].Split('^');
				}
			}

			return DrawHoverText(batch, hoverText, Game1.smallFont, -1, -1, -1, hoverTitle, -1, buffIconsToDisplay, hoveredItem);
		}

		static Rectangle defaultTooltip = new Rectangle();

		private static Vector2 DrawHoverText(SpriteBatch batch, String text, SpriteFont font, int xOffset = 0, int yOffset = 0, int moneyAmountToDisplayAtBottom = -1, String boldTitleText = null, int healAmountToDisplay = -1, string[] buffIconsToDisplay = null, Item hoveredItem = null) {
			Vector2 result = Vector2.Zero;

			if (String.IsNullOrEmpty(text)) {
				result = Vector2.Zero;
			} else {
				if (String.IsNullOrEmpty(boldTitleText))
					boldTitleText = null;

				int num1 = 20;
				int infoWindowWidth = (int) Math.Max(healAmountToDisplay != -1 ? font.MeasureString(healAmountToDisplay.ToString() + "+ Energy" + (Game1.tileSize / 2)).X : 0, Math.Max(font.MeasureString(text).X, boldTitleText != null ? Game1.dialogueFont.MeasureString(boldTitleText).X : 0)) + Game1.tileSize / 2;
				int extraInfoBackgroundHeight = (int) Math.Max(
						num1 * 3,
						font.MeasureString(text).Y + Game1.tileSize / 2 + (moneyAmountToDisplayAtBottom > -1 ? (font.MeasureString(string.Concat(moneyAmountToDisplayAtBottom)).Y + 4.0) : 0) + (boldTitleText != null ? Game1.dialogueFont.MeasureString(boldTitleText).Y + (Game1.tileSize / 4) : 0) + (healAmountToDisplay != -1 ? 38 : 0));
				if (buffIconsToDisplay != null) {
					for (int i = 0; i < buffIconsToDisplay.Length; ++i) {
						if (!buffIconsToDisplay[i].Equals("0"))
							extraInfoBackgroundHeight += 34;
					}
					extraInfoBackgroundHeight += 4;
				}

				String categoryName = null;
				if (hoveredItem != null) {
					extraInfoBackgroundHeight += (Game1.tileSize + 4) * hoveredItem.attachmentSlots();
					categoryName = hoveredItem.getCategoryName();
					if (categoryName.Length > 0)
						extraInfoBackgroundHeight += (int) font.MeasureString("T").Y;

					if (hoveredItem is MeleeWeapon) {
						extraInfoBackgroundHeight = (int) (Math.Max(
								num1 * 3,
								(boldTitleText != null ?
										Game1.dialogueFont.MeasureString(boldTitleText).Y + (Game1.tileSize / 4)
										: 0) +
								Game1.tileSize / 2) +
								font.MeasureString("T").Y +
								(moneyAmountToDisplayAtBottom > -1 ?
										font.MeasureString(string.Concat(moneyAmountToDisplayAtBottom)).Y + 4.0
										: 0) +
								(hoveredItem as MeleeWeapon).getNumberOfDescriptionCategories() *
								Game1.pixelZoom * 12 +
								font.MeasureString(Game1.parseText((hoveredItem as MeleeWeapon).Description,
								Game1.smallFont,
								Game1.tileSize * 4 +
								Game1.tileSize / 4)).Y);

						infoWindowWidth = (int) Math.Max(infoWindowWidth, font.MeasureString("99-99 Damage").X + (15 * Game1.pixelZoom) + (Game1.tileSize / 2));
					} else if (hoveredItem is Boots) {
						Boots hoveredBoots = hoveredItem as Boots;
						extraInfoBackgroundHeight = extraInfoBackgroundHeight - (int) font.MeasureString(text).Y + (int) (hoveredBoots.getNumberOfDescriptionCategories() * Game1.pixelZoom * 12 + font.MeasureString(Game1.parseText(hoveredBoots.description, Game1.smallFont, Game1.tileSize * 4 + Game1.tileSize / 4)).Y);
						infoWindowWidth = (int) Math.Max(infoWindowWidth, font.MeasureString("99-99 Damage").X + (15 * Game1.pixelZoom) + (Game1.tileSize / 2));
					} else if (hoveredItem is StardewValley.Object &&
								(hoveredItem as StardewValley.Object).edibility != -300) {
						StardewValley.Object hoveredObject = hoveredItem as StardewValley.Object;
						healAmountToDisplay = (int) Math.Ceiling(hoveredObject.Edibility * 2.5) + hoveredObject.quality * hoveredObject.Edibility;
						extraInfoBackgroundHeight += (Game1.tileSize / 2 + Game1.pixelZoom * 2) * (healAmountToDisplay > 0 ? 2 : 1);
					}
				}

				//Crafting ingredients were never used

				int xPos = Game1.getOldMouseX() + Game1.tileSize / 2 + xOffset;
				int yPos = Game1.getOldMouseY() + Game1.tileSize / 2 + yOffset;

				if (xPos + infoWindowWidth > Game1.viewport.Width) {
					xPos = Game1.viewport.Width - infoWindowWidth;
					yPos += Game1.tileSize / 4;
				}

				if (yPos + extraInfoBackgroundHeight > Game1.viewport.Height) {
					xPos += Game1.tileSize / 4;
					yPos = Game1.viewport.Height - extraInfoBackgroundHeight;
				}
				int hoveredItemHeight = (int) (hoveredItem == null || categoryName.Length <= 0 ? 0 : font.MeasureString("asd").Y);

				// save location for custom tooltip
				ShowItemHoverInformation.defaultTooltip.X = xPos;
				ShowItemHoverInformation.defaultTooltip.Y = yPos;
				ShowItemHoverInformation.defaultTooltip.Width = infoWindowWidth;
				ShowItemHoverInformation.defaultTooltip.Height = extraInfoBackgroundHeight;

				IClickableMenu.drawTextureBox(
						batch,
						Game1.menuTexture,
						new Rectangle(0, 256, 60, 60),
						xPos,
						yPos,
						infoWindowWidth,
						extraInfoBackgroundHeight,
						Color.White);

				if (boldTitleText != null) {
					IClickableMenu.drawTextureBox(
							batch,
							Game1.menuTexture,
							new Rectangle(0, 256, 60, 60),
							xPos,
							yPos,
							infoWindowWidth,
							(int) (Game1.dialogueFont.MeasureString(boldTitleText).Y + Game1.tileSize / 2 + hoveredItemHeight - Game1.pixelZoom),
							Color.White,
							1,
							false);

					batch.Draw(
							Game1.menuTexture,
							new Rectangle(xPos + Game1.pixelZoom * 3, yPos + (int) Game1.dialogueFont.MeasureString(boldTitleText).Y + Game1.tileSize / 2 + hoveredItemHeight - Game1.pixelZoom, infoWindowWidth - Game1.pixelZoom * 6, Game1.pixelZoom),
							new Rectangle(44, 300, 4, 4),
							Color.White);

					batch.DrawString(
							Game1.dialogueFont,
							boldTitleText,
							new Vector2(xPos + Game1.tileSize / 4, yPos + Game1.tileSize / 4 + 4) + new Vector2(2, 2),
							Game1.textShadowColor);

					batch.DrawString(
							Game1.dialogueFont,
							boldTitleText,
							new Vector2(xPos + Game1.tileSize / 4, yPos + Game1.tileSize / 4 + 4) + new Vector2(0, 2),
							Game1.textShadowColor);

					batch.DrawString(
							Game1.dialogueFont,
							boldTitleText,
							new Vector2(xPos + Game1.tileSize / 4, yPos + Game1.tileSize / 4 + 4),
							Game1.textColor);

					yPos += (int) Game1.dialogueFont.MeasureString(boldTitleText).Y;
				}

				int yPositionToReturn = yPos;
				if (hoveredItem != null && categoryName.Length > 0) {
					yPos -= 4;
					Utility.drawTextWithShadow(
							batch,
							categoryName,
							font,
							new Vector2(xPos + Game1.tileSize / 4, yPos + Game1.tileSize / 4 + 4),
							hoveredItem.getCategoryColor(),
							1,
							-1,
							2,
							2);
					yPos += (int) (font.MeasureString("T").Y + (boldTitleText != null ? Game1.tileSize / 4 : 0) + Game1.pixelZoom);
				} else {
					yPos += (boldTitleText != null ? Game1.tileSize / 4 : 0);
				}

				if (hoveredItem is Boots) {
					Boots boots = hoveredItem as Boots;
					Utility.drawTextWithShadow(
							batch,
							Game1.parseText(
									boots.description,
									Game1.smallFont,
									Game1.tileSize * 4 + Game1.tileSize / 4),
							font,
							new Vector2(xPos + Game1.tileSize / 4, yPos + Game1.tileSize / 4 + 4),
							Game1.textColor);

					yPos += (int) font.MeasureString(
							Game1.parseText(
									boots.description,
									Game1.smallFont,
									Game1.tileSize * 4 + Game1.tileSize / 4)).Y;

					if (boots.defenseBonus > 0) {
						Utility.drawWithShadow(
								batch,
								Game1.mouseCursors,
								new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 4),
								new Rectangle(110, 428, 10, 10),
								Color.White,
								0,
								Vector2.Zero,
								Game1.pixelZoom);

						Utility.drawTextWithShadow(
								batch,
								Game1.content.LoadString("Strings\\UI:ItemHover_DefenseBonus", new object[] { boots.defenseBonus }),
								font,
								new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 13, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3),
								Game1.textColor * 0.9f);
						yPos += (int) Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);
					}

					if (boots.immunityBonus > 0) {
						Utility.drawWithShadow(
								batch,
								Game1.mouseCursors,
								new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 4),
								new Rectangle(150, 428, 10, 10),
								Color.White,
								0,
								Vector2.Zero,
								Game1.pixelZoom);
						Utility.drawTextWithShadow(
								batch,
								Game1.content.LoadString("Strings\\UI:ItemHover_ImmunityBonus", new object[] { boots.immunityBonus }),
								font,
								new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 13, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3),
								Game1.textColor * 0.9f);

						yPos += (int) Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);
					}
				} else if (hoveredItem is MeleeWeapon) {
					MeleeWeapon meleeWeapon = hoveredItem as MeleeWeapon;
					Utility.drawTextWithShadow(
							batch,
							Game1.parseText(meleeWeapon.Description, Game1.smallFont, Game1.tileSize * 4 + Game1.tileSize / 4),
							font,
							new Vector2(xPos + Game1.tileSize / 4, yPos + Game1.tileSize / 4 + 4),
							Game1.textColor);
					yPos += (int) font.MeasureString(Game1.parseText(meleeWeapon.Description, Game1.smallFont, Game1.tileSize * 4 + Game1.tileSize / 4)).Y;

					if ((meleeWeapon as Tool).indexOfMenuItemView != 47) {
						Utility.drawWithShadow(
								batch,
								Game1.mouseCursors,
								new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 4),
								new Rectangle(120, 428, 10, 10),
								Color.White,
								0,
								Vector2.Zero,
								Game1.pixelZoom);

						Utility.drawTextWithShadow(
								batch,
								Game1.content.LoadString("Strings\\UI:ItemHover_Damage", new object[] { meleeWeapon.minDamage, meleeWeapon.maxDamage }),
								font,
								new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 13, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3),
								Game1.textColor * 0.9f);
						yPos += (int) Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);

						if (meleeWeapon.speed != (meleeWeapon.type == 2 ? -8 : 0)) {
							Utility.drawWithShadow(
									batch,
									Game1.mouseCursors,
									new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 4),
									new Rectangle(130, 428, 10, 10),
									Color.White,
									0,
									Vector2.Zero,
									Game1.pixelZoom,
									false,
									1);
							bool flag = meleeWeapon.type == 2 ? meleeWeapon.speed < -8 : meleeWeapon.speed < 0;
							String speedText = ((meleeWeapon.type == 2 ? meleeWeapon.speed + 8 : meleeWeapon.speed) / 2).ToString();
							Utility.drawTextWithShadow(
									batch,
									Game1.content.LoadString("Strings\\UI:ItemHover_Speed", new object[] { (meleeWeapon.speed > 0 ? "+" : "") + speedText }),
									font,
									new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 13, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3),
									flag ? Color.DarkRed : Game1.textColor * 0.9f);
							yPos += (int) Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);
						}

						if (meleeWeapon.addedDefense > 0) {
							Utility.drawWithShadow(
									batch,
									Game1.mouseCursors,
									new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 4),
									new Rectangle(110, 428, 10, 10),
									Color.White,
									0.0f,
									Vector2.Zero,
									Game1.pixelZoom,
									false,
									1f);
							Utility.drawTextWithShadow(
									batch,
									Game1.content.LoadString("Strings\\UI:ItemHover_DefenseBonus", new object[] { meleeWeapon.addedDefense }),
									font,
									new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 13, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3),
									Game1.textColor * 0.9f);
							yPos += (int) Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);
						}

						if (meleeWeapon.critChance / 0.02 >= 2.0) {
							Utility.drawWithShadow(
									batch,
									Game1.mouseCursors,
									new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 4),
									new Rectangle(40, 428, 10, 10),
									Color.White,
									0.0f,
									Vector2.Zero,
									Game1.pixelZoom,
									false,
									1f);
							Utility.drawTextWithShadow(
									batch, Game1.content.LoadString("Strings\\UI:ItemHover_CritChanceBonus", new object[] { meleeWeapon.critChance / 0.02 }),
									font,
									new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 13, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3),
									Game1.textColor * 0.9f);
							yPos += (int) Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);
						}

						if (((double) meleeWeapon.critMultiplier - 3.0) / 0.02 >= 1.0) {
							Utility.drawWithShadow(
									batch,
									Game1.mouseCursors,
									new Vector2(xPos + Game1.tileSize / 4, yPos + Game1.tileSize / 4 + 4),
									new Rectangle(160, 428, 10, 10),
									Color.White,
									0.0f,
									Vector2.Zero,
									Game1.pixelZoom,
									false,
									1f);

							Utility.drawTextWithShadow(
									batch, Game1.content.LoadString("Strings\\UI:ItemHover_CritPowerBonus", new object[] { (int) ((meleeWeapon.critMultiplier - 3.0) / 0.02) }),
									font,
									new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 11, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3),
									Game1.textColor * 0.9f);
							yPos += (int) Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);
						}

						if (meleeWeapon.knockback != meleeWeapon.defaultKnockBackForThisType(meleeWeapon.type)) {
							Utility.drawWithShadow(
									batch,
									Game1.mouseCursors,
									new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 4),
									new Rectangle(70, 428, 10, 10),
									Color.White,
									0.0f,
									Vector2.Zero, Game1.pixelZoom,
									false,
									1f);
							Utility.drawTextWithShadow(
									batch,
									Game1.content.LoadString(
											"Strings\\UI:ItemHover_Weight",
											new object[] { meleeWeapon.knockback > meleeWeapon.defaultKnockBackForThisType(meleeWeapon.type) ? "+" : "" + Math.Ceiling(Math.Abs(meleeWeapon.knockback - meleeWeapon.defaultKnockBackForThisType(meleeWeapon.type) * 10.0)) }),
									font,
									new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom * 13, yPos + Game1.tileSize / 4 + Game1.pixelZoom * 3),
									Game1.textColor * 0.9f);
							yPos += (int) Math.Max(font.MeasureString("TT").Y, 12 * Game1.pixelZoom);
						}
					}

				} else if (text.Length > 1) {
					int textXPos = xPos + Game1.tileSize / 4;
					int textYPos = yPos + Game1.tileSize / 4 + 4;
					batch.DrawString(
							font,
							text,
							new Vector2(textXPos, textYPos) + new Vector2(2, 2),
							Game1.textShadowColor);

					batch.DrawString(
							font,
							text,
							new Vector2(textXPos, textYPos) + new Vector2(0, 2),
							Game1.textShadowColor);

					batch.DrawString(
							font,
							text,
							new Vector2(textXPos, textYPos) + new Vector2(2, 0),
							Game1.textShadowColor);

					batch.DrawString(
							font,
							text,
							new Vector2(textXPos, textYPos),
							Game1.textColor * 0.9f);

					yPos += (int) font.MeasureString(text).Y + 4;
				}

				if (healAmountToDisplay != -1) {
					Utility.drawWithShadow(
							batch,
							Game1.mouseCursors,
							new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4),
							new Rectangle(healAmountToDisplay < 0 ? 140 : 0, 428, 10, 10),
							Color.White,
							0.0f,
							Vector2.Zero,
							3f,
							false,
							0.95f);
					Utility.drawTextWithShadow(
							batch, Game1.content.LoadString("Strings\\UI:ItemHover_Energy", new object[] { ((healAmountToDisplay > 0 ? "+" : "") + healAmountToDisplay) }),
							font,
							new Vector2(xPos + Game1.tileSize / 4 + 34 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 8),
							Game1.textColor);
					yPos += 34;

					if (healAmountToDisplay > 0) {
						Utility.drawWithShadow(
								batch,
								Game1.mouseCursors,
								new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4),
								new Rectangle(0, 438, 10, 10),
								Color.White,
								0,
								Vector2.Zero,
								3,
								false,
								0.95f);

						Utility.drawTextWithShadow(
								batch,
								Game1.content.LoadString(
										"Strings\\UI:ItemHover_Health",
										new object[] { "+" + (healAmountToDisplay * 0.4) }),
								font,
								new Vector2(xPos + Game1.tileSize / 4 + 34 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 8),
								Game1.textColor);

						yPos += 34;
					}
				}

				if (buffIconsToDisplay != null) {
					for (int i = 0; i < buffIconsToDisplay.Length; ++i) {
						String buffIcon = buffIconsToDisplay[i];
						if (buffIcon != "0") {
							Utility.drawWithShadow(
									batch,
									Game1.mouseCursors,
									new Vector2(xPos + Game1.tileSize / 4 + Game1.pixelZoom, yPos + Game1.tileSize / 4),
									new Rectangle(10 + i * 10, 428, 10, 10),
									Color.White,
									0, Vector2.Zero,
									3,
									false,
									0.95f);

							string textToDraw = (buffIcon.SafeParseInt32() > 0 ? "+" : string.Empty) + buffIcon + " ";

							//if (i <= 10)
							//    textToDraw = Game1.content.LoadString("Strings\\UI:ItemHover_Buff" + i, new object[] { textToDraw });

							Utility.drawTextWithShadow(
									batch,
									textToDraw,
									font,
									new Vector2(xPos + Game1.tileSize / 4 + 34 + Game1.pixelZoom, yPos + Game1.tileSize / 4 + 8),
									Game1.textColor);
							yPos += 34;
						}
					}
				}

				if (hoveredItem != null &&
						hoveredItem.attachmentSlots() > 0) {
					yPos += 16;
					hoveredItem.drawAttachments(batch, xPos + Game1.tileSize / 4, yPos);
					if (moneyAmountToDisplayAtBottom > -1)
						yPos += Game1.tileSize * hoveredItem.attachmentSlots();
				}

				if (moneyAmountToDisplayAtBottom > -1) {

				}

				result = new Vector2(xPos, yPositionToReturn);
			}

			return result;
		}
	}
}
