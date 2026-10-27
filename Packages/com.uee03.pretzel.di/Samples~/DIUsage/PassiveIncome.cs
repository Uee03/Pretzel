using Pretzel.DI;
using UnityEngine;

namespace Pretzel.Samples.DI
{
    /// <summary>An entry point: created when the scope starts, then ticked every frame without being a MonoBehaviour.</summary>
    public sealed class PassiveIncome : IStartable, ITickable
    {
        private readonly ScoreService _score;
        private float _elapsed;

        public PassiveIncome(ScoreService score)
        {
            _score = score;
        }

        public void Start() => Debug.Log("PassiveIncome started.");

        public void Tick()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= 1f)
            {
                _elapsed -= 1f;
                _score.Add(1);
            }
        }
    }
}
