
using StardewModdingAPI;

namespace UIInfoSuite2.Compatibility
{
    /// <summary>Entrypoint for all things DGA</summary>
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

        /// <summary>Inject the DGA API which allows DGAHelper to be inialized</summary>
        public void InjectApi(IDynamicGameAssetsApi api)
        {
            if (this.Api == null)
            {
                this.Api = api;
                this._dgaHelper = new DynamicGameAssetsHelper(Api, Helper, Monitor);
            }
        }

        /// <summary>Check if <paramref name="obj"/> is a DGA CustomCrop and provide a <see cref="DynamicGameAssetsHelper"/></summary>
        public bool IsCustomCrop(object obj, out DynamicGameAssetsHelper? dgaHelper)
        {
            dgaHelper = null;
            if (obj.GetType().FullName == "DynamicGameAssets.Game.CustomCrop")
                dgaHelper = GetDgaHelper(obj);
            return dgaHelper != null;
        }


        /// <summary>Check if <paramref name="obj"/> is a DGA CustomObject and provide a <see cref="DynamicGameAssetsHelper"/></summary>        
        public bool IsCustomObject(object obj, out DynamicGameAssetsHelper? dgaHelper)
        {
            dgaHelper = null;
            if (obj.GetType().FullName == "DynamicGameAssets.Game.CustomObject")
                dgaHelper = GetDgaHelper(obj);
            return dgaHelper != null;
        }

        /// <returns>null if <see cref="_dgaHelper"/> is null</returns>
        private DynamicGameAssetsHelper? GetDgaHelper(object obj)
        {
            _dgaHelper?.InjectDga(obj);
            return _dgaHelper;
        }
    }
}
