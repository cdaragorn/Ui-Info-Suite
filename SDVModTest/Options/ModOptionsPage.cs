using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;

namespace UIInfoSuite.Options
{
    public class ModOptionsPage : IClickableMenu
    {
        private const int Width = 800;

        private List<ClickableComponent> _optionSlots = new List<ClickableComponent>();
        private List<ModOptionsElement> _options;
        private string _hoverText;
        private int _optionsSlotHeld;
        private int _currentItemIndex;
        private bool _isScrolling;
        private ClickableTextureComponent _upArrow;
        private ClickableTextureComponent _downArrow;
        private ClickableTextureComponent _scrollBar;
        private Rectangle _scrollBarRunner;

        public ModOptionsPage(List<ModOptionsElement> options, IModEvents events)
            : base(Game1.activeClickableMenu.xPositionOnScreen, Game1.activeClickableMenu.yPositionOnScreen + 10, Width, Game1.activeClickableMenu.height)
        {
            _options = options;
            _upArrow = new ClickableTextureComponent(
                new Rectangle(
                    xPositionOnScreen + width + Game1.tileSize / 4, 
                    yPositionOnScreen + Game1.tileSize, 
                    11 * Game1.pixelZoom, 
                    12 * Game1.pixelZoom), 
                Game1.mouseCursors, 
                new Rectangle(421, 459, 11, 12), 
                Game1.pixelZoom);

            _downArrow = new ClickableTextureComponent(
                new Rectangle(
                    _upArrow.bounds.X,
                    yPositionOnScreen + height - Game1.tileSize,
                    _upArrow.bounds.Width,
                    _upArrow.bounds.Height),
                Game1.mouseCursors,
                new Rectangle(421, 472, 11, 12),
                Game1.pixelZoom);

            _scrollBar = new ClickableTextureComponent(
                new Rectangle(
                    _upArrow.bounds.X + Game1.pixelZoom * 3,
                    _upArrow.bounds.Y + _upArrow.bounds.Height + Game1.pixelZoom,
                    6 * Game1.pixelZoom,
                    10 * Game1.pixelZoom),
                Game1.mouseCursors,
                new Rectangle(435, 463, 6, 10),
                Game1.pixelZoom);

            _scrollBarRunner = new Rectangle(_scrollBar.bounds.X,
                _scrollBar.bounds.Y,
                _scrollBar.bounds.Width,
                height - Game1.tileSize * 2 - _upArrow.bounds.Height - Game1.pixelZoom * 2);

            for (var i = 0; i < 7; ++i)
                _optionSlots.Add(new ClickableComponent(
                    new Rectangle(
                        xPositionOnScreen + Game1.tileSize / 4,
                        yPositionOnScreen + Game1.tileSize * 5 / 4 + Game1.pixelZoom + i * (height - Game1.tileSize * 2) / 7,
                        width - Game1.tileSize / 2,
                        (height - Game1.tileSize * 2) / 7 + Game1.pixelZoom),
                    i.ToString()));

            events.Display.MenuChanged += OnMenuChanged;
        }

        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is GameMenu)
            {
                xPositionOnScreen = Game1.activeClickableMenu.xPositionOnScreen;
                yPositionOnScreen = Game1.activeClickableMenu.yPositionOnScreen + 10;
                height = Game1.activeClickableMenu.height;

                for (var i = 0; i < _optionSlots.Count; ++i)
                {
                    var next = _optionSlots[i];
                    next.bounds.X = xPositionOnScreen + Game1.tileSize / 4;
                    next.bounds.Y = yPositionOnScreen + Game1.tileSize * 5 / 4 + Game1.pixelZoom + i * (height - Game1.tileSize * 2) / 7;
                    next.bounds.Width = width - Game1.tileSize / 2;
                    next.bounds.Height = (height - Game1.tileSize * 2) / 7 + Game1.pixelZoom;
                }

                _upArrow.bounds.X = xPositionOnScreen + width + Game1.tileSize / 4;
                _upArrow.bounds.Y = yPositionOnScreen + Game1.tileSize;
                _upArrow.bounds.Width = 11 * Game1.pixelZoom;
                _upArrow.bounds.Height = 12 * Game1.pixelZoom;

                _downArrow.bounds.X = _upArrow.bounds.X;
                _downArrow.bounds.Y = yPositionOnScreen + height - Game1.tileSize;
                _downArrow.bounds.Width = _upArrow.bounds.Width;
                _downArrow.bounds.Height = _upArrow.bounds.Height;

                _scrollBar.bounds.X = _upArrow.bounds.X + Game1.pixelZoom * 3;
                _scrollBar.bounds.Y = _upArrow.bounds.Y + _upArrow.bounds.Height + Game1.pixelZoom;
                _scrollBar.bounds.Width = 6 * Game1.pixelZoom;
                _scrollBar.bounds.Height = 10 * Game1.pixelZoom;

                _scrollBarRunner.X = _scrollBar.bounds.X;
                _scrollBarRunner.Y = _scrollBar.bounds.Y;
                _scrollBarRunner.Width = _scrollBar.bounds.Width;
                _scrollBarRunner.Height = height - Game1.tileSize * 2 - _upArrow.bounds.Height - Game1.pixelZoom * 2;
            }
        }

        private void SetScrollBarToCurrentItem()
        {
            if (_options.Count > 0)
            {
                _scrollBar.bounds.Y = _scrollBarRunner.Height / Math.Max(1, _options.Count - 7 + 1) * _currentItemIndex + _upArrow.bounds.Bottom + Game1.pixelZoom;

                if (_currentItemIndex == _options.Count - 7)
                {
                    _scrollBar.bounds.Y = _downArrow.bounds.Y - _scrollBar.bounds.Height - Game1.pixelZoom;
                }
            }
        }

        public override void leftClickHeld(int x, int y)
        {
            if (!GameMenu.forcePreventClose)
            {
                base.leftClickHeld(x, y);

                if (_isScrolling)
                {
                    var yBefore = _scrollBar.bounds.Y;

                    _scrollBar.bounds.Y = Math.Min(
                        yPositionOnScreen + height - Game1.tileSize - Game1.pixelZoom * 3 - _scrollBar.bounds.Height, 
                        Math.Max(
                            y, 
                            yPositionOnScreen + _upArrow.bounds.Height + Game1.pixelZoom * 5));

                    _currentItemIndex = Math.Min(
                        _options.Count - 7, 
                        Math.Max(
                            0, 
                            _options.Count * (y - _scrollBarRunner.Y) / _scrollBarRunner.Height));

                    SetScrollBarToCurrentItem();

                    if (yBefore != _scrollBar.bounds.Y)
                        Game1.playSound("shiny4");
                }
                else if (_optionsSlotHeld > -1 && _optionsSlotHeld + _currentItemIndex < _options.Count)
                {
                    _options[_currentItemIndex + _optionsSlotHeld].LeftClickHeld(
                        x - _optionSlots[_optionsSlotHeld].bounds.X,
                        y - _optionSlots[_optionsSlotHeld].bounds.Y);
                }
            }
        }

        public override void receiveKeyPress(Keys key)
        {
            if (_optionsSlotHeld > -1 &&
                _optionsSlotHeld + _currentItemIndex < _options.Count)
            {
                _options[_currentItemIndex + _optionsSlotHeld].ReceiveKeyPress(key);
            }
        }

        public override void receiveScrollWheelAction(int direction)
        {
            if (!GameMenu.forcePreventClose)
            {
                base.receiveScrollWheelAction(direction);

                if (direction > 0 && _currentItemIndex > 0)
                {
                    UpArrowPressed();
                    Game1.playSound("shiny4");
                }
                else if (direction < 0 && _currentItemIndex < Math.Max(0, _options.Count - 7))
                {
                    DownArrowPressed();
                    Game1.playSound("shiny4");
                }
            }
        }

        public override void releaseLeftClick(int x, int y)
        {
            if (!GameMenu.forcePreventClose)
            {
                base.releaseLeftClick(x, y);

                if (_optionsSlotHeld > -1 && _optionsSlotHeld + _currentItemIndex < _options.Count)
                {
                    var optionSlot = _optionSlots[_optionsSlotHeld];
                    _options[_currentItemIndex + _optionsSlotHeld].LeftClickReleased(x - optionSlot.bounds.X, y - optionSlot.bounds.Y);
                }
                _optionsSlotHeld = -1;
                _isScrolling = false;
            }
        }

        private void DownArrowPressed()
        {
            _downArrow.scale = _downArrow.baseScale;
            ++_currentItemIndex;
            SetScrollBarToCurrentItem();
        }

        private void UpArrowPressed()
        {
            _upArrow.scale = _upArrow.baseScale;
            --_currentItemIndex;
            SetScrollBarToCurrentItem();
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (!GameMenu.forcePreventClose)
            {
                if (_downArrow.containsPoint(x, y) && _currentItemIndex < Math.Max(0, _options.Count - 7))
                {
                    DownArrowPressed();
                    Game1.playSound("shwip");
                }
                else if (_upArrow.containsPoint(x, y) && _currentItemIndex > 0)
                {
                    UpArrowPressed();
                    Game1.playSound("shwip");
                }
                else if (_scrollBar.containsPoint(x, y))
                {
                    _isScrolling = true;
                }
                else if (!_downArrow.containsPoint(x, y) && 
                    x > xPositionOnScreen + width && 
                    x < xPositionOnScreen + width + Game1.tileSize * 2 &&
                    y > yPositionOnScreen &&
                    y < yPositionOnScreen + height)
                {
                    _isScrolling = true;
                    base.leftClickHeld(x, y);
                    base.releaseLeftClick(x, y);
                }
                _currentItemIndex = Math.Max(0, Math.Min(_options.Count - 7, _currentItemIndex));
                for (var i = 0; i < _optionSlots.Count; ++i)
                {
                    if (_optionSlots[i].bounds.Contains(x, y) && 
                        _currentItemIndex + i < _options.Count && 
                        _options[_currentItemIndex + i].Bounds.Contains(x - _optionSlots[i].bounds.X, y - _optionSlots[i].bounds.Y))
                    {
                        _options[_currentItemIndex + i].ReceiveLeftClick(
                            x - _optionSlots[i].bounds.X, 
                            y - _optionSlots[i].bounds.Y);
                        _optionsSlotHeld = i;
                        break;
                    }
                }
            }
        }


        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            
        }

        public override void receiveGamePadButton(Buttons b)
        {
            if (b == Buttons.A)
            {
                receiveLeftClick(Game1.getMouseX(), Game1.getMouseY());
            }
        }

        public override void performHoverAction(int x, int y)
        {
            if (!GameMenu.forcePreventClose)
            {
                _hoverText = "";
                _upArrow.tryHover(x, y);
                _downArrow.tryHover(x, y);
                _scrollBar.tryHover(x, y);
            }
        }

        public override void draw(SpriteBatch batch)
        {
            Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen - 10, width, height, false, true);
            batch.End();
            batch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null);
            for (var i = 0; i < _optionSlots.Count; ++i)
            {
                if (_currentItemIndex >= 0 &&
                    _currentItemIndex + i < _options.Count)
                {
                    _options[_currentItemIndex + i].Draw(
                        batch, 
                        _optionSlots[i].bounds.X, 
                        _optionSlots[i].bounds.Y);
                }
            }
            batch.End();
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            if (!GameMenu.forcePreventClose)
            {
                _upArrow.draw(batch);
                _downArrow.draw(batch);
                if (_options.Count > 7)
                {
                    drawTextureBox(
                        batch, 
                        Game1.mouseCursors, 
                        new Rectangle(403, 383, 6, 6), 
                        _scrollBarRunner.X, 
                        _scrollBarRunner.Y, 
                        _scrollBarRunner.Width, 
                        _scrollBarRunner.Height, 
                        Color.White, 
                        Game1.pixelZoom, 
                        false);
                    _scrollBar.draw(batch);
                }
            }
            if (_hoverText != "")
                drawHoverText(batch, _hoverText, Game1.smallFont);

            //if (Game1.options.hardwareCursor)
            //{
            //    Game1.spriteBatch.Draw(
            //        Game1.mouseCursors,
            //        new Vector2(
            //            Game1.getMouseX(),
            //            Game1.getMouseY()),
            //        new Rectangle?(
            //            Game1.getSourceRectForStandardTileSheet(
            //                Game1.mouseCursors,
            //                Game1.mouseCursor,
            //                16,
            //                16)),
            //        Color.White,
            //        0.0f,
            //        Vector2.Zero,
            //        (float)(Game1.pixelZoom + (Game1.dialogueButtonScale / 150.0)),
            //        SpriteEffects.None,
            //        1f);
            //}
        }
    }
}
