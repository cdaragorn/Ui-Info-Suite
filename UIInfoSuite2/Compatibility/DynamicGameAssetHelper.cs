using System;
using System.Collections;
using StardewValley;
using UIInfoSuite2.Infrastructure.Extensions;
using StardewModdingAPI;

namespace UIInfoSuite2.Compatibility
{
    public class DynamicGameAssetsHelper
    {
        public IDynamicGameAssetsApi Api { get; init; }
        private IReflectionHelper Reflection;

        public DynamicGameAssetsHelper(IDynamicGameAssetsApi api, IModHelper helper)
        {
            this.Api = api;
            this.Reflection = helper.Reflection;
        }

        public int GetHarvestPrice(Item item)
        {
            // Code without reflection:
            // 
            //   string? itemPlants = item.Data?.Plants;
            //   if (itemPlants == null)
            //       return 0;
            //
            //   CropPackData cropData = DynamicGameAssets.Mod.Find(itemPlants);
            //   
            //   List<HarvestedDropData>? harvestDrops = null;
            //   foreach (var phase in cropData.Phases)
            //   {
            //       List<HarvestedDropData> phaseDrops = phase.HarvestedDrops;
            //       if (phaseDrops!.Count > 0)
            //           harvestDrops = phaseDrops;
            //   }
            //   if (harvestDrops!.Count > 1)
            //      throw new Exception("DGA crops with multiple drops on the last harvest are not supported");
            //
            //   var possibleDrops = harvestDrops[0].Item;
            //   if (possibleDrops.Count != 1)
            //       throw new Exception("DGA crops with random drops are not supported");
            //
            //   ItemAbstraction dropItem = possibleDrops[0].Value;
            //   string dropItemType = dropItem.Type.ToString();
            //   string dropItemValue = dropItem.Value;
            //
            // Code with SMAPI-like reflection:

            string? itemPlants = Reflection.GetProperty<string?>(Reflection.GetPropertyGetter<object?>(item, "Data").GetValue()!, "Plants").GetValue();
            if (itemPlants == null)
                return 0;

            string itemAQName = item.GetType().AssemblyQualifiedName!; // eg. "DynamicGameAssets.Game.CustomObject, DynamicGameAssets, ..."
            string modAQName = "DynamicGameAssets.Mod" + itemAQName.Substring(itemAQName.IndexOf(','));
            var cropData = Reflection.GetMethod(Type.GetType(modAQName)!, "Find").Invoke<object?>(itemPlants)!;

            var cropPhases = Reflection.GetPropertyGetter<IList>(cropData, "Phases").GetValue();

            // Find the last phase that has a harvest drop
            IList? harvestDrops = null;
            foreach (var phase in cropPhases)
            {
                var phaseDrops = Reflection.GetPropertyGetter<IList>(phase!, "HarvestedDrops").GetValue()!;
                if (phaseDrops.Count > 0)
                    harvestDrops = phaseDrops;
            }
            if (harvestDrops == null)
                return 0;
            if (harvestDrops.Count > 1)
                throw new Exception("DGA crops with multiple drops on the last harvest are not supported");

            var possibleDrops = Reflection.GetPropertyGetter<IList>(harvestDrops[0]!, "Item").GetValue();
            if (possibleDrops.Count != 1)
                throw new Exception("DGA crops with random drops are not supported");

            var dropItem = Reflection.GetPropertyGetter<object?>(possibleDrops[0]!, "Value").GetValue()!;
            string dropItemType = Reflection.GetPropertyGetter<Enum>(dropItem, "Type").GetValue()!.ToString()!;
            string dropItemValue = Reflection.GetPropertyGetter<string?>(dropItem, "Value").GetValue()!;
            // End of SMAPI-like reflection code.
            
            if (dropItemType == "DGAItem")
            {
                var drop = this.Api.SpawnDGAItem(dropItemValue);
                return Reflection.GetMethod(drop, "sellToStorePrice").Invoke<int>(Type.Missing);
            }
            else if (dropItemType == "VanillaItem")
            {
                return new StardewValley.Object(int.Parse(dropItemValue), 1).sellToStorePrice();
            }
            else
            {
                throw new Exception("Harvest types other than DGAItem and VanillaItem are not supported");
            }
        }
    }
}
