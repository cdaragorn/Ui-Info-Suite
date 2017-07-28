using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UIInfoSuite.Extensions;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewConfigFramework;
using StardewModdingAPI;

namespace UIInfoSuite.UIElements
{
	class ShowBirthdayIcon: IDisposable
	{
		private NPC _birthdayNPC;
		private readonly ModOptionToggle _showBirthdayIcon;

		public ShowBirthdayIcon(ModOptions modOptions)
		{

			_showBirthdayIcon = modOptions.GetOptionWithIdentifier<ModOptionToggle>(OptionKeys.ShowBirthdayIcon) ?? new ModOptionToggle(OptionKeys.ShowBirthdayIcon, "Show Birthday icon");
			_showBirthdayIcon.ValueChanged += ToggleOption;
			modOptions.AddModOption(_showBirthdayIcon);

			ToggleOption(_showBirthdayIcon.identifier, _showBirthdayIcon.IsOn);
		}

		public void ToggleOption(string identifier, bool showBirthdayIcon)
		{
			if (identifier != OptionKeys.ShowBirthdayIcon)
				return;

			TimeEvents.AfterDayStarted -= CheckForBirthday;
			GraphicsEvents.OnPreRenderHudEvent -= DrawBirthdayIcon;
			GameEvents.HalfSecondTick -= CheckIfGiftHasBeenGiven;

			if (showBirthdayIcon)
			{
				CheckForBirthday(null, null);
				TimeEvents.AfterDayStarted += CheckForBirthday;
				GraphicsEvents.OnPreRenderHudEvent += DrawBirthdayIcon;
				GameEvents.HalfSecondTick += CheckIfGiftHasBeenGiven;
			}
		}

		private void CheckIfGiftHasBeenGiven(object sender, EventArgs e)
		{
			if (_birthdayNPC != null)
			{
				var birthdayNPCDetails = Game1.player.friendships.SafeGet(_birthdayNPC.name);

				if (birthdayNPCDetails != null)
				{
					if (birthdayNPCDetails[3] == 1)
						_birthdayNPC = null;
				}
			}
		}

		public void Dispose()
		{
			ToggleOption(OptionKeys.ShowBirthdayIcon, false);
		}

		private void CheckForBirthday(object sender, EventArgs e)
		{
			_birthdayNPC = null;
			foreach (var location in Game1.locations)
			{
				foreach (var character in location.characters)
				{
					if (character.isBirthday(Game1.currentSeason, Game1.dayOfMonth))
					{
						_birthdayNPC = character;
						break;
					}
				}

				if (_birthdayNPC != null)
					break;
			}
		}

		private void DrawBirthdayIcon(object sender, EventArgs e)
		{
			var test = Game1.player.friendships;
			if (!Game1.eventUp)
			{
				if (_birthdayNPC != null)
				{
					Rectangle headShot = _birthdayNPC.GetHeadShot();
					Point iconPosition = IconHandler.Handler.GetNewIconPosition();
					float scale = 2.9f;

					Game1.spriteBatch.Draw(
							Game1.mouseCursors,
							new Vector2(iconPosition.X, iconPosition.Y),
							new Rectangle(228, 409, 16, 16),
							Color.White,
							0.0f,
							Vector2.Zero,
							scale,
							SpriteEffects.None,
							1f);

					ClickableTextureComponent texture =
							new ClickableTextureComponent(
									_birthdayNPC.name,
									new Rectangle(
											iconPosition.X - 7,
											iconPosition.Y - 2,
											(int) (16.0 * scale),
											(int) (16.0 * scale)),
									null,
									_birthdayNPC.name,
									_birthdayNPC.sprite.Texture,
									headShot,
									2f);

					texture.draw(Game1.spriteBatch);

					if (texture.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
					{
						String hoverText = String.Format("{0}'s Birthday", _birthdayNPC.name);
						IClickableMenu.drawHoverText(
								Game1.spriteBatch,
								hoverText,
								Game1.dialogueFont);
					}
				}
			}
		}
	}
}
