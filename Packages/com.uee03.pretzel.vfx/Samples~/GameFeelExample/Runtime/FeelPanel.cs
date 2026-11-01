using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Vfx.Samples
{
    /// <summary>Buttons that trigger each effect alone and together, as game code would on a hit.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class FeelPanel : MonoBehaviour
    {
        [SerializeField] private SpriteFlash _flash;
        [SerializeField] private PunchScale _punch;

        private Label _info;
        private IVisualElementScheduledItem _refresh;

        private void OnEnable()
        {
            if (_flash == null || _punch == null)
            {
                Debug.LogError("FeelPanel needs a SpriteFlash and a PunchScale.", this);
                return;
            }

            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            VisualElement bar = new();
            bar.AddToClassList("gf-bar");
            root.Add(bar);

            AddButton(bar, "Hit stop", () => TimeScaleService.Instance.Freeze(0.08f));
            AddButton(bar, "Slow motion", () => TimeScaleService.Instance.SlowMotion(0.25f, 1f));
            AddButton(bar, "Flash", () => _flash.Flash());
            AddButton(bar, "Punch", () => _punch.Punch());
            AddButton(bar, "All together", Hit);

            _info = new Label();
            _info.AddToClassList("gf-info");
            root.Add(_info);
            _refresh = _info.schedule.Execute(() => _info.text = $"Time scale {Time.timeScale:0.####}").Every(100);
        }

        private void OnDisable()
        {
            _refresh?.Pause();
        }

        // What a hit does: a short freeze, a flash and a pop, all at once.
        private void Hit()
        {
            TimeScaleService.Instance.Freeze(0.06f);
            _flash.Flash();
            _punch.Punch(0.35f, 0.3f);
        }

        private static void AddButton(VisualElement parent, string text, Action onClick)
        {
            Button button = new(onClick) { text = text };
            button.AddToClassList("gf-button");
            parent.Add(button);
        }
    }
}
