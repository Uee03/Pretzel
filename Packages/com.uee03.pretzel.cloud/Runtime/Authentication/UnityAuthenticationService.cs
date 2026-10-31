using System;
using System.Threading;
using System.Threading.Tasks;
using Pretzel.Core;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

namespace Pretzel.Cloud.Authentication
{
    /// <summary>
    /// <see cref="ICloudAuthService"/> on Unity Authentication. <see cref="InitializeAsync"/> starts Unity Services (Play Mode only)
    /// and must succeed before any sign-in. The SDK calls take no cancellation token, so cancelling stops the wait but not the request.
    /// Dispose it to unsubscribe from the SDK events. Use from the main thread.
    /// </summary>
    public sealed class UnityAuthenticationService : ICloudAuthService, IDisposable
    {
        private readonly InitializationOptions _options;
        private IAuthenticationService _auth;
        private CloudAuthState _published;

        public CloudAuthState State => _auth == null ? CloudAuthState.SignedOut : Map(_auth.State);

        public bool IsSignedIn => _auth != null && _auth.IsSignedIn;

        public string PlayerId => IsSignedIn ? _auth.PlayerId : null;

        public event Action<CloudAuthState> StateChanged;

        /// <param name="options">Passed to <c>UnityServices.InitializeAsync</c>, for example a profile or environment. Null for the defaults.</param>
        public UnityAuthenticationService(InitializationOptions options = null)
        {
            _options = options;
        }

        /// <param name="profile">A local scope for the cached sign-in, to test several players on one machine. Null or empty for the default profile.</param>
        /// <param name="environment">The Unity Services environment name, for example "production". Null or empty for the default environment.</param>
        public UnityAuthenticationService(string profile, string environment)
        {
            _options = new InitializationOptions();
            if (!string.IsNullOrEmpty(profile))
            {
                _options.SetProfile(profile);
            }

            if (!string.IsNullOrEmpty(environment))
            {
                _options.SetEnvironmentName(environment);
            }
        }

        public async Awaitable<CloudResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    await UnityServices.InitializeAsync(_options ?? new InitializationOptions()).WithCancellationAsync(cancellationToken);
                }

                Attach(AuthenticationService.Instance);
                return CloudResult.Success();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return CloudResult.Failure(UnityAuthenticationErrors.Map(ex));
            }
        }

        public async Awaitable<CloudResult> SignInAnonymouslyAsync(CancellationToken cancellationToken = default)
        {
            if (IsSignedIn)
            {
                return CloudResult.Success();
            }

            return await RunAsync(auth => auth.SignInAnonymouslyAsync(), cancellationToken);
        }

        public async Awaitable<CloudResult> SignInWithIdentityAsync(CloudIdentity identity, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(identity, nameof(identity));
            if (!HasSecondary(identity))
            {
                return MissingSecondary(identity);
            }

            return await RunAsync(auth => SignInAsync(auth, identity), cancellationToken);
        }

        public async Awaitable<CloudResult> SignInWithUsernamePasswordAsync(string username, string password, bool createAccount, CancellationToken cancellationToken = default)
        {
            Guard.NotNullOrEmpty(username, nameof(username));
            Guard.NotNullOrEmpty(password, nameof(password));
            return await RunAsync(auth => createAccount ? auth.SignUpWithUsernamePasswordAsync(username, password) : auth.SignInWithUsernamePasswordAsync(username, password), cancellationToken);
        }

        public async Awaitable<CloudResult> LinkIdentityAsync(CloudIdentity identity, bool forceLink = false, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(identity, nameof(identity));
            if (!HasSecondary(identity))
            {
                return MissingSecondary(identity);
            }

            LinkOptions options = new() { ForceLink = forceLink };
            return await RunAsync(auth => LinkAsync(auth, identity, options), cancellationToken);
        }

        public async Awaitable<CloudResult> AddUsernamePasswordAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            Guard.NotNullOrEmpty(username, nameof(username));
            Guard.NotNullOrEmpty(password, nameof(password));
            return await RunAsync(auth => auth.AddUsernamePasswordAsync(username, password), cancellationToken);
        }

        public void SignOut(bool clearCredentials = false)
        {
            if (_auth != null)
            {
                _auth.SignOut(clearCredentials);
                Publish();
            }
        }

        public void Dispose()
        {
            Detach();
        }

        private async Awaitable<CloudResult> RunAsync(Func<IAuthenticationService, Task> call, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_auth == null)
            {
                return CloudResult.Failure(CloudErrorKind.NotInitialized, "Call InitializeAsync before signing in.");
            }

            try
            {
                Task task = call(_auth);
                Publish();
                await task.WithCancellationAsync(cancellationToken);
                return CloudResult.Success();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return CloudResult.Failure(UnityAuthenticationErrors.Map(ex));
            }
            finally
            {
                Publish();
            }
        }

        private static Task SignInAsync(IAuthenticationService auth, CloudIdentity identity)
        {
            switch (identity.Provider)
            {
                case CloudIdentityProvider.Apple:
                    return auth.SignInWithAppleAsync(identity.Token);
                case CloudIdentityProvider.Google:
                    return auth.SignInWithGoogleAsync(identity.Token);
                case CloudIdentityProvider.GooglePlayGames:
                    return auth.SignInWithGooglePlayGamesAsync(identity.Token);
                case CloudIdentityProvider.Facebook:
                    return auth.SignInWithFacebookAsync(identity.Token);
                case CloudIdentityProvider.Steam:
                    return auth.SignInWithSteamAsync(identity.Token, identity.Secondary);
                case CloudIdentityProvider.Oculus:
                    return auth.SignInWithOculusAsync(identity.Token, identity.Secondary);
                case CloudIdentityProvider.Unity:
                    return auth.SignInWithUnityAsync(identity.Token);
                case CloudIdentityProvider.OpenIdConnect:
                    return auth.SignInWithOpenIdConnectAsync(identity.Secondary, identity.Token);
                default:
                    throw new ArgumentOutOfRangeException(nameof(identity), identity.Provider, "Unknown identity provider.");
            }
        }

        private static Task LinkAsync(IAuthenticationService auth, CloudIdentity identity, LinkOptions options)
        {
            switch (identity.Provider)
            {
                case CloudIdentityProvider.Apple:
                    return auth.LinkWithAppleAsync(identity.Token, options);
                case CloudIdentityProvider.Google:
                    return auth.LinkWithGoogleAsync(identity.Token, options);
                case CloudIdentityProvider.GooglePlayGames:
                    return auth.LinkWithGooglePlayGamesAsync(identity.Token, options);
                case CloudIdentityProvider.Facebook:
                    return auth.LinkWithFacebookAsync(identity.Token, options);
                case CloudIdentityProvider.Steam:
                    return auth.LinkWithSteamAsync(identity.Token, identity.Secondary, options);
                case CloudIdentityProvider.Oculus:
                    return auth.LinkWithOculusAsync(identity.Token, identity.Secondary, options);
                case CloudIdentityProvider.Unity:
                    return auth.LinkWithUnityAsync(identity.Token, options);
                case CloudIdentityProvider.OpenIdConnect:
                    return auth.LinkWithOpenIdConnectAsync(identity.Secondary, identity.Token, options);
                default:
                    throw new ArgumentOutOfRangeException(nameof(identity), identity.Provider, "Unknown identity provider.");
            }
        }

        private static bool HasSecondary(CloudIdentity identity)
        {
            bool needsSecondary = identity.Provider is CloudIdentityProvider.Steam or CloudIdentityProvider.Oculus or CloudIdentityProvider.OpenIdConnect;
            return !needsSecondary || !string.IsNullOrEmpty(identity.Secondary);
        }

        private static CloudResult MissingSecondary(CloudIdentity identity)
        {
            return CloudResult.Failure(CloudErrorKind.InvalidRequest, $"{identity.Provider} needs CloudIdentity.Secondary.");
        }

        private void Attach(IAuthenticationService auth)
        {
            if (ReferenceEquals(_auth, auth))
            {
                return;
            }

            Detach();
            _auth = auth;
            _auth.SignedIn += Publish;
            _auth.SignedOut += Publish;
            _auth.Expired += Publish;
            _published = State;
        }

        private void Detach()
        {
            if (_auth != null)
            {
                _auth.SignedIn -= Publish;
                _auth.SignedOut -= Publish;
                _auth.Expired -= Publish;
                _auth = null;
            }
        }

        private void Publish()
        {
            CloudAuthState current = State;
            if (current != _published)
            {
                _published = current;
                StateChanged?.Invoke(current);
            }
        }

        private static CloudAuthState Map(AuthenticationState state)
        {
            switch (state)
            {
                case AuthenticationState.SigningIn:
                    return CloudAuthState.SigningIn;
                case AuthenticationState.Authorized:
                case AuthenticationState.Refreshing:
                    return CloudAuthState.SignedIn;
                case AuthenticationState.Expired:
                    return CloudAuthState.Expired;
                default:
                    return CloudAuthState.SignedOut;
            }
        }
    }
}
