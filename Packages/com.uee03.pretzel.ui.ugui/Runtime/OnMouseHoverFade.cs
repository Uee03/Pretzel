using UnityEngine;
using UnityEngine.EventSystems;

namespace Pretzel.UI.UGUI
{
    /// <summary>
    /// Fades a <see cref="UIFader"/> in while the pointer is over this object, and out when it leaves.
    /// Works with UI graphics through the EventSystem, and with colliders through the OnMouse messages.
    /// </summary>
    [AddComponentMenu("Pretzel/UI/On Mouse Hover Fade")]
    public class OnMouseHoverFade : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Found on this object when empty.")]
        [SerializeField] private UIFader _uiFader;

        private void Reset()
        {
            TryGetComponent(out _uiFader);
        }

        private void Awake()
        {
            if (!_uiFader)
            {
                TryGetComponent(out _uiFader);
            }
        }

        public void OnMouseEnter()
        {
            if (_uiFader)
            {
                _uiFader.FadeIn();
            }
        }

        public void OnMouseExit()
        {
            if (_uiFader)
            {
                _uiFader.FadeOut();
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => OnMouseEnter();

        public void OnPointerExit(PointerEventData eventData) => OnMouseExit();
    }
}
