using Pretzel.DI;
using UnityEngine;

namespace Pretzel.Samples.DI
{
    /// <summary>A MonoBehaviour that receives services through [Inject] members instead of searching the scene.</summary>
    public sealed class ScoreView : MonoBehaviour
    {
        private ScoreService _score;
        private int _shown = -1;

        [Inject]
        private void Construct(ScoreService score) => _score = score;

        private void Update()
        {
            if (_score == null || _score.Score == _shown)
            {
                return;
            }

            _shown = _score.Score;
            Debug.Log($"Score: {_shown}", this);
        }
    }
}
