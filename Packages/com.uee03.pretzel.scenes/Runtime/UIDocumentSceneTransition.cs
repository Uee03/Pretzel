using System;
using System.Threading;
using Pretzel.Tween;
using Pretzel.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Scenes
{
    /// <summary>
    /// A fading loading screen built from a <see cref="UIDocument"/>. Give the UXML a full-screen root with a background.
    /// An optional <see cref="ProgressBar"/> shows the load progress. The screen survives single-mode loads by default.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Pretzel/Scenes/UI Document Scene Transition")]
    public sealed class UIDocumentSceneTransition : MonoBehaviour, ISceneTransition
    {
        [SerializeField] private UIDocument _document;
        [SerializeField, Min(0f)] private float _fadeDuration = 0.35f;
        [SerializeField] private EaseType _ease = EaseType.InOutSine;

        [Tooltip("Name of a ProgressBar in the document. Leave empty for none.")]
        [SerializeField] private string _progressBarName = "progress";

        [Tooltip("Moves this object to DontDestroyOnLoad, so the screen stays up while a single-mode load replaces the scene.")]
        [SerializeField] private bool _persistAcrossLoads = true;

        private ProgressBar _progressBar;

        private VisualElement Root => _document ? _document.rootVisualElement : null;

        private void Reset()
        {
            _document = GetComponent<UIDocument>();
        }

        private void Awake()
        {
            if (!_document)
            {
                _document = GetComponent<UIDocument>();
            }

            if (_persistAcrossLoads)
            {
                if (transform.parent)
                {
                    Debug.LogWarning($"{name}: only a root object can persist across loads. Moving it to the root.", this);
                    transform.SetParent(null, true);
                }

                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            Root?.FadeOutNow();
        }

        public Awaitable ShowAsync(CancellationToken cancellationToken) => FadeAsync(true, cancellationToken);

        public Awaitable HideAsync(CancellationToken cancellationToken) => FadeAsync(false, cancellationToken);

        public void ReportProgress(float progress)
        {
            VisualElement root = Root;
            if (root == null || string.IsNullOrEmpty(_progressBarName))
            {
                return;
            }

            _progressBar ??= root.Q<ProgressBar>(_progressBarName);
            if (_progressBar != null)
            {
                _progressBar.value = Mathf.Lerp(_progressBar.lowValue, _progressBar.highValue, Mathf.Clamp01(progress));
            }
        }

        private async Awaitable FadeAsync(bool show, CancellationToken cancellationToken)
        {
            VisualElement root = Root;
            if (root == null)
            {
                return;
            }

            if (show)
            {
                ReportProgress(0f);
            }

            AwaitableCompletionSource completion = new();
            Action done = () => completion.TrySetResult();
            if (show)
            {
                root.FadeIn(_fadeDuration, _ease, done);
            }
            else
            {
                root.FadeOut(_fadeDuration, _ease, done);
            }

            using (cancellationToken.Register(() =>
                   {
                       root.GetFader().Cancel();
                       completion.TrySetCanceled();
                   }))
            {
                await completion.Awaitable;
            }
        }
    }
}
