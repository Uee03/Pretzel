using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pretzel.DI
{
    /// <summary>
    /// The composition root of a scene or prefab. Builds a container in Awake from <see cref="Configure"/>, the installer assets,
    /// and every <see cref="IInstaller"/> component on this GameObject. It then injects its own hierarchy and the auto-inject list,
    /// and runs the entry points. Destroying it disposes the container.
    /// </summary>
    [DefaultExecutionOrder(-5000)]
    [AddComponentMenu("Pretzel/DI/Lifetime Scope")]
    public class LifetimeScope : MonoBehaviour
    {
        [Tooltip("Optional. Its services are visible in this scope, and this scope is disposed with it.")]
        [SerializeField] private LifetimeScope _parent;

        [SerializeField] private List<ScriptableInstaller> _installers = new();

        [Tooltip("Other objects whose components get their [Inject] members filled. This scope's own hierarchy is always injected.")]
        [SerializeField] private List<GameObject> _autoInject = new();

        [Tooltip("When off, call Build yourself, for example after setting the parent from code.")]
        [SerializeField] private bool _buildOnAwake = true;

        private EntryPointDispatcher _entryPoints;
        private bool _building;

        public Container Container { get; private set; }

        public bool IsBuilt => Container != null;

        public LifetimeScope Parent
        {
            get => _parent;
            set
            {
                if (IsBuilt)
                {
                    throw new InvalidOperationException("The parent cannot change after the scope is built.");
                }

                _parent = value;
            }
        }

        protected virtual void Awake()
        {
            if (_buildOnAwake)
            {
                Build();
            }
        }

        /// <summary>Builds the container once. A parent scope is built first.</summary>
        public void Build()
        {
            if (IsBuilt)
            {
                return;
            }

            if (_building)
            {
                throw new ContainerException($"{name}: a LifetimeScope parent chain loops back to itself.");
            }

            _building = true;
            try
            {
                if (_parent)
                {
                    _parent.Build();
                    Container = _parent.Container.CreateScope(Install);
                }
                else
                {
                    ContainerBuilder builder = new();
                    Install(builder);
                    Container = builder.Build();
                }
            }
            finally
            {
                _building = false;
            }

            Container.InjectGameObject(gameObject);
            foreach (GameObject target in _autoInject)
            {
                if (target)
                {
                    Container.InjectGameObject(target);
                }
            }

            _entryPoints = new EntryPointDispatcher(Container.ResolveEntryPoints());
            _entryPoints.Start();
        }

        /// <summary>Adds this scope's own registrations. Called before the container is built.</summary>
        protected virtual void Configure(ContainerBuilder builder)
        {
        }

        protected virtual void Update()
        {
            if (IsAlive)
            {
                _entryPoints.Tick();
            }
        }

        protected virtual void LateUpdate()
        {
            if (IsAlive)
            {
                _entryPoints.LateTick();
            }
        }

        protected virtual void FixedUpdate()
        {
            if (IsAlive)
            {
                _entryPoints.FixedTick();
            }
        }

        // A parent scope disposes this container when it is destroyed first.
        private bool IsAlive => _entryPoints != null && Container is { IsDisposed: false };

        protected virtual void OnDestroy()
        {
            _entryPoints = null;
            Container?.Dispose();
            Container = null;
        }

        private void Install(ContainerBuilder builder)
        {
            builder.RegisterInstance(this);
            foreach (ScriptableInstaller installer in _installers)
            {
                if (installer)
                {
                    builder.Install(installer);
                }
            }

            foreach (IInstaller installer in GetComponents<IInstaller>())
            {
                builder.Install(installer);
            }

            Configure(builder);
        }
    }
}
