using UnityEngine;

namespace Pretzel.Samples.Pooling.Waves
{
    /// <summary>Shows the pool counts and wave stats. Refreshes four times a second, so it allocates nothing between refreshes.</summary>
    [RequireComponent(typeof(WaveSpawner))]
    public sealed class PoolMetricsHud : MonoBehaviour
    {
        private WaveSpawner _spawner;
        private GUIStyle _style;
        private string _text = string.Empty;
        private float _nextRefresh;

        private void Awake()
        {
            _spawner = GetComponent<WaveSpawner>();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh)
            {
                return;
            }

            _nextRefresh = Time.unscaledTime + 0.25f;
            _text = $"Wave {_spawner.Wave}   Alive {_spawner.Alive}   Defeated {_spawner.Defeated}   Escaped {_spawner.Escaped}\n{_spawner.DescribePools()}";
        }

        private void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = Color.white } };
            GUI.Label(new Rect(12f, 8f, 700f, 120f), _text, _style);
        }
    }
}
