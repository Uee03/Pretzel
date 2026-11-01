using Pretzel.DevConsole;
using Pretzel.Integrations.VContainer;
using SpaceInvaders.Audio;
using SpaceInvaders.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SpaceInvaders.Gameplay
{
    /// <summary>
    /// The registrations of the Game scene: the scene objects the level needs, plus the plain C# presenters that wire them together.
    /// Lives on the level's scope object, so everything registered here is disposed when the scene unloads.
    /// </summary>
    public sealed class LevelInstaller : MonoInstaller
    {
        [SerializeField] private PlayerShip _player;
        [SerializeField] private InvaderFormation _formation;
        [SerializeField] private UfoSpawner _ufo;
        [SerializeField] private BunkerField _bunkers;
        [SerializeField] private ProjectileSpawner _projectiles;
        [SerializeField] private ExplosionSpawner _explosions;
        [SerializeField] private HudView _hud;
        [SerializeField] private DeveloperConsole _console;

        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterComponent(_player);
            builder.RegisterComponent(_formation);
            builder.RegisterComponent(_ufo);
            builder.RegisterComponent(_bunkers);
            builder.RegisterComponent(_projectiles).As<IProjectileSpawner>();
            builder.RegisterComponent(_explosions);
            builder.RegisterComponent(_hud);
            builder.RegisterComponent(_console);

            builder.Register<RunState>(Lifetime.Scoped);
            builder.Register<ShipControls>(Lifetime.Scoped);
            builder.RegisterEntryPoint<LevelPresenter>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<HudPresenter>(Lifetime.Scoped);
            builder.RegisterEntryPoint<GameAudioReactions>(Lifetime.Scoped);
            builder.RegisterEntryPoint<DevCommands>(Lifetime.Scoped);
        }
    }
}
