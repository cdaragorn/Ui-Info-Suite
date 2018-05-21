using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace UIInfoSuite.Extensions
{
    public static class ObjectExtensions
    {
        #region Memebers
        private static readonly Dictionary<String, int> _npcHeadShotSize = new Dictionary<string, int>()
        {
            { "Piere", 9 },
            { "Sebastian", 7 },
            { "Evelyn", 5 },
            { "Penny", 6 },
            { "Jas", 6 },
            { "Caroline", 5 },
            { "Dwarf", 5 },
            { "Sam", 9 },
            { "Maru", 6 },
            { "Wizard", 9 },
            { "Jodi", 7 },
            { "Krobus", 7 },
            { "Alex", 8 },
            { "Kent", 10 },
            { "Linus", 4 },
            { "Harvey", 9 },
            { "Shane", 8 },
            { "Haley", 6 },
            { "Robin", 7 },
            { "Marlon", 2 },
            { "Emily", 8 },
            { "Marnie", 5 },
            { "Abigail", 7 },
            { "Leah", 6 },
            { "George", 5 },
            { "Elliott", 9 },
            { "Gus", 7 },
            { "Lewis", 8 },
            { "Demetrius", 11 },
            { "Pam", 5 },
            { "Vincent", 6 },
            { "Sandy", 7 },
            { "Clint", 10 },
            { "Willy", 10 }
        };

        #endregion

        public static Rectangle GetHeadShot(this NPC npc)
        {
            int size;
            if (!_npcHeadShotSize.TryGetValue(npc.name, out size))
                size = 4;

            Rectangle mugShotSourceRect = npc.getMugShotSourceRect();
            mugShotSourceRect.Height -= size / 2;
            mugShotSourceRect.Y -= size / 2;
            return mugShotSourceRect;
        }

        public static String SafeGetString(this IModHelper helper, String key)
        {
            String result = string.Empty;

            if (!String.IsNullOrEmpty(key) &&
                helper != null)
            {
                result = helper.Translation.Get(key);
            }

            return result;
        }

        public static String SafeGetString(this ResourceManager manager, String key)
        {
            String result = string.Empty;

            if (!String.IsNullOrEmpty(key))
            {
                try
                {
                    result = manager.GetString(key, ModEntry.SpecificCulture);
                }
                catch
                {
                    try
                    {
                        result = Properties.Resources.ResourceManager.GetString(key);
                    }
                    catch
                    {

                    }
                }
            }

            return result ?? String.Empty;
        }
    }
}
