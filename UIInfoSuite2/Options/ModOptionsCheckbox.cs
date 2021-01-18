using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using UIInfoSuite.Infrastructure.Extensions;

namespace UIInfoSuite.Options
{
    class ModOptionsCheckbox : ModOptionsElement
    {
        private readonly Action<bool> _toggleOptionsDelegate;
        private bool _isChecked;
        private readonly IDictionary<string, string> _options;
        private readonly string _optionKey;
        private bool _canClick => !(_parent is ModOptionsCheckbox) || (_parent as ModOptionsCheckbox)._isChecked;

        public ModOptionsCheckbox(
            string label,
            int whichOption,
            Action<bool> toggleOptionDelegate,
            IDictionary<string, string> options,
            string optionKey,
            ModOptionsCheckbox parent = null)
            : base(label, whichOption, parent)
        {
            _toggleOptionsDelegate = toggleOptionDelegate;
            _options = options;
            _optionKey = optionKey;

            if (!_options.ContainsKey(_optionKey))
                _options[_optionKey] = "true";

            _isChecked = _options[_optionKey].SafeParseBool();
            _toggleOptionsDelegate(_isChecked);
        }

        public override void ReceiveLeftClick(int x, int y)
        {
            if (_canClick)
            {
                Game1.playSound("drumkit6");
                base.ReceiveLeftClick(x, y);
                _isChecked = !_isChecked;
                _options[_optionKey] = _isChecked.ToString();
                _toggleOptionsDelegate(_isChecked);
            }
        }

        public override void Draw(SpriteBatch batch, int slotX, int slotY)
        {
            batch.Draw(Game1.mouseCursors, new Vector2(slotX + Bounds.X, slotY + Bounds.Y), new Rectangle?(_isChecked ? OptionsCheckbox.sourceRectChecked : OptionsCheckbox.sourceRectUnchecked), Color.White * (_canClick ? 1f : 0.33f), 0.0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.4f);
            base.Draw(batch, slotX, slotY);
        }
    }
}
