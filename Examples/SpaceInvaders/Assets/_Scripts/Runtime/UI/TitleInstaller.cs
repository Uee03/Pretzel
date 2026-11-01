using Pretzel.Integrations.VContainer;
using SpaceInvaders.Audio;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SpaceInvaders.UI
{
    /// <summary>The registrations of the Title scene.</summary>
    public sealed class TitleInstaller : MonoInstaller
    {
        [SerializeField] private TitleView _title;

        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterComponent(_title);
            builder.RegisterEntryPoint<TitlePresenter>(Lifetime.Scoped);
            builder.RegisterEntryPoint<TitleMusic>(Lifetime.Scoped);
        }
    }
}
