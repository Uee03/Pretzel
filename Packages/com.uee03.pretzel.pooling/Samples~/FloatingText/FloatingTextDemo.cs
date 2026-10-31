using UnityEngine;

namespace Pretzel.Samples.Pooling.FloatingText
{
    /// <summary>Fires random damage numbers on a <see cref="FloatingTextLayer"/> and shows what the pools hold.</summary>
    [RequireComponent(typeof(FloatingTextLayer))]
    public sealed class FloatingTextDemo : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _numbersPerSecond = 40f;

        private FloatingTextLayer _layer;
        private float _nextNumber;
        private float _nextRefresh;
        private string _text = string.Empty;
        private GUIStyle _style;

        private void Awake()
        {
            _layer = GetComponent<FloatingTextLayer>();
        }

        private void Update()
        {
            float interval = 1f / _numbersPerSecond;
            while (Time.unscaledTime >= _nextNumber)
            {
                _nextNumber += interval;
                Emit();
                if (_nextNumber < Time.unscaledTime - 0.5f)
                {
                    _nextNumber = Time.unscaledTime;
                }
            }

            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 0.25f;
                _text = $"Showing {_layer.ActiveCount}   Labels created {_layer.PooledLabelsCreated}   idle {_layer.PooledLabelsIdle}";
            }
        }

        private void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = Color.white } };
            GUI.Label(new Rect(12f, 8f, 700f, 28f), _text, _style);
        }

        [ContextMenu("Burst Of 50")]
        private void Burst()
        {
            for (int i = 0; i < 50; i++)
            {
                Emit();
            }
        }

        private void Emit()
        {
            Vector2 size = _layer.PanelSize;
            Vector2 position = new(Random.Range(0.1f, 0.9f) * size.x, Random.Range(0.25f, 0.85f) * size.y);
            bool critical = Random.value < 0.15f;
            int damage = Random.Range(5, 40) * (critical ? 3 : 1);
            _layer.Show(critical ? damage + "!" : damage.ToString(), position, critical ? new Color(1f, 0.8f, 0.2f) : Color.white);
        }
    }
}
