using Pretzel.UI.UGUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pretzel.Samples.UGUI
{
    /// <summary>
    /// Put this on a panel with a CanvasGroup and a UIFader, then assign the buttons. See the README for the full setup,
    /// including an OnMouseHoverFade tooltip.
    /// </summary>
    [RequireComponent(typeof(UIFader))]
    public class UGUIUsageExample : MonoBehaviour
    {
        [SerializeField] private Button _showButton;
        [SerializeField] private Button _hideButton;
        [Tooltip("Optional. Snaps the panel in or out without a fade.")]
        [SerializeField] private Button _snapButton;
        [Tooltip("Optional. A TMP_InputField whose placeholder shows the panel state.")]
        [SerializeField] private TMP_InputField _nameInput;
        [SerializeField, Min(0f)] private float _fadeInSeconds = 0.25f;
        [SerializeField, Min(0f)] private float _fadeOutSeconds = 0.5f;

        private UIFader _fader;

        private void Awake()
        {
            _fader = GetComponent<UIFader>();

            // The durations can be changed from code, for example from a settings menu.
            _fader.FadeInDuration = _fadeInSeconds;
            _fader.FadeOutDuration = _fadeOutSeconds;
        }

        private void OnEnable()
        {
            // The extensions throw for a destroyed button and ignore a null action, so optional buttons are checked first.
            if (_showButton)
            {
                _showButton.AddListener(_fader.FadeIn);
            }

            if (_hideButton)
            {
                _hideButton.AddListener(_fader.FadeOut);
            }

            if (_snapButton)
            {
                _snapButton.AddListener(Snap);
            }

            UpdatePlaceholder();
        }

        private void OnDisable()
        {
            if (_showButton)
            {
                _showButton.RemoveListener(_fader.FadeIn);
            }

            if (_hideButton)
            {
                _hideButton.RemoveListener(_fader.FadeOut);
            }

            if (_snapButton)
            {
                _snapButton.RemoveListener(Snap);
            }
        }

        private void OnDestroy()
        {
            // Clears listeners added from the Inspector as well, so a pooled button carries nothing to its next owner.
            if (_snapButton)
            {
                _snapButton.RemoveAllListeners();
            }
        }

        private void Snap()
        {
            // Alpha is the current opacity, so it also works in the middle of a fade.
            if (_fader.Alpha > 0.5f)
            {
                _fader.FadeOutNow();
            }
            else
            {
                _fader.FadeInNow();
            }

            UpdatePlaceholder();
        }

        private void UpdatePlaceholder()
        {
            if (_nameInput && _nameInput.placeholder)
            {
                // Does nothing unless the placeholder is a TextMeshPro text.
                _nameInput.placeholder.SetPlaceHolderText(_fader.Alpha > 0.5f ? "Enter a name" : "Panel hidden");
            }
        }
    }
}
