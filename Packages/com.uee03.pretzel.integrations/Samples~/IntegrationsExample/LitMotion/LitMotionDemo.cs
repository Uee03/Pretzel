using LitMotion;
using Pretzel.Integrations.LitMotion;
using Pretzel.Tween;
using Pretzel.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.Integrations
{
    /// <summary>Slides a box with a LitMotion tween whose ease is picked from Pretzel's <see cref="EaseType"/>.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class LitMotionDemo : MonoBehaviour
    {
        private const float Distance = 300f;

        private MotionHandle _handle;
        private EnumField _easeField;
        private VisualElement _box;

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _easeField = UIToolkitUtility.CreateEnumField("Ease", EaseType.OutBounce, root);
            UIToolkitUtility.CreateButton("Slide", Slide, root);

            VisualElement track = UIToolkitUtility.CreateContainer(root);
            track.style.height = 60f;
            _box = UIToolkitUtility.CreateContainer(track);
            _box.style.width = 40f;
            _box.style.height = 40f;
            _box.style.backgroundColor = new Color(0.9f, 0.6f, 0.2f);
        }

        private void OnDisable()
        {
            if (_handle.IsActive())
            {
                _handle.Cancel();
            }
        }

        private void Slide()
        {
            if (_handle.IsActive())
            {
                _handle.Cancel();
            }

            EaseType ease = (EaseType)_easeField.value;
            _handle = LMotion.Create(0f, Distance, 1f)
                .WithEase(ease)
                .Bind(x => _box.style.translate = new Translate(x, 0f));
        }
    }
}
