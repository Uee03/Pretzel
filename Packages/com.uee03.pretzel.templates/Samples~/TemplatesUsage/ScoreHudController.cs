using Pretzel.UI;
using UnityEngine.UIElements;

namespace Pretzel.Samples.Templates
{
    /// <summary>
    /// Made from Assets > Create > Pretzel > C# UI Controller. Shows the score from <see cref="ScoreKeeper"/>
    /// and a button that adds to it. Use ScoreHud.uxml as the UIDocument's Source Asset.
    /// </summary>
    public class ScoreHudController : UIControllerBase
    {
        private Label _scoreLabel;
        private Button _addButton;
        private ScoreKeeper _scoreKeeper;

        protected override void OnBind(VisualElement root)
        {
            _scoreLabel = root.Q<Label>("score-label");
            _addButton = root.Q<Button>("add-button");
            if (_scoreLabel == null || _addButton == null)
            {
                UnityEngine.Debug.LogWarning("[Pretzel Samples] Assign ScoreHud.uxml to the UIDocument.", this);
                return;
            }

            // Instance creates the ScoreKeeper the first time it is needed.
            _scoreKeeper = ScoreKeeper.Instance;
            _scoreKeeper.ScoreChanged += ShowScore;
            _addButton.clicked += AddPoint;
            ShowScore(_scoreKeeper.Score);
        }

        protected override void OnUnbind(VisualElement root)
        {
            if (_addButton != null)
            {
                _addButton.clicked -= AddPoint;
            }

            // The singleton may already be destroyed when the scene unloads.
            if (_scoreKeeper)
            {
                _scoreKeeper.ScoreChanged -= ShowScore;
            }

            _scoreKeeper = null;
        }

        private void AddPoint() => _scoreKeeper.Add(1);

        private void ShowScore(int score) => _scoreLabel.text = "Score: " + score;
    }
}
