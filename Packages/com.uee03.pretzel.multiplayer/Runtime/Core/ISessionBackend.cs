using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pretzel.Multiplayer
{
    /// <summary>
    /// A session transport behind <see cref="IMultiplayerSessionService"/>: the local loopback host or Multiplayer Services plus Relay.
    /// Implementations throw <see cref="MultiplayerException"/> for expected failures and raise the events on the main thread.
    /// </summary>
    public interface ISessionBackend : IDisposable
    {
        SessionMode Mode { get; }

        /// <summary>True while the network connection is up.</summary>
        bool IsConnected { get; }

        event Action<LobbySnapshot> LobbyChanged;

        /// <summary>Raised with true when the host starts the match and false when it ends.</summary>
        event Action<bool> MatchStateChanged;

        /// <summary>The session is gone for good: host left, kicked, deleted.</summary>
        event Action<SessionEndReason, MultiplayerError> SessionEnded;

        /// <summary>The connection dropped but the session may still exist.</summary>
        event Action<MultiplayerError> ConnectionLost;

        Task<LobbySnapshot> HostAsync(HostRequest request, CancellationToken cancellationToken);

        Task<LobbySnapshot> JoinAsync(JoinRequest request, CancellationToken cancellationToken);

        /// <summary>Re-enters a session the client was already part of. Throws a non-retryable error when it cannot be rejoined.</summary>
        Task<LobbySnapshot> ReconnectAsync(ReconnectRecord record, CancellationToken cancellationToken);

        Task<IReadOnlyList<SessionListing>> QuerySessionsAsync(SessionQuery query, CancellationToken cancellationToken);

        Task SetReadyAsync(bool ready, CancellationToken cancellationToken);

        /// <summary>Host only.</summary>
        Task SetMatchStartedAsync(bool started, CancellationToken cancellationToken);

        /// <summary>Leaves the session and stops the network. Never throws for an already-ended session.</summary>
        Task LeaveAsync();
    }
}
