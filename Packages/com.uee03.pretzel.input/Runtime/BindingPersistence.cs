using System;
using UnityEngine.InputSystem;

namespace Pretzel.Inputs
{
    /// <summary>Loads, saves and resets the rebinds of an action asset through an <see cref="IBindingStore"/>.</summary>
    public static class BindingPersistence
    {
        /// <summary>Applies the saved overrides. Call it once at startup, before the actions are used.</summary>
        public static void Apply(InputActionAsset asset, IBindingStore store)
        {
            Guard(asset, store);
            string json = store.Load();
            if (!string.IsNullOrEmpty(json))
            {
                asset.LoadBindingOverridesFromJson(json);
            }
        }

        /// <summary>Saves the current overrides. Call it after a rebind completes.</summary>
        public static void Save(InputActionAsset asset, IBindingStore store)
        {
            Guard(asset, store);
            store.Save(asset.SaveBindingOverridesAsJson());
        }

        /// <summary>Restores every default binding and clears the saved overrides.</summary>
        public static void Reset(InputActionAsset asset, IBindingStore store)
        {
            Guard(asset, store);
            asset.RemoveAllBindingOverrides();
            store.Save(string.Empty);
        }

        private static void Guard(InputActionAsset asset, IBindingStore store)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }
        }
    }
}
