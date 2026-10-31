using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pretzel.Multiplayer
{
    /// <summary>
    /// The single entry point for sessions: local couch co-op, online host/join/quick-join and rejoin after a dropped connection.
    /// Operations report failures through <see cref="ErrorRaised"/> and a false result; they only throw for programming errors. No backend types appear here.
    /// </summary>
    public interface IMultiplayerSessionService : IDisposable
    {
        ConnectionState State { get; }

        /// <summary>The current session, or null when not in one. A new instance is raised through <see cref="LobbyChanged"/> on every change.</summary>
        LobbySnapshot Lobby { get; }

        /// <summary>The remembered session offered for rejoin, or null.</summary>
        ReconnectRecord PendingRejoin { get; }

        SessionEndReason LastEndReason { get; }

        event Action<ConnectionState, ConnectionState> StateChanged;

        event Action<LobbySnapshot> LobbyChanged;

        event Action<MultiplayerError> ErrorRaised;

        event Action<ReconnectProgress> ReconnectProgressChanged;

        /// <summary>Starts a same-machine session: a loopback host plus the requested local players. Needs no cloud services.</summary>
        Task<bool> StartLocalAsync(HostRequest request, CancellationToken cancellationToken = default);

        /// <summary>Creates an online session with Relay and starts hosting it.</summary>
        Task<bool> HostOnlineAsync(HostRequest request, CancellationToken cancellationToken = default);

        /// <summary>Joins an online session by code, by id or with quick-join.</summary>
        Task<bool> JoinOnlineAsync(JoinRequest request, CancellationToken cancellationToken = default);

        /// <summary>Lists joinable sessions, filtered by platform when cross-play is off.</summary>
        Task<IReadOnlyList<SessionListing>> QuerySessionsAsync(SessionQuery query, CancellationToken cancellationToken = default);

        Task<bool> SetReadyAsync(bool ready, CancellationToken cancellationToken = default);

        /// <summary>Host only: starts the match for everyone in the session.</summary>
        Task<bool> StartMatchAsync(CancellationToken cancellationToken = default);

        /// <summary>Host only: returns everyone to the lobby.</summary>
        Task<bool> EndMatchAsync(CancellationToken cancellationToken = default);

        /// <summary>Offers a rejoin when a dropped session is remembered (for example after a relaunch). Returns true when the prompt state was entered.</summary>
        bool CheckForPendingRejoin();

        /// <summary>Runs the reconnect policy again for the remembered session. Valid in <see cref="ConnectionState.ReconnectPrompt"/>.</summary>
        Task<bool> RejoinAsync(CancellationToken cancellationToken = default);

        /// <summary>Cancels a pending connect or reconnect.</summary>
        void CancelPending();

        /// <summary>Leaves the session and forgets it. This is "Return to lobby": the player can start a new game.</summary>
        Task LeaveAsync();

        /// <summary>Call when the app regains focus. Starts the reconnect flow if the connection died while suspended.</summary>
        void NotifyApplicationResumed();
    }
}
