using Pretzel.UI.UGUI;
using UnityEngine;
using UnityEngine.UI;

namespace Pretzel.Samples.UGUI
{
    /// <summary>Put this on a Canvas object with a CanvasGroup, UIFader and two Buttons.</summary>
    [RequireComponent(typeof(UIFader))]
    public class UGUIUsageExample : MonoBehaviour
    {
        [SerializeField] private Button _showButton;
        [SerializeField] private Button _hideButton;

        private UIFader _fader;

        private void Awake()
        {
            _fader = GetComponent<UIFader>();
        }

        private void OnEnable()
        {
            _showButton.AddListener(_fader.FadeIn);
            _hideButton.AddListener(_fader.FadeOut);
        }

        private void OnDisable()
        {
            _showButton.RemoveListener(_fader.FadeIn);
            _hideButton.RemoveListener(_fader.FadeOut);
        }
    }
}
