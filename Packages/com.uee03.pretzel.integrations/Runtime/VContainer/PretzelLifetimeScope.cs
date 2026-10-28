namespace Pretzel.Integrations.VContainer
{
    using System;
    using System.Collections.Generic;
    using global::VContainer;
    using global::VContainer.Unity;
    using UnityEngine;

    /// <summary>
    /// A <see cref="LifetimeScope"/> configured in the Inspector. Settings assets are registered as their own type, then
    /// <see cref="ScriptableInstaller"/>s run in list order, then the <see cref="MonoInstaller"/>s that belong to this scope.
    /// </summary>
    [DisallowMultipleComponent]
    public class PretzelLifetimeScope : LifetimeScope
    {
        [Tooltip("Configuration assets, resolvable by their concrete type. Treat them as read-only at runtime.")]
        [SerializeField] private List<ScriptableObject> _settings = new();

        [Tooltip("Registrations shared by every scene, run in list order.")]
        [SerializeField] private List<ScriptableInstaller> _installers = new();

        public IReadOnlyList<ScriptableObject> Settings => _settings;

        public IReadOnlyList<ScriptableInstaller> Installers => _installers;

        protected override void Configure(IContainerBuilder builder)
        {
            for (int i = 0; i < _settings.Count; i++)
            {
                ScriptableObject settings = _settings[i] ? _settings[i] : throw EmptySlot("Settings", i);
                builder.RegisterInstance(settings, settings.GetType());
            }

            for (int i = 0; i < _installers.Count; i++)
            {
                ScriptableInstaller installer = _installers[i] ? _installers[i] : throw EmptySlot("Installers", i);
                installer.Install(builder);
            }

            foreach (MonoInstaller installer in GetComponentsInChildren<MonoInstaller>(true))
            {
                if (installer.GetComponentInParent<LifetimeScope>(true) == this)
                {
                    installer.Install(builder);
                }
            }
        }

        private InvalidOperationException EmptySlot(string list, int index)
        {
            return new InvalidOperationException($"{name}: {list} element {index} is empty. Assign it or remove the slot.");
        }
    }
}
