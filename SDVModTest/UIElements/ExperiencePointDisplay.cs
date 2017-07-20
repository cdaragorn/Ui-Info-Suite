using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIInfoSuite.UIElements
{
    class ExperiencePointDisplay
    {
        private int _alpha = 100;
        private float _experiencePoints;
        private Vector2 _position;

        public ExperiencePointDisplay(float experiencePoints, Vector2 position)
        {
            _position = position;
            _experiencePoints = experiencePoints;
        }

        public void Draw()
        {
            _position.Y -= 0.5f;
            --_alpha;
            Game1.drawWithBorder(
                "Exp " + _experiencePoints,
                Color.DarkSlateGray * ((float)_alpha / 100f),
                Color.PaleTurquoise * ((float)_alpha / 100f),
                new Vector2(_position.X - 28, _position.Y - 130),
                0.0f,
                0.8f,
                0.0f);
        }

        public bool IsInvisible
        {
            get { return _alpha < 3; }
        }
    }
}
