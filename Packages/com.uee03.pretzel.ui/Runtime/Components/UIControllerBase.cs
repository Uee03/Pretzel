using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.UI
{
    /// <summary>
    /// Base class for a screen built with a <see cref="UIDocument"/>. Query elements and register callbacks in
    /// <see cref="OnBind"/>, and release them in <see cref="OnUnbind"/>. Show and hide use <c>display</c>, so a hidden screen costs no layout.
    /// Overrides of <see cref="OnEnable"/> and <see cref="OnDisable"/> must call the base method.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public abstract class UIControllerBase : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        [Header("Delays")]
        [Tooltip("Seconds to wait before ShowDelayed shows the screen.")]
        [SerializeField, Min(0f)] private float _showDelay = 1f;
        [Tooltip("Seconds to wait before HideDelayed hides the screen.")]
        [SerializeField, Min(0f)] private float _hideDelay = 1f;

        private CancellationTokenSource _pending;
        private VisualElement _boundRoot;

        public UIDocument Document => _document;

        public VisualElement Root => _document ? _document.rootVisualElement : null;

        public bool IsVisible => Root != null && Root.style.display != DisplayStyle.None;

        protected virtual void Reset()
        {
            _document = GetComponent<UIDocument>();
        }

        protected virtual void Awake()
        {
            if (!_document)
            {
                _document = GetComponent<UIDocument>();
            }
        }

        protected virtual void OnEnable()
        {
            TryBind();
        }

        // UIDocument may build its tree after this component's OnEnable, so try again once every OnEnable has run.
        protected virtual void Start()
        {
            TryBind();
        }

        protected virtual void OnDisable()
        {
            CancelPending();
            if (_boundRoot != null)
            {
                OnUnbind(_boundRoot);
                _boundRoot = null;
            }
        }

        protected virtual void OnDestroy()
        {
            CancelPending();
        }

        /// <summary>Called once the document tree exists. Query elements and register callbacks here.</summary>
        protected virtual void OnBind(VisualElement root)
        {
        }

        /// <summary>Called when the component is disabled. Unregister what <see cref="OnBind"/> registered.</summary>
        protected virtual void OnUnbind(VisualElement root)
        {
        }

        protected virtual void OnShown()
        {
        }

        protected virtual void OnHidden()
        {
        }

        public void Show()
        {
            CancelPending();
            SetVisible(true);
        }

        public void Hide()
        {
            CancelPending();
            SetVisible(false);
        }

        public void ShowDelayed() => _ = RunAfterAsync(_showDelay, true);

        public void HideDelayed() => _ = RunAfterAsync(_hideDelay, false);

        /// <summary>Shows this screen and hides <paramref name="other"/>.</summary>
        public void SwitchTo(UIControllerBase other)
        {
            Show();
            if (other && other != this)
            {
                other.Hide();
            }
        }

        public void DeactivateGameObject() => gameObject.SetActive(false);

        public void ActivateGameObject() => gameObject.SetActive(true);

        private void TryBind()
        {
            VisualElement root = Root;
            if (root == null || _boundRoot == root)
            {
                return;
            }

            _boundRoot = root;
            OnBind(root);
        }

        private void SetVisible(bool visible)
        {
            VisualElement root = Root;
            if (root == null)
            {
                return;
            }

            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible)
            {
                OnShown();
            }
            else
            {
                OnHidden();
            }
        }

        private async Awaitable RunAfterAsync(float seconds, bool visible)
        {
            CancelPending();
            if (seconds <= 0f)
            {
                SetVisible(visible);
                return;
            }

            CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            _pending = source;
            try
            {
                await Awaitable.WaitForSecondsAsync(seconds, source.Token);
                SetVisible(visible);
            }
            catch (OperationCanceledException)
            {
                // Replaced by a newer call, or the component went away.
            }
            finally
            {
                if (_pending == source)
                {
                    _pending = null;
                }

                source.Dispose();
            }
        }

        private void CancelPending()
        {
            _pending?.Cancel();
            _pending = null;
        }
    }
}
