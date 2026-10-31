using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Multiplayer.UI
{
    /// <summary>
    /// Drop-in multiplayer menu. Add it next to a UIDocument whose source asset is MultiplayerMenu.uxml, with a <see cref="PretzelNetworkBootstrap"/> in the scene.
    /// <c>GameObject > Pretzel > Multiplayer > Menu</c> sets everything up. It only talks to <see cref="IMultiplayerSessionService"/>.
    /// </summary>
    [AddComponentMenu("Pretzel/Multiplayer/Multiplayer Menu")]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MultiplayerMenu : MonoBehaviour
    {
        [Tooltip("Optional. Found in the scene when empty.")]
        [SerializeField] private PretzelNetworkBootstrap _bootstrap;

        [SerializeField] private string _defaultDisplayName = "Player";

        [Tooltip("Fade screens in and out with UIScreenStack.")]
        [SerializeField] private bool _animate = true;

        private UIDocument _document;
        private MultiplayerMenuView _view;
        private MultiplayerMenuPresenter _presenter;
        private bool _started;

        /// <summary>The service the menu drives, or null before it is bound.</summary>
        public IMultiplayerSessionService Service => _bootstrap != null ? _bootstrap.Service : null;

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
            if (_presenter != null)
            {
                return;
            }

            if (_bootstrap == null)
            {
                _bootstrap = FindAnyObjectByType<PretzelNetworkBootstrap>();
            }

            if (_bootstrap == null || _bootstrap.Service == null)
            {
                Debug.LogError("[Pretzel.Multiplayer] The menu needs an enabled PretzelNetworkBootstrap in the scene. Use GameObject > Pretzel > Multiplayer > Network Manager (Pretzel defaults).", this);
                return;
            }

            _document = GetComponent<UIDocument>();
            VisualElement root = _document.rootVisualElement;
            if (root == null || root.Q<VisualElement>("multiplayer-root") == null)
            {
                Debug.LogError("[Pretzel.Multiplayer] The UIDocument needs a PanelSettings asset and MultiplayerMenu.uxml as its source asset. Use GameObject > Pretzel > Multiplayer > Menu.", this);
                return;
            }

            _view = new MultiplayerMenuView(root, _animate);
            _presenter = new MultiplayerMenuPresenter(_bootstrap.Service, _view);
            _presenter.Bind(_defaultDisplayName);
        }

        private void Unbind()
        {
            _presenter?.Dispose();
            _presenter = null;
            _view?.Dispose();
            _view = null;
        }
    }
}
