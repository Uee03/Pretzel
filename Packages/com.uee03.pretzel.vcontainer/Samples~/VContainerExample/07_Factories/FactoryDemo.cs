using System;
using UnityEngine;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    public sealed class FactoryDemo : IStartable
    {
        private readonly Func<string, Enemy> _createEnemy;
        private readonly EnemyViewFactory _views;

        public FactoryDemo(Func<string, Enemy> createEnemy, EnemyViewFactory views)
        {
            _createEnemy = createEnemy;
            _views = views;
        }

        public void Start()
        {
            Enemy goblin = _createEnemy("Goblin");
            Enemy orc = _createEnemy("Orc");
            goblin.Hit(3);
            orc.Hit(7);

            _views.Create("Goblin View", new Vector3(-2f, 0f, 0f));
            _views.Create("Orc View", new Vector3(2f, 0f, 0f));
        }
    }
}
