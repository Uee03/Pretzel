using Pretzel.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Sfx.Samples
{
    /// <summary>
    /// A sound board: one button per sound in the library. The Mash toggle calls one sound every frame, so you can see
    /// the cooldown and the voice limit at work, with the played and skipped counts shown live.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class SfxBoard : MonoBehaviour
    {
        private const string MashId = "hit_light";

        [SerializeField] private SfxLibrary _library;
        [SerializeField] private AudioService _audio;

        private SfxPlayer _player;
        private Label _counts;
        private bool _mashing;

        private void OnEnable()
        {
            if (_library == null || _audio == null)
            {
                Debug.LogError("SfxBoard needs the SfxLibrary and an AudioService.", this);
                enabled = false;
                return;
            }

            _player = new SfxPlayer(_audio, _library);
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();

            VisualElement screen = new();
            screen.AddToClassList("sb-screen");
            root.Add(screen);

            Label title = new("SFX Pack");
            title.AddToClassList("sb-title");
            screen.Add(title);

            VisualElement grid = new();
            grid.AddToClassList("sb-grid");
            screen.Add(grid);
            foreach (SfxEntry entry in _library.Entries)
            {
                string id = entry.Id;
                Button button = new(() => _player.Play(id)) { text = id };
                button.AddToClassList("sb-button");
                grid.Add(button);
            }

            Toggle mash = new($"Mash {MashId} every frame");
            mash.AddToClassList("sb-toggle");
            mash.RegisterValueChangedCallback(change => _mashing = change.newValue);
            screen.Add(mash);

            _counts = new Label();
            _counts.AddToClassList("sb-counts");
            screen.Add(_counts);
        }

        private void Update()
        {
            if (_player == null)
            {
                return;
            }

            if (_mashing)
            {
                _player.Play(MashId);
            }

            _counts.text = $"Played {_player.Played}   Skipped by cooldown or voice limit {_player.Suppressed}";
        }
    }
}
