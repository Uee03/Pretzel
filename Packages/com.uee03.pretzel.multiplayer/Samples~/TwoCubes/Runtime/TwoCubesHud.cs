using System;
using Pretzel.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Multiplayer.Samples.TwoCubes
{
    /// <summary>
    /// The Spawn Item / Delete Items bar, shown to every player while the match runs. The buttons call the spawner, which asks the server by Rpc,
    /// so they work the same for the host and for clients. Needs a UIDocument with TwoCubesHud.uxml.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class TwoCubesHud : MonoBehaviour
    {
        [SerializeField] private TwoCubesItemSpawner _spawner;
        [SerializeField] private PretzelNetworkBootstrap _bootstrap;

        private readonly System.Collections.Generic.List<IDisposable> _bindings = new System.Collections.Generic.List<IDisposable>();
        private VisualElement _root;
        private Label _count;
        private IMultiplayerSessionService _service;
        private bool _started;

        private void Start()
        {
            _started = true;
            Bind();
        }

        private void OnEnable()
        {
            if (_started)
            {
                Bind();
            }
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Bind()
        {
            if (_root != null)
            {
                return;
            }

            if (_bootstrap == null)
            {
                _bootstrap = FindAnyObjectByType<PretzelNetworkBootstrap>();
            }

            VisualElement document = GetComponent<UIDocument>().rootVisualElement;
            if (_bootstrap == null || _bootstrap.Service == null || document == null || document.Q<VisualElement>("twocubes-root") == null || _spawner == null)
            {
                Debug.LogError("[TwoCubes] The HUD needs a PretzelNetworkBootstrap, a TwoCubesItemSpawner and a UIDocument with TwoCubesHud.uxml.", this);
                return;
            }

            _root = document.QRequired<VisualElement>("twocubes-root");
            _root.pickingMode = PickingMode.Ignore;
            _count = document.QRequired<Label>("item-count");
            _bindings.Add(document.QRequired<Button>("spawn-item").OnClick(() => _spawner.RequestSpawn()));
            _bindings.Add(document.QRequired<Button>("delete-items").OnClick(() => _spawner.RequestDeleteAll()));
            _service = _bootstrap.Service;
            _service.StateChanged += OnStateChanged;
            _spawner.ItemCountChanged += OnItemCountChanged;
            _root.SetDisplayed(_service.State == ConnectionState.Playing);
        }

        private void Unbind()
        {
            if (_service != null)
            {
                _service.StateChanged -= OnStateChanged;
            }

            if (_spawner != null)
            {
                _spawner.ItemCountChanged -= OnItemCountChanged;
            }

            for (int i = 0; i < _bindings.Count; i++)
            {
                _bindings[i].Dispose();
            }

            _bindings.Clear();
            _service = null;
            _root = null;
        }

        private void OnStateChanged(ConnectionState previous, ConnectionState current)
        {
            _root.SetDisplayed(current == ConnectionState.Playing);
        }

        private void OnItemCountChanged(int count)
        {
            _count.text = "Items: " + count;
        }
    }
}
