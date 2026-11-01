using System;
using System.Collections.Generic;
using Pretzel.Core;
using Pretzel.Events;
using Pretzel.Multiplayer;
using Pretzel.UI;
using SpaceInvaders.Gameplay;
using UnityEngine;
using VContainer.Unity;

namespace SpaceInvaders.Coop
{
    /// <summary>Connects the co-op screen to the replicated match: the stats of each player, the wave, the game-over panel and the way back out.</summary>
    public sealed class CoopHudPresenter : IStartable, IDisposable
    {
        private readonly CoopHudView _view;
        private readonly CoopMatch _match;
        private readonly PretzelNetworkBootstrap _bootstrap;
        private readonly IEventBus _bus;
        private readonly IGameNavigator _navigator;
        private readonly CompositeDisposable _subscriptions = new();
        private readonly List<CoopPlayerStat> _stats = new();

        public CoopHudPresenter(CoopHudView view, CoopMatch match, PretzelNetworkBootstrap bootstrap, IEventBus bus, IGameNavigator navigator)
        {
            _view = view;
            _match = match;
            _bootstrap = bootstrap;
            _bus = bus;
            _navigator = navigator;
        }

        public void Start()
        {
            _view.LeaveRequested += OnLeave;
            _view.BackToLobbyRequested += OnBackToLobby;
            _view.Bound += Refresh;
            _match.PhaseChanged += OnPhaseChanged;
            _match.WaveChanged += OnWaveChanged;
            _match.StatsChanged += RefreshStats;
            _match.LocalShipsChanged += RefreshStats;
            _bus.Subscribe<WaveStarted>(message => _view.ShowToast($"WAVE {message.Number}", ToastKind.Success)).AddTo(_subscriptions);
            _bus.Subscribe<UfoDestroyed>(message => _view.ShowToast($"MYSTERY SHIP  +{message.Points}", ToastKind.Warning)).AddTo(_subscriptions);
            Refresh();
        }

        public void Dispose()
        {
            _view.LeaveRequested -= OnLeave;
            _view.BackToLobbyRequested -= OnBackToLobby;
            _view.Bound -= Refresh;
            _match.PhaseChanged -= OnPhaseChanged;
            _match.WaveChanged -= OnWaveChanged;
            _match.StatsChanged -= RefreshStats;
            _match.LocalShipsChanged -= RefreshStats;
            _subscriptions.Dispose();
        }

        private void Refresh()
        {
            OnPhaseChanged(_match.Phase);
            OnWaveChanged(_match.Wave);
            RefreshStats();
        }

        private void OnPhaseChanged(CoopPhase phase)
        {
            _view.SetBarVisible(phase != CoopPhase.Lobby);
            if (phase == CoopPhase.GameOver)
            {
                ShowGameOver();
            }
            else
            {
                _view.HideGameOver();
            }
        }

        private void OnWaveChanged(int wave)
        {
            _view.SetWave(wave);
        }

        private void RefreshStats()
        {
            _match.CopyStats(_stats);
            for (int slot = 0; slot < CoopHudView.PlayerPanels; slot++)
            {
                if (!TryFindStat(slot, out CoopPlayerStat stat))
                {
                    _view.HidePlayer(slot);
                    continue;
                }

                _view.SetPlayer(slot, stat.Score, stat.Lives, IsLocal(slot));
            }
        }

        private void ShowGameOver()
        {
            RefreshStats();
            int leading = -1;
            int best = -1;
            bool tied = false;
            for (int slot = 0; slot < CoopHudView.PlayerPanels; slot++)
            {
                if (!TryFindStat(slot, out CoopPlayerStat stat))
                {
                    _view.HideResult(slot);
                    continue;
                }

                _view.SetResult(slot, stat.Score);
                if (stat.Score > best)
                {
                    best = stat.Score;
                    leading = slot;
                    tied = false;
                }
                else if (stat.Score == best)
                {
                    tied = true;
                }
            }

            _view.ShowGameOver(tied ? -1 : leading, _match.IsServer);
        }

        private bool TryFindStat(int slot, out CoopPlayerStat found)
        {
            foreach (CoopPlayerStat stat in _stats)
            {
                if (stat.Slot == slot)
                {
                    found = stat;
                    return true;
                }
            }

            found = default;
            return false;
        }

        // "You" only means something when this machine has one player; with two on the couch both are local.
        private bool IsLocal(int slot)
        {
            if (_match.LocalShipCount != 1)
            {
                return false;
            }

            foreach (CoopShip ship in _match.Ships)
            {
                if (ship != null && ship.IsSlotKnown && ship.Slot == slot && ship.IsLocallyControlled)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnLeave()
        {
            _ = LeaveAsync();
        }

        private void OnBackToLobby()
        {
            _ = BackToLobbyAsync();
        }

        private async Awaitable LeaveAsync()
        {
            try
            {
                IMultiplayerSessionService service = _bootstrap.Service;
                if (service != null)
                {
                    await service.LeaveAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            await _navigator.ReturnToTitleAsync();
        }

        private async Awaitable BackToLobbyAsync()
        {
            try
            {
                IMultiplayerSessionService service = _bootstrap.Service;
                if (service != null)
                {
                    await service.EndMatchAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
