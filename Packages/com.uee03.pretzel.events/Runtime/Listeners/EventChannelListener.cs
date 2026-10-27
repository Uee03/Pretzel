using System;
using UnityEngine;
using UnityEngine.Events;

namespace Pretzel.Events
{
    /// <summary>Forwards a channel to a UnityEvent while enabled. Derive a concrete class per payload type.</summary>
    public abstract class EventChannelListener<TChannel, T> : MonoBehaviour where TChannel : EventChannel<T>
    {
        [SerializeField] private TChannel _channel;
        [SerializeField] private UnityEvent<T> _response = new();

        private IDisposable _subscription;

        public TChannel Channel => _channel;

        public UnityEvent<T> Response => _response;

        protected virtual void OnEnable()
        {
            if (_channel)
            {
                _subscription = _channel.Subscribe(OnRaised);
            }
        }

        protected virtual void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        protected virtual void OnRaised(T value) => _response.Invoke(value);
    }
}
