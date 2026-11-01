using Pretzel.Core;
using Pretzel.Events;
using Pretzel.Pooling;
using UnityEngine;
using VContainer;

namespace SpaceInvaders.Gameplay
{
    /// <summary>Turns game events into pooled bursts. Nothing in the game calls it directly.</summary>
    public sealed class ExplosionSpawner : MonoBehaviour
    {
        [SerializeField] private ExplosionFx _prefab;
        [SerializeField] private Color _squid = new(1f, 0.35f, 0.85f);
        [SerializeField] private Color _crab = new(0.3f, 0.9f, 1f);
        [SerializeField] private Color _octopus = new(0.4f, 1f, 0.45f);
        [SerializeField] private Color _ufo = new(1f, 0.3f, 0.3f);
        [SerializeField] private Color _player = new(1f, 1f, 1f);

        private readonly CompositeDisposable _subscriptions = new();
        private IEventBus _bus;
        private IPoolRegistry _pools;

        // Runs when the level scope builds, before Awake: keep it to storing dependencies.
        [Inject]
        public void Construct(IEventBus bus, IPoolRegistry pools)
        {
            _bus = Guard.NotNull(bus, nameof(bus));
            _pools = Guard.NotNull(pools, nameof(pools));
        }

        private void Start()
        {
            _bus.Subscribe<InvaderDestroyed>(OnInvaderDestroyed).AddTo(_subscriptions);
            _bus.Subscribe<UfoDestroyed>(message => Burst(message.Position, _ufo, 1.6f)).AddTo(_subscriptions);
            _bus.Subscribe<PlayerHit>(message => Burst(message.Position, _player, 1.8f)).AddTo(_subscriptions);
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        private void OnInvaderDestroyed(InvaderDestroyed message)
        {
            Color tint = message.Kind == InvaderKind.Squid ? _squid : message.Kind == InvaderKind.Crab ? _crab : _octopus;
            Burst(message.Position, tint, 1.1f);
        }

        private void Burst(Vector3 position, Color tint, float size)
        {
            ExplosionFx fx = _pools.Spawn(_prefab, position, Quaternion.identity);
            fx.Play(tint, size);
        }
    }
}
