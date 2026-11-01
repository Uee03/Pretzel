using System.Collections.Generic;
using UnityEngine;

namespace Pretzel.Vfx
{
    /// <summary>
    /// Time-scale requests that expire on their own. The result is the slowest scale among the live requests, so a hit
    /// stop and a slow motion overlap sensibly and nothing can forget to restore the time. Durations are unscaled
    /// seconds. Plain C#, so it is testable with any clock.
    /// </summary>
    public sealed class TimeScaleStack
    {
        private readonly List<Request> _requests = new(8);
        private readonly Stack<Request> _free = new(8);
        private int _nextId = 1;

        /// <summary>1 when no request is live, otherwise the lowest scale among them.</summary>
        public float CurrentScale { get; private set; } = 1f;

        public int ActiveCount => _requests.Count;

        public bool IsActive => _requests.Count > 0;

        /// <summary>Slows time to <paramref name="scale"/> for <paramref name="seconds"/>. Returns an id for <see cref="Cancel"/>, or 0 when the request is ignored.</summary>
        public int Push(float scale, float seconds)
        {
            if (seconds <= 0f)
            {
                return 0;
            }

            Request request = _free.Count > 0 ? _free.Pop() : new Request();
            request.Id = _nextId++;
            request.Scale = Mathf.Clamp01(scale);
            request.Remaining = seconds;
            _requests.Add(request);
            Recompute();
            return request.Id;
        }

        public bool Cancel(int id)
        {
            for (int i = 0; i < _requests.Count; i++)
            {
                if (_requests[i].Id == id)
                {
                    Release(i);
                    Recompute();
                    return true;
                }
            }

            return false;
        }

        public void Clear()
        {
            while (_requests.Count > 0)
            {
                Release(_requests.Count - 1);
            }

            Recompute();
        }

        /// <summary>Moves every request forward by the unscaled frame time and drops the ones that ended.</summary>
        public void Advance(float unscaledDelta)
        {
            if (_requests.Count == 0 || unscaledDelta <= 0f)
            {
                return;
            }

            bool changed = false;
            for (int i = _requests.Count - 1; i >= 0; i--)
            {
                _requests[i].Remaining -= unscaledDelta;
                if (_requests[i].Remaining <= 0f)
                {
                    Release(i);
                    changed = true;
                }
            }

            if (changed)
            {
                Recompute();
            }
        }

        private void Release(int index)
        {
            Request request = _requests[index];
            _requests.RemoveAt(index);
            request.Id = 0;
            _free.Push(request);
        }

        private void Recompute()
        {
            float scale = 1f;
            foreach (Request request in _requests)
            {
                scale = Mathf.Min(scale, request.Scale);
            }

            CurrentScale = scale;
        }

        private sealed class Request
        {
            public int Id;
            public float Scale;
            public float Remaining;
        }
    }
}
