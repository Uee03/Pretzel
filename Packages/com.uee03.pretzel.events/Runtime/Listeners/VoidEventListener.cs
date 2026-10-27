using System;
using UnityEngine;
using UnityEngine.Events;

namespace Pretzel.Events
{
    /// <summary>Forwards a <see cref="VoidEventChannel"/> to a UnityEvent while enabled.</summary>
    [AddComponentMenu("Pretzel/Events/Void Event Listener")]
    public sealed class VoidEventListener : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel _channel;
        [SerializeField] private UnityEvent _response = new();

        private IDisposable _subscription;

        public UnityEvent Response => _response;

        private void OnEnable()
        {
            if (_channel)
            {
                _subscription = _channel.Subscribe(_response.Invoke);
            }
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
