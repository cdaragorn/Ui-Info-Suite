using System;
using System.Collections;
using StardewValley;
using UIInfoSuite2.Infrastructure.Extensions;
using StardewModdingAPI;
using SObject = StardewValley.Object;

namespace UIInfoSuite2.Compatibility
{
    public class DynamicGameAssetsHelper
    {
        public IDynamicGameAssetsApi Api { get; init; }
        private IReflectionHelper Reflection { get; init; }

        private IReflectedMethod? modFindMethod;

        public DynamicGameAssetsHelper(IDynamicGameAssetsApi api, IModHelper helper)
        {
            this.Api = api;
            this.Reflection = helper.Reflection;
        }

        public bool isDgaType(object obj)
        {
            return obj.GetType().FullName?.StartsWith("DynamicGameAssets.") == true;
        }

        /// dga is an object of any type within the DynamicGameAssets assembly
        public object? FindPackData(object dga, string fullId, bool checkType = true)
        {
            if (modFindMethod == null)
            {
                if (dga.GetType().FullName?.StartsWith("DynamicGameAssets.") != true)
                    throw new ArgumentException(nameof(dga));

                string dgaAQName = dga.GetType().AssemblyQualifiedName!;
                string modAQName = "DynamicGameAssets.Mod" + dgaAQName.Substring(dgaAQName.IndexOf(','));
                modFindMethod = Reflection.GetMethod(Type.GetType(modAQName)!, "Find");
            }

            return modFindMethod.Invoke<object?>(fullId);
        }

        public object? GetCropData(object customCrop, bool checkType = true)
        {
            if (checkType && customCrop.GetType().FullName != "DynamicGameAssets.Game.CustomCrop")
                throw new ArgumentException(nameof(customCrop));

            return Reflection.GetPropertyGetter<object?>(customCrop, "Data").GetValue();
        }

        public string? GetFullId(object dgaItem, bool checkType = true)
        {
            if (checkType && dgaItem.GetType().BaseType?.FullName == "DynamicGameAssets.Game.IDGAItem")
                throw new ArgumentException(nameof(dgaItem));

            return Reflection.GetProperty<string?>(dgaItem, "FullId").GetValue();
        }

        public SObject? GetCropHarvest(object customCrop, bool checkType = true)
        {
            if (checkType && customCrop.GetType().FullName != "DynamicGameAssets.Game.CustomCrop")
                throw new ArgumentException(nameof(customCrop));

            var cropData = this.GetCropData(customCrop, checkType: false);
            if (cropData == null)
                return null;
            
            return this.GetCropPackHarvest(cropData);
        }

        public SObject? GetSeedsHarvest(Item item, bool checkType = true)
        {
            if (checkType && item.GetType().FullName != "DynamicGameAssets.Game.CustomObject")
                throw new ArgumentException(nameof(item));
            
            if (!(item is StardewValley.Object seedsObject && seedsObject.Category == StardewValley.Object.SeedsCategory))
                return null;

            var itemData = Reflection.GetPropertyGetter<object?>(item, "Data").GetValue();
            if (itemData == null)
                return null;
            
            string? itemPlants = Reflection.GetProperty<string?>(itemData, "Plants").GetValue();
            if (itemPlants == null)
                return null;

            var cropData = this.FindPackData(item, itemPlants, checkType: checkType);
            if (cropData == null)
                return null;

            return this.GetCropPackHarvest(cropData);
        }

        public SObject? GetCropPackHarvest(object cropData, bool checkType = true)
        {
            if (checkType && cropData.GetType().FullName != "DynamicGameAssets.PackData.CropPackData")
                throw new ArgumentException(nameof(cropData));

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
                return null;
            if (harvestDrops.Count > 1)
                throw new Exception("DGA crops with multiple drops on the last harvest are not supported");

            var possibleDrops = Reflection.GetPropertyGetter<IList>(harvestDrops[0]!, "Item").GetValue();
            if (possibleDrops.Count != 1)
                throw new Exception("DGA crops with random drops are not supported");

            var dropItem = Reflection.GetPropertyGetter<object?>(possibleDrops[0]!, "Value").GetValue()!;
            string dropItemType = Reflection.GetPropertyGetter<Enum>(dropItem, "Type").GetValue()!.ToString()!;
            string dropItemValue = Reflection.GetPropertyGetter<string?>(dropItem, "Value").GetValue()!;
            
            if (dropItemType == "DGAItem")
                return (StardewValley.Object) this.Api.SpawnDGAItem(dropItemValue);
            else if (dropItemType == "VanillaItem")
                return new StardewValley.Object(int.Parse(dropItemValue), 1);
            else
                throw new Exception("Harvest types other than DGAItem and VanillaItem are not supported");
        }
    }
}
