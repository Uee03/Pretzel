using System;
using Pretzel.Cameras;
using Pretzel.Core;
using Pretzel.Events;
using Pretzel.Parallax;
using Pretzel.Vfx;
using SpaceInvaders.Gameplay;
using UnityEngine;
using VContainer.Unity;

namespace SpaceInvaders.Feel
{
    /// <summary>
    /// Game feel, in one place: what each event does to time, the camera, the ship and the backdrop. A small hit stop
    /// and shake on every kill, a long slow motion when the game is lost. The game logic never knows any of it exists;
    /// remove this class and the game plays the same, only flatter.
    /// </summary>
    public sealed class GameFeelReactions : IStartable, IDisposable
    {
        // Seconds of hit stop, trauma (0 to 1) of camera shake, and so on for each event.
        public const float InvaderFreeze = 0.03f;
        public const float InvaderTrauma = 0.12f;
        public const float UfoFreeze = 0.06f;
        public const float UfoTrauma = 0.35f;
        public const float PlayerFreeze = 0.09f;
        public const float PlayerTrauma = 0.75f;
        public const float PlayerSlowScale = 0.35f;
        public const float PlayerSlowSeconds = 0.6f;
        public const float GameOverSlowScale = 0.25f;
        public const float GameOverSlowSeconds = 1.2f;
        public const float GameOverTrauma = 0.9f;

        /// <summary>How much faster the backdrop scrolls per wave, up to <see cref="MaxScrollWaves"/> waves.</summary>
        public const float ScrollPerWave = 0.12f;

        public const int MaxScrollWaves = 10;

        private readonly IEventBus _bus;
        private readonly ITimeScale _time;
        private readonly ICameraShaker _shaker;
        private readonly PlayerShip _player;
        private readonly ParallaxController _stars;
        private readonly CompositeDisposable _subscriptions = new();
        private SpriteFlash _flash;
        private PunchScale _punch;

        public GameFeelReactions(IEventBus bus, ITimeScale time, ICameraShaker shaker, PlayerShip player, ParallaxController stars)
        {
            _bus = bus;
            _time = time;
            _shaker = shaker;
            _player = player;
            _stars = stars;
        }

        /// <summary>The backdrop speed multiplier for a wave: 1 on the first, a little faster on each, capped.</summary>
        public static float ScrollScaleForWave(int wave)
        {
            return 1f + ScrollPerWave * (Mathf.Clamp(wave, 1, MaxScrollWaves) - 1);
        }

        public void Start()
        {
            _flash = _player.GetComponent<SpriteFlash>();
            _punch = _player.GetComponent<PunchScale>();

            _bus.Subscribe<InvaderDestroyed>(_ => Impact(InvaderFreeze, InvaderTrauma)).AddTo(_subscriptions);
            _bus.Subscribe<UfoDestroyed>(_ => Impact(UfoFreeze, UfoTrauma)).AddTo(_subscriptions);
            _bus.Subscribe<PlayerHit>(_ => OnPlayerHit()).AddTo(_subscriptions);
            _bus.Subscribe<GameEnded>(_ => OnGameEnded()).AddTo(_subscriptions);
            _bus.Subscribe<WaveStarted>(message => _stars.SetAutoScrollScale(ScrollScaleForWave(message.Number))).AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        private void Impact(float freezeSeconds, float trauma)
        {
            _time.Freeze(freezeSeconds);
            _shaker.AddTrauma(trauma);
        }

        // The longer slow motion starts when the freeze ends: the stack takes the slowest request, so the two overlap cleanly.
        private void OnPlayerHit()
        {
            Impact(PlayerFreeze, PlayerTrauma);
            _time.SlowMotion(PlayerSlowScale, PlayerSlowSeconds);
            if (_flash != null)
            {
                _flash.Flash();
            }

            if (_punch != null)
            {
                _punch.Punch();
            }
        }

        private void OnGameEnded()
        {
            _time.SlowMotion(GameOverSlowScale, GameOverSlowSeconds);
            _shaker.AddTrauma(GameOverTrauma);
        }
    }
}
