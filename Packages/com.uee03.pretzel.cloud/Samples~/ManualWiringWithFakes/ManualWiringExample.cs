using System;
using System.Threading;
using Pretzel.Cloud;
using Pretzel.Cloud.InMemory;
using Pretzel.Data;
using UnityEngine;

namespace Pretzel.Samples.Cloud.Fakes
{
    /// <summary>
    /// Wires every cloud interface to an in-memory fake and walks through sign-in, an offline save, a queued upload, Cloud Code and Remote Config.
    /// No Unity Gaming Services package, network or Dashboard is involved, so the same code runs in tests and in a game
    /// during offline development. Swap the fakes for the <c>UseUnity...</c> adapters in one place to go live.
    /// </summary>
    public sealed class ManualWiringExample : MonoBehaviour
    {
        private static readonly DataKey _profileKey = new("profile");

        private async void Start()
        {
            // Start is a Unity message, so this is the one place an async void is needed.
            try
            {
                await RunAsync(destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // The component was destroyed; nothing to report.
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
        }

        private async Awaitable RunAsync(CancellationToken cancellationToken)
        {
            // Composition root: build the fakes, put them in CloudServices, and depend only on the interfaces from here on.
            NewtonsoftSerializer serializer = new();
            InMemoryCloudAuthService auth = new("fake-player");
            InMemoryCloudSaveService save = new();
            InMemoryCloudCodeService code = new(serializer);
            InMemoryRemoteConfigService remoteConfig = new(serializer);
            code.RegisterModule<DoubleRequest, DoubleResponse>("Math", "Double", request => new DoubleResponse { result = request.value * 2 });
            remoteConfig.SetServerValue("welcome_message", "Hello from the fake server");

            CloudServices services = CloudServices.CreateBuilder(serializer)
                .UseAuth(auth)
                .UseSave(save)
                .UseCode(code)
                .UseRemoteConfig(remoteConfig)
                .Build();

            // The local store keeps the data in a temporary folder; the synced store queues what the fake cloud has not seen.
            DataStore local = new(DataRoots.TemporaryCache("ManualWiringWithFakes"));
            SyncedDataStoreOptions options = new() { FlushRetry = RetryPolicy.NoRetry() };
            SyncedDataStore store = services.CreateSyncedStore(local, options);
            store.StatusChanged += status => Debug.Log($"Sync status: {status}", this);

            CloudResult connected = await services.ConnectAsync(cancellationToken);
            Debug.Log($"Signed in: {connected.IsSuccess} as {services.Auth.PlayerId}", this);

            await store.SaveAsync(_profileKey, new ProfileData { displayName = "Ana", level = 2 }, cancellationToken);
            Debug.Log($"Cloud holds {save.Keys.Count} item(s) after an online save.", this);

            // Pull the plug: the device copy is still written, the change waits in the queue.
            save.Faults.Offline = true;
            await store.SaveAsync(_profileKey, new ProfileData { displayName = "Ana", level = 3 }, cancellationToken);
            Debug.Log($"Offline: {store.Status.State}, {store.PendingCount} change(s) queued, level on the device is {store.Load<ProfileData>(_profileKey).Value.level}.", this);

            save.Faults.Offline = false;
            SyncStatus flushed = await store.FlushAsync(cancellationToken);
            Debug.Log($"Back online: {flushed.State}, {flushed.PendingCount} queued.", this);

            CloudResult<DoubleResponse> doubled = await services.Code.CallModuleAsync<DoubleRequest, DoubleResponse>("Math", "Double", new DoubleRequest { value = 21 }, cancellationToken);
            Debug.Log($"Cloud Code Math.Double(21) = {doubled.Value.result}", this);

            await services.RemoteConfig.FetchAsync(null, null, cancellationToken);
            Debug.Log($"Remote Config: {services.RemoteConfig.GetString("welcome_message", "(default)")}", this);

            // Leave nothing behind in the temporary folder.
            await store.DeleteAsync(_profileKey, cancellationToken);
        }
    }
}
