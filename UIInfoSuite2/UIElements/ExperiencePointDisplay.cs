using Microsoft.Xna.Framework;
using StardewValley;

namespace UIInfoSuite2.UIElements
{
    internal class ExperiencePointDisplay
    {
        private int _alpha = 100;
        private Vector2 _position;
        private readonly float _experiencePoints;

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
                Color.DarkSlateGray * (_alpha / 100f),
                Color.PaleTurquoise * (_alpha / 100f),
                Utility.ModifyCoordinatesForUIScale(new Vector2(_position.X - 28, _position.Y - 130)),
                0.0f,
                0.8f,
                0.0f);
        }

        public bool IsInvisible => _alpha < 3;
    }
}
