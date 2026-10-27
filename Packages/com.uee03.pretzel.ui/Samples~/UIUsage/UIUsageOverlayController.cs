using Pretzel.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI
{
    /// <summary>
    /// The overlay screen. <see cref="UIUsageController"/> opens it with SwitchTo, and the Back button switches back.
    /// The UIDocumentFader on the same object fades the whole document in each time it is shown.
    /// </summary>
    [RequireComponent(typeof(UIDocumentFader))]
    public class UIUsageOverlayController : UIControllerBase
    {
        [SerializeField] private UIUsageController _main;

        private UIDocumentFader _documentFader;
        private Button _back;

        protected override void Awake()
        {
            base.Awake();
            _documentFader = GetComponent<UIDocumentFader>();
        }

        protected override void OnBind(VisualElement root)
        {
            _back = root.Q<Button>("back-button");
            if (_back != null)
            {
                _back.clicked += OnBack;
            }

            // The overlay starts closed. Show and Hide only change display, so a hidden screen costs no layout.
            Hide();
        }

        protected override void OnUnbind(VisualElement root)
        {
            if (_back != null)
            {
                _back.clicked -= OnBack;
            }
        }

        protected override void OnShown()
        {
            // Start from invisible, then fade the whole document in.
            _documentFader.Fade(FadeType.FadeOutNow);
            _documentFader.FadeIn();
        }

        private void OnBack()
        {
            if (_main)
            {
                _main.SwitchTo(this);
            }
            else
            {
                Hide();
            }
        }
    }
}
