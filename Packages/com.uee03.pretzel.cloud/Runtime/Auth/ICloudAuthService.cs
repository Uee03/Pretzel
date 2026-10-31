using System;
using System.Threading;
using UnityEngine;

namespace Pretzel.Cloud
{
    /// <summary>
    /// Player authentication for cloud services. Methods return failures as <see cref="CloudResult"/>; cancellation throws.
    /// All members must be used from the main thread.
    /// </summary>
    public interface ICloudAuthService
    {
        CloudAuthState State { get; }

        bool IsSignedIn { get; }

        /// <summary>The player id while signed in, otherwise null.</summary>
        string PlayerId { get; }

        /// <summary>Raised on the main thread when <see cref="State"/> changes.</summary>
        event Action<CloudAuthState> StateChanged;

        /// <summary>Starts the underlying services. Safe to call again once it has succeeded.</summary>
        Awaitable<CloudResult> InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>Signs in with a device-bound anonymous account. Succeeds at once when already signed in.</summary>
        Awaitable<CloudResult> SignInAnonymouslyAsync(CancellationToken cancellationToken = default);

        Awaitable<CloudResult> SignInWithIdentityAsync(CloudIdentity identity, CancellationToken cancellationToken = default);

        /// <param name="createAccount">True to sign up a new account, false to sign in to an existing one.</param>
        Awaitable<CloudResult> SignInWithUsernamePasswordAsync(string username, string password, bool createAccount, CancellationToken cancellationToken = default);

        /// <summary>Links a platform identity to the signed-in account, for example to keep an anonymous player's progress.</summary>
        /// <param name="forceLink">True to link even when the identity belongs to another account.</param>
        Awaitable<CloudResult> LinkIdentityAsync(CloudIdentity identity, bool forceLink = false, CancellationToken cancellationToken = default);

        /// <summary>Adds a username and password to the signed-in account.</summary>
        Awaitable<CloudResult> AddUsernamePasswordAsync(string username, string password, CancellationToken cancellationToken = default);

        /// <param name="clearCredentials">True to also forget the cached session, so the next sign in creates a new anonymous player.</param>
        void SignOut(bool clearCredentials = false);
    }
}
