using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;

namespace UIInfoSuite.AdditionalFeatures
{
    public class SkipIntro
    {
        private readonly IModEvents _events;

        public SkipIntro(IModEvents events)
        {
            _events = events;

            events.Input.ButtonPressed += OnButtonPressed;
            events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        private void OnSaveLoaded(object sender, EventArgs e)
        {
            _events.Input.ButtonPressed -= OnButtonPressed;
            _events.GameLoop.SaveLoaded -= OnSaveLoaded;
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (Game1.activeClickableMenu is TitleMenu menu && e.Button == SButton.Escape)
            {
                menu.skipToTitleButtons();
                _events.Input.ButtonPressed -= OnButtonPressed;
            }
        }
    }
}
