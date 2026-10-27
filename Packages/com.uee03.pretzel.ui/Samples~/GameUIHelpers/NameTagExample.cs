using Pretzel.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UIHelpers
{
    /// <summary>
    /// Creates a name tag in a screen-space document for a target, using <see cref="UIFollowTarget"/>.
    /// For a world-space panel (Unity 6.2 or newer), put a UIDocument on the target instead and add <see cref="UIBillboard"/>.
    /// </summary>
    public sealed class NameTagExample : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        [SerializeField] private string _displayName = "Player";

        private void Start()
        {
            if (!_document || _document.rootVisualElement == null)
            {
                Debug.LogError("Assign a screen-space UIDocument.", this);
                return;
            }

            Label tag = new(_displayName);
            tag.style.color = Color.white;
            tag.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
            tag.style.paddingLeft = tag.style.paddingRight = 6;
            tag.pickingMode = PickingMode.Ignore;
            _document.rootVisualElement.Add(tag);

            UIFollowTarget follow = gameObject.AddComponent<UIFollowTarget>();
            follow.Configure(_document, tag, transform);
        }
    }
}
