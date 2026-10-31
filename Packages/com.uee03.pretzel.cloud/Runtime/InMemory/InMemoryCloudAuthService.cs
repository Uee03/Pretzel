using System;
using System.Threading;
using Pretzel.Core;
using UnityEngine;

namespace Pretzel.Cloud.InMemory
{
    /// <summary>
    /// An in-memory <see cref="ICloudAuthService"/>: any sign-in succeeds and yields a fixed or generated player id.
    /// Like the real service it requires <see cref="InitializeAsync"/> first and a signed-in player to link.
    /// </summary>
    public sealed class InMemoryCloudAuthService : ICloudAuthService
    {
        private readonly string _playerId;
        private bool _initialized;
        private CloudAuthState _state;
        private string _currentPlayerId;

        public CloudFaults Faults { get; } = new();

        public CloudAuthState State => _state;

        public bool IsSignedIn => _state == CloudAuthState.SignedIn;

        public string PlayerId => IsSignedIn ? _currentPlayerId : null;

        public event Action<CloudAuthState> StateChanged;

        /// <param name="playerId">The id every sign-in yields. Defaults to a new GUID per service.</param>
        public InMemoryCloudAuthService(string playerId = null)
        {
            _playerId = string.IsNullOrEmpty(playerId) ? Guid.NewGuid().ToString("N") : playerId;
        }

        public async Awaitable<CloudResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            await Awaitable.MainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();
            CloudError fault = Faults.Next();
            if (fault != null)
            {
                return CloudResult.Failure(fault);
            }

            _initialized = true;
            return CloudResult.Success();
        }

        public async Awaitable<CloudResult> SignInAnonymouslyAsync(CancellationToken cancellationToken = default)
        {
            await Awaitable.MainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return SignIn(true);
        }

        public async Awaitable<CloudResult> SignInWithIdentityAsync(CloudIdentity identity, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(identity, nameof(identity));
            await Awaitable.MainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return SignIn(false);
        }

        public async Awaitable<CloudResult> SignInWithUsernamePasswordAsync(string username, string password, bool createAccount, CancellationToken cancellationToken = default)
        {
            Guard.NotNullOrEmpty(username, nameof(username));
            Guard.NotNullOrEmpty(password, nameof(password));
            await Awaitable.MainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return SignIn(false);
        }

        public async Awaitable<CloudResult> LinkIdentityAsync(CloudIdentity identity, bool forceLink = false, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(identity, nameof(identity));
            await Awaitable.MainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return RequireSignedIn();
        }

        public async Awaitable<CloudResult> AddUsernamePasswordAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            Guard.NotNullOrEmpty(username, nameof(username));
            Guard.NotNullOrEmpty(password, nameof(password));
            await Awaitable.MainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return RequireSignedIn();
        }

        public void SignOut(bool clearCredentials = false)
        {
            if (_state != CloudAuthState.SignedOut)
            {
                SetState(CloudAuthState.SignedOut);
            }
        }

        /// <summary>Simulates the session token expiring.</summary>
        public void Expire()
        {
            if (_state == CloudAuthState.SignedIn)
            {
                SetState(CloudAuthState.Expired);
            }
        }

        private CloudResult SignIn(bool anonymous)
        {
            CloudError fault = Faults.Next();
            if (fault != null)
            {
                return CloudResult.Failure(fault);
            }

            if (!_initialized)
            {
                return CloudResult.Failure(CloudErrorKind.NotInitialized, "Call InitializeAsync before signing in.");
            }

            if (anonymous && IsSignedIn)
            {
                return CloudResult.Success();
            }

            _currentPlayerId = _playerId;
            SetState(CloudAuthState.SigningIn);
            SetState(CloudAuthState.SignedIn);
            return CloudResult.Success();
        }

        private CloudResult RequireSignedIn()
        {
            CloudError fault = Faults.Next();
            if (fault != null)
            {
                return CloudResult.Failure(fault);
            }

            return IsSignedIn ? CloudResult.Success() : CloudResult.Failure(CloudErrorKind.NotSignedIn, "Sign in before linking an identity.");
        }

        private void SetState(CloudAuthState state)
        {
            _state = state;
            StateChanged?.Invoke(state);
        }
    }
}
