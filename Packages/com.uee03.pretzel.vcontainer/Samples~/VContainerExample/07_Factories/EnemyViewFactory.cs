using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>
    /// Spawns injected prefab instances. A factory is one of the few places that may hold the resolver. The sample
    /// builds its template in code; in a game it is a prefab field on a MonoInstaller or a ScriptableObject.
    /// </summary>
    public sealed class EnemyViewFactory : IDisposable
    {
        private readonly IObjectResolver _resolver;
        private readonly List<GameObject> _spawned = new();
        private EnemyView _template;

        public EnemyViewFactory(IObjectResolver resolver) => _resolver = resolver;

        public EnemyView Create(string enemyName, Vector3 position)
        {
            if (_template == null)
            {
                GameObject template = new("Enemy Template");
                template.SetActive(false);
                _template = template.AddComponent<EnemyView>();
            }

            EnemyView view = _resolver.Instantiate(_template, position, Quaternion.identity);
            view.name = enemyName;
            view.gameObject.SetActive(true);
            _spawned.Add(view.gameObject);
            return view;
        }

        public void Dispose()
        {
            foreach (GameObject spawned in _spawned)
            {
                if (spawned != null)
                {
                    UnityEngine.Object.Destroy(spawned);
                }
            }

            if (_template != null)
            {
                UnityEngine.Object.Destroy(_template.gameObject);
            }
        }
    }
}
