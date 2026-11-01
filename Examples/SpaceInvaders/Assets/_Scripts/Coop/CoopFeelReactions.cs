using System;
using Pretzel.Cameras;
using Pretzel.Core;
using Pretzel.Events;
using Pretzel.Parallax;
using SpaceInvaders.Feel;
using SpaceInvaders.Gameplay;
using VContainer.Unity;

namespace SpaceInvaders.Coop
{
    /// <summary>
    /// The feel of a co-op match: camera shake on kills and hits and a faster star field each wave. There is no hit stop or slow motion here:
    /// those change the time scale, and one player's freeze must not slow the host's simulation for everyone.
    /// </summary>
    public sealed class CoopFeelReactions : IStartable, IDisposable
    {
        private readonly IEventBus _bus;
        private readonly ICameraShaker _shaker;
        private readonly ParallaxController _stars;
        private readonly CompositeDisposable _subscriptions = new();

        public CoopFeelReactions(IEventBus bus, ICameraShaker shaker, ParallaxController stars)
        {
            _bus = bus;
            _shaker = shaker;
            _stars = stars;
        }

        public void Start()
        {
            _bus.Subscribe<InvaderDestroyed>(_ => _shaker.AddTrauma(GameFeelReactions.InvaderTrauma)).AddTo(_subscriptions);
            _bus.Subscribe<UfoDestroyed>(_ => _shaker.AddTrauma(GameFeelReactions.UfoTrauma)).AddTo(_subscriptions);
            _bus.Subscribe<PlayerHit>(_ => _shaker.AddTrauma(GameFeelReactions.PlayerTrauma)).AddTo(_subscriptions);
            _bus.Subscribe<GameEnded>(_ => _shaker.AddTrauma(GameFeelReactions.GameOverTrauma)).AddTo(_subscriptions);
            _bus.Subscribe<WaveStarted>(message => _stars.SetAutoScrollScale(GameFeelReactions.ScrollScaleForWave(message.Number))).AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }
    }
}
