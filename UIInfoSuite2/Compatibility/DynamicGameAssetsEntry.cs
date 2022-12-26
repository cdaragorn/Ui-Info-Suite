
using StardewModdingAPI;

namespace UIInfoSuite2.Compatibility
{
    /// Entrypoint for all things DGA
    public class DynamicGameAssetsEntry
    {
        private IModHelper Helper { get; init; }
        private IMonitor Monitor { get; init; }

        public IDynamicGameAssetsApi? Api { get; private set; }
        private DynamicGameAssetsHelper? _dgaHelper;

        public DynamicGameAssetsEntry(IModHelper helper, IMonitor monitor)
        {
            this.Helper = helper;
            this.Monitor = monitor;
        }

        public void SetApi(IDynamicGameAssetsApi api)
        {
            this.Api = api;
            this._dgaHelper = new DynamicGameAssetsHelper(Api, Helper, Monitor);
        }

        public bool IsCustomCrop(object obj, out DynamicGameAssetsHelper? dgaHelper)
        {
            dgaHelper = null;
            if (obj.GetType().FullName == "DynamicGameAssets.Game.CustomCrop")
                return GetDgaHelper(obj, out dgaHelper);
            return false;
        }
        
        public bool IsCustomObject(object obj, out DynamicGameAssetsHelper? dgaHelper)
        {
            dgaHelper = null;
            if (obj.GetType().FullName == "DynamicGameAssets.Game.CustomObject")
                return GetDgaHelper(obj, out dgaHelper);
            return false;
        }

        private bool GetDgaHelper(object obj, out DynamicGameAssetsHelper? dgaHelper)
        {
            dgaHelper = _dgaHelper;
            if (_dgaHelper == null)
                return false;
            
            _dgaHelper.SupplyDga(obj);
            return true;
        }
    }
}
