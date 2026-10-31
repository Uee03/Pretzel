using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Pretzel.Multiplayer.Tests
{
    public sealed class ConnectionStateMachineTests
    {
        [Test]
        public void StartsDisconnected()
        {
            Assert.AreEqual(ConnectionState.Disconnected, new ConnectionStateMachine().State);
        }

        [TestCase(ConnectionState.Disconnected, ConnectionState.Connecting)]
        [TestCase(ConnectionState.Disconnected, ConnectionState.ReconnectPrompt)]
        [TestCase(ConnectionState.Connecting, ConnectionState.InLobby)]
        [TestCase(ConnectionState.Connecting, ConnectionState.Playing)]
        [TestCase(ConnectionState.Connecting, ConnectionState.Disconnected)]
        [TestCase(ConnectionState.InLobby, ConnectionState.Playing)]
        [TestCase(ConnectionState.InLobby, ConnectionState.Reconnecting)]
        [TestCase(ConnectionState.Playing, ConnectionState.InLobby)]
        [TestCase(ConnectionState.Playing, ConnectionState.Reconnecting)]
        [TestCase(ConnectionState.Reconnecting, ConnectionState.InLobby)]
        [TestCase(ConnectionState.Reconnecting, ConnectionState.Playing)]
        [TestCase(ConnectionState.Reconnecting, ConnectionState.ReconnectPrompt)]
        [TestCase(ConnectionState.ReconnectPrompt, ConnectionState.Reconnecting)]
        [TestCase(ConnectionState.ReconnectPrompt, ConnectionState.Disconnected)]
        [TestCase(ConnectionState.Disconnecting, ConnectionState.Disconnected)]
        public void AllowedTransitions(ConnectionState from, ConnectionState to)
        {
            Assert.IsTrue(ConnectionStateMachine.CanTransition(from, to));
        }

        [TestCase(ConnectionState.Disconnected, ConnectionState.InLobby)]
        [TestCase(ConnectionState.Disconnected, ConnectionState.Playing)]
        [TestCase(ConnectionState.Disconnected, ConnectionState.Reconnecting)]
        [TestCase(ConnectionState.Connecting, ConnectionState.Reconnecting)]
        [TestCase(ConnectionState.InLobby, ConnectionState.Connecting)]
        [TestCase(ConnectionState.ReconnectPrompt, ConnectionState.InLobby)]
        [TestCase(ConnectionState.ReconnectPrompt, ConnectionState.Playing)]
        [TestCase(ConnectionState.Disconnecting, ConnectionState.Connecting)]
        [TestCase(ConnectionState.Disconnecting, ConnectionState.InLobby)]
        public void ForbiddenTransitions(ConnectionState from, ConnectionState to)
        {
            Assert.IsFalse(ConnectionStateMachine.CanTransition(from, to));
        }

        [Test]
        public void Transition_RaisesChangedWithPreviousAndCurrent()
        {
            ConnectionStateMachine machine = new ConnectionStateMachine();
            List<(ConnectionState, ConnectionState)> seen = new List<(ConnectionState, ConnectionState)>();
            machine.Changed += (previous, current) => seen.Add((previous, current));

            machine.Transition(ConnectionState.Connecting);
            machine.Transition(ConnectionState.InLobby);

            Assert.AreEqual(2, seen.Count);
            Assert.AreEqual((ConnectionState.Disconnected, ConnectionState.Connecting), seen[0]);
            Assert.AreEqual((ConnectionState.Connecting, ConnectionState.InLobby), seen[1]);
        }

        [Test]
        public void TryTransition_ToTheSameState_IsANoOpThatSucceeds()
        {
            ConnectionStateMachine machine = new ConnectionStateMachine();
            int raised = 0;
            machine.Changed += (previous, current) => raised++;

            Assert.IsTrue(machine.TryTransition(ConnectionState.Disconnected));
            Assert.AreEqual(0, raised);
        }

        [Test]
        public void TryTransition_Forbidden_ReturnsFalseAndKeepsTheState()
        {
            ConnectionStateMachine machine = new ConnectionStateMachine();

            Assert.IsFalse(machine.TryTransition(ConnectionState.Playing));
            Assert.AreEqual(ConnectionState.Disconnected, machine.State);
        }

        [Test]
        public void Transition_Forbidden_Throws()
        {
            ConnectionStateMachine machine = new ConnectionStateMachine();

            Assert.Throws<InvalidOperationException>(() => machine.Transition(ConnectionState.InLobby));
        }

        [Test]
        public void EveryStateCanReachDisconnected()
        {
            foreach (ConnectionState state in Enum.GetValues(typeof(ConnectionState)))
            {
                if (state == ConnectionState.Disconnected)
                {
                    continue;
                }

                bool direct = ConnectionStateMachine.CanTransition(state, ConnectionState.Disconnected);
                bool viaDisconnecting = ConnectionStateMachine.CanTransition(state, ConnectionState.Disconnecting);
                Assert.IsTrue(direct || viaDisconnecting, state + " must be able to leave the session");
            }
        }
    }
}
