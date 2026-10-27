using System;
using UnityEngine;

namespace Pretzel.Scenes
{
    /// <summary>
    /// Maps 0 to 1 onto a range of another progress, so several steps can share one progress bar.
    /// Nest them to split a range further.
    /// </summary>
    public sealed class SubProgress : IProgress<float>
    {
        private readonly IProgress<float> _target;
        private readonly float _start;
        private readonly float _end;

        public SubProgress(IProgress<float> target, float start, float end)
        {
            _target = target;
            _start = Mathf.Clamp01(start);
            _end = Mathf.Clamp(end, _start, 1f);
        }

        public void Report(float value) => _target?.Report(Mathf.Lerp(_start, _end, Mathf.Clamp01(value)));
    }
}
