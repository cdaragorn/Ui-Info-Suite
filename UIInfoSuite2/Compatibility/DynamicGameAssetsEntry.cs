
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;

namespace UIInfoSuite2.Compatibility
{
    /// <summary>Entrypoint for all things DGA</summary>
    public class DynamicGameAssetsEntry : IDisposable
    {
        private const string MOD_ID = "spacechase0.DynamicGameAssets"; 

        private IModHelper Helper { get; init; }
        private IMonitor Monitor { get; init; }

        public IDynamicGameAssetsApi? Api { get; private set; }
        private DynamicGameAssetsHelper? _dgaHelper;

        public bool IsLoaded { get; private set; }

        public DynamicGameAssetsEntry(IModHelper helper, IMonitor monitor)
        {
            this.Helper = helper;
            this.Monitor = monitor;

            this.Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        public void Dispose()
        {
            this.Helper.Events.GameLoop.GameLaunched -= OnGameLaunched;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // Check if DGA is loaded
            if (Helper.ModRegistry.IsLoaded(MOD_ID))
            {
                this.IsLoaded = true;

                // Get DGA's API
                var api = Helper.ModRegistry.GetApi<IDynamicGameAssetsApi>(MOD_ID);
                if (api != null)
                {
                    this.Api = api;
                    this._dgaHelper = new DynamicGameAssetsHelper(Api, Helper, Monitor);
                }
            }

        }

        /// <summary>Check if <paramref name="obj"/> is a DGA CustomCrop and provide a <see cref="DynamicGameAssetsHelper"/></summary>
        public bool IsCustomCrop(object obj, out DynamicGameAssetsHelper? dgaHelper)
        {
            dgaHelper = null;
            if (this.IsLoaded && obj.GetType().FullName == "DynamicGameAssets.Game.CustomCrop")
                dgaHelper = _dgaHelper?.InjectDga(obj);
            return dgaHelper != null;
        }


        /// <summary>Check if <paramref name="obj"/> is a DGA CustomObject and provide a <see cref="DynamicGameAssetsHelper"/></summary>        
        public bool IsCustomObject(object obj, out DynamicGameAssetsHelper? dgaHelper)
        {
            dgaHelper = null;
            if (this.IsLoaded && obj.GetType().FullName == "DynamicGameAssets.Game.CustomObject")
                dgaHelper = _dgaHelper?.InjectDga(obj);
            return dgaHelper != null;
        }
    }
}
