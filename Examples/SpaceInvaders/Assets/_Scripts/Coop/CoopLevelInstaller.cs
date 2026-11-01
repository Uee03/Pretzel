using Pretzel.Cameras;
using Pretzel.Integrations.VContainer;
using Pretzel.Multiplayer;
using Pretzel.Parallax;
using SpaceInvaders.Audio;
using SpaceInvaders.Gameplay;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SpaceInvaders.Coop
{
    /// <summary>
    /// The registrations of the Coop scene. It reuses the single-player actors (formation, bunkers, mystery ship, explosions) and the sound
    /// and music reactions, and adds the network pieces: the shared match, the world replicator, the projectile spawner and the host's rules.
    /// </summary>
    public sealed class CoopLevelInstaller : MonoInstaller
    {
        [SerializeField] private PretzelNetworkBootstrap _bootstrap;
        [SerializeField] private CoopMatch _match;
        [SerializeField] private CoopWorldReplicator _world;
        [SerializeField] private CoopProjectileSpawner _projectiles;
        [SerializeField] private InvaderFormation _formation;
        [SerializeField] private UfoSpawner _ufo;
        [SerializeField] private BunkerField _bunkers;
        [SerializeField] private ExplosionSpawner _explosions;
        [SerializeField] private CameraShake _shake;
        [SerializeField] private ParallaxController _stars;
        [SerializeField] private CoopHudView _hud;

        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterComponent(_bootstrap);
            builder.RegisterComponent(_projectiles).As<IProjectileSpawner>().AsSelf();
            builder.RegisterComponent(_match);
            builder.RegisterComponent(_world);
            builder.RegisterComponent(_formation);
            builder.RegisterComponent(_ufo);
            builder.RegisterComponent(_bunkers);
            builder.RegisterComponent(_explosions);
            builder.RegisterComponent(_shake).As<ICameraShaker>();
            builder.RegisterComponent(_stars);
            builder.RegisterComponent(_hud);

            builder.Register<CoopRunState>(Lifetime.Scoped);
            builder.RegisterEntryPoint<CoopMatchDirector>(Lifetime.Scoped);
            builder.RegisterEntryPoint<CoopHudPresenter>(Lifetime.Scoped);
            builder.RegisterEntryPoint<GameAudioReactions>(Lifetime.Scoped);
            builder.RegisterEntryPoint<GameMusic>(Lifetime.Scoped);
            builder.RegisterEntryPoint<CoopFeelReactions>(Lifetime.Scoped);
        }
    }
}
