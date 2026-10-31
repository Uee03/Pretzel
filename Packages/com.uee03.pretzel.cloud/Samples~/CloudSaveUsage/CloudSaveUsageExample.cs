using System;
using System.Threading;
using Pretzel.Cloud;
using Pretzel.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.Cloud.SaveUsage
{
    /// <summary>
    /// Signs in anonymously, loads the player profile through an offline-first <see cref="SyncedDataStore"/> and keeps it synced.
    /// Assign a UIDocument that has a PanelSettings asset to get a small status panel; without one the same information goes to the Console.
    /// The composition root is <see cref="RunAsync"/>: nothing here is static and nothing is a singleton.
    /// </summary>
    public sealed class CloudSaveUsageExample : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        [SerializeField] private float _syncIntervalSeconds = 30f;

        private static readonly DataKey _profileKey = new("profile");

        private CloudServices _services;
        private SyncedDataStore _store;
        private PlayerProfile _profile = new();
        private Label _statusLabel;
        private Label _profileLabel;

        private async void Start()
        {
            // Start is a Unity message, so this is the one place an async void is needed. Everything below returns an Awaitable.
            await ObserveAsync(RunAsync);
        }

        private void OnDestroy()
        {
            if (_store != null)
            {
                _store.StatusChanged -= OnStatusChanged;
            }
        }

        private async Awaitable RunAsync(CancellationToken cancellationToken)
        {
            // The local store always holds the data. The synced store queues uploads and retries them.
            _services = CloudServices.CreateBuilder()
                .UseUnityAuthentication()
                .UseUnityCloudSave()
                .Build();
            DataStore local = new(DataRoots.PersistentData("CloudSaveUsage"));
            _store = _services.CreateSyncedStore(local);
            _store.StatusChanged += OnStatusChanged;
            BuildUi();
            OnStatusChanged(_store.Status);

            // Offline is fine: the profile loads from the device and uploads once the connection and sign-in succeed.
            await TryConnectAsync(cancellationToken);

            LoadResult<PlayerProfile> loaded = await _store.LoadAsync<PlayerProfile>(_profileKey, cancellationToken);
            if (loaded.Ok)
            {
                _profile = loaded.Value;
            }

            ShowProfile();

            while (true)
            {
                await Awaitable.WaitForSecondsAsync(_syncIntervalSeconds, cancellationToken);
                if (!_services.Auth.IsSignedIn)
                {
                    await TryConnectAsync(cancellationToken);
                }

                await _store.FlushAsync(cancellationToken);
            }
        }

        private async Awaitable TryConnectAsync(CancellationToken cancellationToken)
        {
            CloudResult connected = await _services.ConnectAsync(cancellationToken);
            if (connected.IsSuccess)
            {
                Debug.Log($"Signed in as {_services.Auth.PlayerId}.", this);
            }
            else
            {
                Debug.LogWarning($"Working offline: {connected.Error}", this);
            }
        }

        private Awaitable LevelUpAsync(CancellationToken cancellationToken)
        {
            _profile.level++;
            _profile.coins += 10;
            ShowProfile();

            // The device copy is written first. A failed upload never throws; it shows in the status and stays queued.
            return _store.SaveAsync(_profileKey, _profile, cancellationToken);
        }

        private async Awaitable ReloadAsync(CancellationToken cancellationToken)
        {
            LoadResult<PlayerProfile> loaded = await _store.LoadAsync<PlayerProfile>(_profileKey, cancellationToken);
            if (loaded.Ok)
            {
                _profile = loaded.Value;
            }

            ShowProfile();
        }

        private async Awaitable SyncNowAsync(CancellationToken cancellationToken)
        {
            await _store.FlushAsync(cancellationToken);
        }

        private async Awaitable SignOutAsync(CancellationToken cancellationToken)
        {
            await _store.FlushAsync(cancellationToken);
            _services.Auth.SignOut(clearCredentials: false);
        }

        private void OnStatusChanged(SyncStatus status)
        {
            string text = $"Sync: {status.State}, queued {status.PendingCount}";
            if (status.LastError != null)
            {
                text += $" ({status.LastError.Kind})";
            }

            if (_statusLabel != null)
            {
                _statusLabel.text = text;
            }
            else
            {
                Debug.Log(text, this);
            }
        }

        private void ShowProfile()
        {
            string text = $"{_profile.displayName}: level {_profile.level}, {_profile.coins} coins";
            if (_profileLabel != null)
            {
                _profileLabel.text = text;
            }
            else
            {
                Debug.Log(text, this);
            }
        }

        private void BuildUi()
        {
            if (_document == null || _document.panelSettings == null)
            {
                return;
            }

            VisualElement root = _document.rootVisualElement;
            _statusLabel = new Label();
            _profileLabel = new Label();
            root.Add(_statusLabel);
            root.Add(_profileLabel);
            root.Add(CreateButton("Level up and save", LevelUpAsync));
            root.Add(CreateButton("Reload", ReloadAsync));
            root.Add(CreateButton("Sync now", SyncNowAsync));
            root.Add(CreateButton("Sign out", SignOutAsync));
        }

        private Button CreateButton(string text, Func<CancellationToken, Awaitable> action)
        {
            Button button = new() { text = text };
            button.clicked += () => FireAsync(action);
            return button;
        }

        // A UI event is the other required async void boundary; it observes the result so no exception goes unobserved.
        private async void FireAsync(Func<CancellationToken, Awaitable> action)
        {
            await ObserveAsync(action);
        }

        private async Awaitable ObserveAsync(Func<CancellationToken, Awaitable> action)
        {
            try
            {
                await action(destroyCancellationToken);
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
    }
}
