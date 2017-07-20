using Microsoft.Xna.Framework.Input;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIInfoSuite.UIElements
{
    class SkipIntro
    {
        //private bool _skipIntro = false;

        public SkipIntro()
        {
            //GameEvents.QuarterSecondTick += CheckForSkip;
            ControlEvents.KeyPressed += ControlEvents_KeyPressed;
            SaveEvents.AfterLoad += StopCheckingForSkipKey;
            //MenuEvents.MenuChanged += SkipToTitleButtons;
        }

        private void StopCheckingForSkipKey(object sender, EventArgs e)
        {
            ControlEvents.KeyPressed -= ControlEvents_KeyPressed;
            SaveEvents.AfterLoad -= StopCheckingForSkipKey;
        }

        private void ControlEvents_KeyPressed(object sender, EventArgsKeyPressed e)
        {
            if (Game1.activeClickableMenu is TitleMenu &&
                e.KeyPressed == Keys.Escape)
            {
                (Game1.activeClickableMenu as TitleMenu)?.skipToTitleButtons();
                ControlEvents.KeyPressed -= ControlEvents_KeyPressed;
            }
        }

        //private void CheckForSkip(object sender, EventArgs e)
        //{
        //    if (Game1.activeClickableMenu is TitleMenu &&
        //        _skipIntro)
        //    {
        //        _skipIntro = false;
        //        (Game1.activeClickableMenu as TitleMenu)?.skipToTitleButtons();
        //    }
        //}

        //private void SkipToTitleButtons(object sender, EventArgsClickableMenuChanged e)
        //{
        //    TitleMenu menu = e.NewMenu as TitleMenu;
        //    menu?.skipToTitleButtons();
        //    //MenuEvents.MenuChanged -= SkipToTitleButtons;
        //}
    }
}
