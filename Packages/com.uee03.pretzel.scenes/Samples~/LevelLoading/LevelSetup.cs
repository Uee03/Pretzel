using Pretzel.Core.Timers;
using Pretzel.Pooling;
using Pretzel.Scenes;
using UnityEngine;

namespace Pretzel.Samples.LevelLoading
{
    /// <summary>
    /// Lives in each level scene. Prepares the level behind the loading screen: creates the enemy pools, then spawns an
    /// enemy for every <see cref="SpawnMarker"/> under it, spread over frames so the progress bar keeps moving.
    /// </summary>
    public sealed class LevelSetup : MonoBehaviour
    {
        [Tooltip("Enemy prefabs by kind, with prewarm counts.")]
        [SerializeField] private PoolFactory<EnemyKind, Enemy> _enemies = new();

        [Tooltip("Spawn work allowed per frame.")]
        [SerializeField, Min(0.5f)] private float _frameBudgetMilliseconds = 4f;

        [Tooltip("Demo only: a pause per spawned enemy so the progress bar is visible. Keep 0 in a real game.")]
        [SerializeField, Min(0f)] private float _demoDelayPerEnemy = 0.02f;

        public bool IsReady { get; private set; }

        public async Awaitable PrepareAsync(SceneSetupContext context)
        {
            SubProgress pools = new(context.Progress, 0f, 0.2f);
            SubProgress spawning = new(context.Progress, 0.2f, 1f);

            context.ReportStatus("Preparing pools");
            _enemies.Initialize(transform);
            pools.Report(1f);

            SpawnMarker[] markers = GetComponentsInChildren<SpawnMarker>(true);
            int total = 0;
            foreach (SpawnMarker marker in markers)
            {
                total += marker.Count;
            }

            context.ReportStatus($"Spawning {total} enemies");
            FrameBudget budget = new(_frameBudgetMilliseconds);
            int spawned = 0;
            foreach (SpawnMarker marker in markers)
            {
                for (int i = 0; i < marker.Count; i++)
                {
                    _enemies.Spawn(marker.Kind, marker.GetPosition(i), marker.transform.rotation);
                    spawned++;
                    spawning.Report((float)spawned / total);
                    if (_demoDelayPerEnemy > 0f)
                    {
                        await Awaitable.WaitForSecondsAsync(_demoDelayPerEnemy, context.CancellationToken);
                    }
                    else
                    {
                        await budget.YieldIfExceededAsync(context.CancellationToken);
                    }
                }
            }

            spawning.Report(1f);
            context.ReportStatus("Ready");
            IsReady = true;
        }

        private void OnDestroy()
        {
            _enemies.Dispose();
        }
    }
}
