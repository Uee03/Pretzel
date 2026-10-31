using System;

namespace Pretzel.Multiplayer
{
    /// <summary>Validates <see cref="ConnectionState"/> transitions and raises <see cref="Changed"/>. Pure C#, no Unity dependency.</summary>
    public sealed class ConnectionStateMachine
    {
        private static readonly bool[,] _allowed = BuildTable();

        private ConnectionState _state = ConnectionState.Disconnected;

        public ConnectionState State => _state;

        /// <summary>Raised with the previous and the new state, after the state changed.</summary>
        public event Action<ConnectionState, ConnectionState> Changed;

        public static bool CanTransition(ConnectionState from, ConnectionState to)
        {
            return _allowed[(int)from, (int)to];
        }

        public bool CanTransitionTo(ConnectionState to)
        {
            return CanTransition(_state, to);
        }

        /// <summary>Moves to <paramref name="to"/> when the transition is allowed. Moving to the current state is a no-op that returns true.</summary>
        public bool TryTransition(ConnectionState to)
        {
            if (to == _state)
            {
                return true;
            }

            if (!CanTransition(_state, to))
            {
                return false;
            }

            ConnectionState previous = _state;
            _state = to;
            Changed?.Invoke(previous, to);
            return true;
        }

        public void Transition(ConnectionState to)
        {
            if (!TryTransition(to))
            {
                throw new InvalidOperationException($"Transition {_state} -> {to} is not allowed.");
            }
        }

        private static bool[,] BuildTable()
        {
            int count = Enum.GetValues(typeof(ConnectionState)).Length;
            bool[,] table = new bool[count, count];
            Allow(table, ConnectionState.Disconnected, ConnectionState.Connecting, ConnectionState.ReconnectPrompt);
            Allow(table, ConnectionState.Connecting, ConnectionState.InLobby, ConnectionState.Playing, ConnectionState.Disconnected, ConnectionState.Disconnecting);
            Allow(table, ConnectionState.InLobby, ConnectionState.Playing, ConnectionState.Reconnecting, ConnectionState.ReconnectPrompt, ConnectionState.Disconnecting, ConnectionState.Disconnected);
            Allow(table, ConnectionState.Playing, ConnectionState.InLobby, ConnectionState.Reconnecting, ConnectionState.ReconnectPrompt, ConnectionState.Disconnecting, ConnectionState.Disconnected);
            Allow(table, ConnectionState.Reconnecting, ConnectionState.InLobby, ConnectionState.Playing, ConnectionState.ReconnectPrompt, ConnectionState.Disconnecting, ConnectionState.Disconnected);
            Allow(table, ConnectionState.ReconnectPrompt, ConnectionState.Reconnecting, ConnectionState.Disconnecting, ConnectionState.Disconnected);
            Allow(table, ConnectionState.Disconnecting, ConnectionState.Disconnected);
            return table;
        }

        private static void Allow(bool[,] table, ConnectionState from, params ConnectionState[] targets)
        {
            foreach (ConnectionState target in targets)
            {
                table[(int)from, (int)target] = true;
            }
        }
    }
}
