using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewModdingAPI;
using System.IO;

namespace UIInfoSuite.UIElements
{

  class Components
  {

	public List<HoverComponent> list = new List<HoverComponent>();

	public Components(IModHelper helper)
	{
	  beachIcon = new IconComponent(helper.Content.Load<Texture2D>(Path.Combine("Resource", "LocationIcons.png")), SourceRects.beachIcon);
	  bugLandIcon = new IconComponent(helper.Content.Load<Texture2D>(Path.Combine("Resource", "LocationIcons.png")), SourceRects.bugLairIcon);
	  desertIcon = new IconComponent(helper.Content.Load<Texture2D>(Path.Combine("Resource", "LocationIcons.png")), SourceRects.desertIcon);
	  forestRiverIcon = new IconComponent(helper.Content.Load<Texture2D>(Path.Combine("Resource", "LocationIcons.png")), SourceRects.forestRiverIcon);
	  minesIcon = new IconComponent(helper.Content.Load<Texture2D>(Path.Combine("Resource", "LocationIcons.png")), SourceRects.minesIcon);
	  mountainIcon = new IconComponent(helper.Content.Load<Texture2D>(Path.Combine("Resource", "LocationIcons.png")), SourceRects.mountainIcon);
	  secretWoodsIcon = new IconComponent(helper.Content.Load<Texture2D>(Path.Combine("Resource", "LocationIcons.png")), SourceRects.secretWoodsIcon);
	  sewersIcon = new IconComponent(helper.Content.Load<Texture2D>(Path.Combine("Resource", "LocationIcons.png")), SourceRects.sewersIcon);
	  townIcon = new IconComponent(helper.Content.Load<Texture2D>(Path.Combine("Resource", "LocationIcons.png")), SourceRects.townIcon);
	  witchSwampIcon = new IconComponent(helper.Content.Load<Texture2D>(Path.Combine("Resource", "LocationIcons.png")), SourceRects.witchSwampIcon);
	  forestPondIcon = new IconComponent(helper.Content.Load<Texture2D>(Path.Combine("Resource", "LocationIcons.png")), SourceRects.forestPondIcon);
	  trapIcon = new IconComponent(helper.Content.Load<Texture2D>(Path.Combine("Resource", "LocationIcons.png")), SourceRects.trapIcon);

	  foreach (var component in this.GetType().GetFields())
	  {
		if (component.GetValue(this) as HoverComponent != null)
		  list.Add(component.GetValue(this) as HoverComponent);
	  }
	}

	public void Reset()
	{

	  list.ForEach(x =>
	  {
		x.hidden = true;
		if (x is TextComponent)
		  (x as TextComponent).text = "";
	  });
	  Background = new Rectangle();
	  titleBackground = new Rectangle();
	  seperator = new Rectangle();
	}

	public void HideAll()
	{
	  list.ForEach(x => { x.hidden = true; });
	}

	public void ExtendBackgroundWidth(params int[] sizes)
	{
	  foreach (int size in sizes)
	  {
		Background.Width = Math.Max(Background.Width, size);
	  }
	}

	public Rectangle Background = new Rectangle();
	public Rectangle titleBackground = new Rectangle();
	public Rectangle seperator = new Rectangle();
	public TextComponent title = new TextComponent(Game1.dialogueFont, Color.Black);
	public TextComponent category = new TextComponent(Game1.smallFont, Color.Black);
	public TextComponent description = new TextComponent(Game1.smallFont, Color.Black);
	public TextComponent healing = new TextComponent(Game1.smallFont, Color.Black);
	public IconComponent healingIcon = new IconComponent(SourceRects.healingIcon);
	public TextComponent energy = new TextComponent(Game1.smallFont, Color.Black);
	public IconComponent energyIcon = new IconComponent(SourceRects.energyIcon);
	public IconComponent bundleIcon = new IconComponent(SourceRects.bundleIcon);
	public TextComponent bundleName = new TextComponent(Game1.smallFont, Color.White);
	public TextComponent price = new TextComponent(Game1.smallFont, Color.Black);
	public TextComponent stackPrice = new TextComponent(Game1.smallFont, Color.Black);
	public TextComponent cropPrice = new TextComponent(Game1.smallFont, Color.Black);
	public TextComponent cropStackPrice = new TextComponent(Game1.smallFont, Color.Black);
	public IconComponent currencyIcon = new IconComponent(Game1.debrisSpriteSheet, SourceRects.currencyIcon);
	public IconComponent fishIcon = new IconComponent(SourceRects.fishIcon);
	public IconComponent rainyIcon = new IconComponent(SourceRects.rainIcon);
	public IconComponent sunnyIcon = new IconComponent(SourceRects.sunnyIcon);
	public TextComponent fishTimes = new TextComponent(Game1.smallFont, Color.Black);
	public IconComponent springIcon = new IconComponent(SourceRects.springIcon);
	public IconComponent summerIcon = new IconComponent(SourceRects.summerIcon);
	public IconComponent fallIcon = new IconComponent(SourceRects.fallIcon);
	public IconComponent winterIcon = new IconComponent(SourceRects.winterIcon);

	// Custom Location Icons by 4Slice
	public IconComponent beachIcon;
	public IconComponent bugLandIcon;
	public IconComponent desertIcon;
	public IconComponent forestRiverIcon;
	public IconComponent minesIcon;
	public IconComponent mountainIcon;
	public IconComponent secretWoodsIcon;
	public IconComponent sewersIcon;
	public IconComponent townIcon;
	public IconComponent witchSwampIcon;
	public IconComponent forestPondIcon;
	public IconComponent trapIcon;

  }
  // End of Class

  class HoverComponent
  {
	public bool hidden = true;
	public virtual int Height { get; }
	public virtual int Width { get; }
  }

  class TextComponent: HoverComponent
  {

	// scale is not used yet
	public TextComponent(SpriteFont font, Color color, float scale = 1f)
	{
	  this.font = font;
	  this.color = color;
	  this.scale = scale;
	}

	public void Set(string text, SpriteFont font, Color color, float scale = 1f)
	{
	  this.text = text;
	  this.font = font;
	  this.color = color;
	  this.scale = scale;
	}

	public void draw(SpriteBatch b, Vector2 location)
	{
	  b.DrawString(this.font, this.text, location, this.color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.88f);
	}

	private Vector2 size => font.MeasureString(text);
	public override int Height => (int) (size.Y * scale);
	public override int Width => (int) (size.X * scale);

	public int Length => text.Length;

	public SpriteFont font = null;
	public string text = null;
	public Color color = Color.Black;
	public float scale = 1f;
  }

  class IconComponent: HoverComponent
  {

	public IconComponent(Rectangle source, float scale = 1f)
	{
	  this.source = source;
	  this.scale = scale;
	}

	public IconComponent(Texture2D sheet, Rectangle source, float scale = 1f)
	{
	  this.source = source;
	  this.scale = scale;
	  this.sheet = sheet;
	}

	public void drawWithShadow(SpriteBatch b, Vector2 location)
	{
	  Utility.drawWithShadow(b, sheet, location, this.source, Color.White, 0f, Vector2.Zero, Game1.pixelZoom * scale);

	}

	public void draw(SpriteBatch b, Vector2 location)
	{
	  b.Draw(sheet, location, this.source, Color.White, 0f, Vector2.Zero, Game1.pixelZoom * scale, SpriteEffects.None, 0.88f);
	}

	public Rectangle source = new Rectangle();
	public float scale = 1f;
	private Texture2D sheet = Game1.mouseCursors;

	public override int Height => (int) (source.Height * Game1.pixelZoom * scale);
	public override int Width => (int) (source.Width * Game1.pixelZoom * scale);

  }
}
