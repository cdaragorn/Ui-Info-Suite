### Shipping bin notes

[svw:Shipping]: https://stardewvalleywiki.com/Shipping

The "Full Shipment" achievement is awarded when the player has [shipped all items](svw:Shipping) in the "Items shipped (Farm & Forage)" tab of the Collections page (`CollectionsPage`) in the game menu.

The game tries to give the achievement to the player when it displays the shipping summary menu (`ShippingMenu`) during the night. It uses `Utility.hasFarmerShippedAllItems` to check if the requirements are met. This means that the "Full Shipment" achievement can only be earned on nights during which the player has shipped some items.

The "Full Shipment" achievement is achievement number 34. An item is shipped if it is present in thes `Game1.player.basicShipped` dictionary.

There are 4 points at which the shipping items are determined:
- In `Utility.getFarmerItemsShippedPercent` used by `ShippingMenu` via `Utility.hasFarmerShippedAllItems` to award the achievement
- In the constructor of `CollectionsPage` to show the basic shipped items tab of collections page of the game menu
- In `Utility.highlightShippableObjects` used by `ShippingBin.doAction` and `IslandWest.checkAction` to who shippable items when interacting with the shipping bin
- In `Object.countsForShippedCollection` used by `ShippingMenu` via `ShippingMenu.parseItems` to select which items we track the shipping count

**Utility.getFarmerItemsShippedPercent** goes through _Game1.objectInformation_ and keeps items that:
- Are not artifacts: `!text.Contains("Arch")`
- Are not fishing items: `!text.Contains("Fish")`
- Are not minerals: `!text.Contains("Mineral")`
- Are not gems (category): `!text.Substring(text.Length - 3).Equals("-2")` [note 1]
- Are not cooking items: `!text.Contains("Cooking")`
- Are not cooking (category): `!text.Substring(text.Length - 3).Equals("-7")` [note 1]
- And are basicShipped items (see below): `Object.isPotentialBasicShippedCategory(...)`

Where `text` is the 4th field of `ObjectInformation.json` representing the item type and category.

\[note 1]: That code seems to always return true if I'm not mistaken?

**CollectionsPage** goes through a sorted _Game1.objectInformation_ and keeps items that:
- Are not artifacts: `if (text.Contains("Arch"))`
- Are not fishing items: `else if (text.Contains("Fish"))`
- Are not minerals or gems (category): `else if (text.Contains("Mineral") || text.Substring(text.Length - 3).Equals("-2"))` [note 2]
- Are not cooking items or cooking (category): `else if (text.Contains("Cooking") || text.Substring(text.Length - 3).Equals("-7"))` [note 2]
- And are basicShipped items (see below): `if (!Object.isPotentialBasicShippedCategory(...))`

Where `text` is the 4th field of `ObjectInformation.json` representing the item type and category.

\[note 2]: The `text.Substring(...).Equals(...)` code seems to always return false if I'm not mistaken?

**Utility.highlightShippableObjects** calls `Object.canBeShipped` which keeps items that:
- Are objects: `if (i is Object)`
- Are not big craftables: `!this.bigCraftable`
- Have a type, which (usually) excludes items that aren't normal objects or big craftables: `this.type != null`
- Are not quest items: `!this.type.Equals("Quest")`
- Can be trashed: `this.canBeTrashed()`
- Are not furniture: `!(this is Furniture)`
- Are not wallpapers: `!(this is Wallpaper)`

Where `this` is an `StardewValley.Object`.

**Object.countsForShippedCollection** checks the object's information and keeps items that:
- Have a type, which (usually) excludes items that aren't normal objects or big craftables: `if (this.type == null ...)`
- Are not artifacts: `if (... this.type.Contains("Arch") ...)`
- Are not big craftables: `if (... (bool)this.bigCraftable)`
- Have item id 433 (Coffee Beam) or...
- Have a category: `case 0:`
- Are not in the following categories:
  - Seeds: `case -74:`
  - Equipment: `case -29:`
  - Furniture: `case -24:`
  - Tackle: `case -22:`
  - Bait: `case -21:`
  - Junk: `case -20:`
  - Fertilizer: `case -19:`
  - Meat: `case -14:`
  - Minerals: `case -12:`
  - Crafting: `case -8:`
  - Cooking: `case -7:`
  - Gem: `case -2:`
- Have an item index that is basicShipped (see below): `Object.isIndexOkForBasicShippedCategory`

**Object.isPotentialBasicShippedCategory** checks the object's category and keeps items that:
- Have item id 433 (Coffee Bean) or...
- Have a category `case 0:`
- Are not in the following categories:
  - Seeds: `case -74:`
  - Equipment: `case -29:`
  - Furniture: `case -24:`
  - Tackle: `case -22:`
  - Bait: `case -21:`
  - Junk: `case -20:`
  - Fertilizer: `case -19:`
  - Meat: `case -14:`
  - Minerals: `case -12:`
  - Crafting: `case -8:`
  - Cooking: `case -7:`
  - Gem: `case -2:`
- Have an item index that is basicShipped (see below): `Object.isIndexOkForBasicShippedCategory`

**Object.isIndexOkForBasicShippedCategory** checks that the object id:
  - Is not 434 (Stardrop)
  - Is not 889 (Qi Fruit)
  - Is not 928 (Golden Egg)

Other functions of note:
- **ShippingBin.shipItem** and **Farm.shipItem** adds the item to the shipping bin
- **Game1.player.shippedBasic** keeps track of shipped items statistic 
