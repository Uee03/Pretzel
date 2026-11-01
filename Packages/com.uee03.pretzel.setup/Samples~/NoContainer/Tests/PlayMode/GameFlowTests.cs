using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Pretzel.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Pretzel.Samples.NoContainer.PlayTests
{
    /// <summary>Plays the real scenes: menu, one round, the result, and back to the menu, using the buttons a player would press.</summary>
    public sealed class GameFlowTests
    {
        private const float Timeout = 5f;

        private string _folder;
        private DataStore _store;
        private GameServices _services;
        private FieldInfo _roundSeconds;
        private float _originalSeconds;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "pretzel-nc-" + System.Guid.NewGuid().ToString("N"));
            _store = new DataStore(DataRoots.Absolute(_folder));
            if (!Application.CanStreamedLevelBeLoaded(SceneNames.Menu) || !Application.CanStreamedLevelBeLoaded(SceneNames.Game))
            {
                Assert.Inconclusive("Add the sample scenes to the build: Pretzel > Samples > No Container > Add Scenes To Build Settings.");
            }

            _services = Resources.Load<GameServices>(GameServices.ResourceName);
            Assert.IsNotNull(_services, "The sample's GameServices asset must be in a Resources folder.");

            // A one second round keeps the test short. The field is restored afterwards.
            _roundSeconds = typeof(GameServices).GetField("_roundSeconds", BindingFlags.NonPublic | BindingFlags.Instance);
            _originalSeconds = (float)_roundSeconds.GetValue(_services);
            _roundSeconds.SetValue(_services, 1f);
            _services.Initialize(_store);
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            _services.Shutdown();
            _roundSeconds.SetValue(_services, _originalSeconds);
            if (Directory.Exists(_folder))
            {
                Directory.Delete(_folder, true);
            }
        }

        [UnityTest]
        public IEnumerator PlayingARound_SavesTheScoreAndShowsItInTheMenu()
        {
            yield return Load(SceneNames.Menu);
            MenuScreen menu = Object.FindAnyObjectByType<MenuScreen>();
            Assert.IsNotNull(menu);

            Press(menu, "Play");
            yield return WaitForScene(SceneNames.Game);
            yield return null;

            GameController controller = Object.FindAnyObjectByType<GameController>();
            Assert.IsNotNull(controller);
            controller.Click();
            controller.Click();
            controller.Click();

            float waited = 0f;
            while (!controller.Round.IsFinished && waited < Timeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsTrue(controller.Round.IsFinished, "The round did not end.");
            Assert.AreEqual(3, _services.Progress.Current.bestScore);
            Assert.IsTrue(_store.TryLoad(ProgressService.SaveKey, out GameProgress saved), "The score was not saved.");
            Assert.AreEqual(3, saved.bestScore);

            // A click after the round must not change the result.
            controller.Click();
            Assert.AreEqual(3, controller.Round.Score);

            Press(Object.FindAnyObjectByType<GameScreen>(), "Menu");
            yield return WaitForScene(SceneNames.Menu);
            yield return null;

            Label best = Object.FindAnyObjectByType<MenuScreen>().GetComponent<UIDocument>().rootVisualElement.Q<Label>(className: "nc-stat");
            Assert.AreEqual("Best score: 3", best.text);
        }

        [UnityTest]
        public IEnumerator StartingFromTheGameScene_WorksBecauseTheBootstrapRunsFirst()
        {
            yield return Load(SceneNames.Game);
            yield return null;

            GameController controller = Object.FindAnyObjectByType<GameController>();
            Assert.IsNotNull(controller);
            Assert.IsTrue(controller.enabled);
            Assert.AreEqual(0, controller.Round.Score);
        }

        private static IEnumerator Load(string sceneName)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            float waited = 0f;
            while (!string.Equals(SceneManager.GetActiveScene().name, sceneName, System.StringComparison.Ordinal) && waited < Timeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(sceneName, SceneManager.GetActiveScene().name, "The scene did not load.");
        }

        // Presses a button the way navigation does, so the test needs no pointer device.
        private static void Press(Component screen, string label)
        {
            VisualElement root = screen.GetComponent<UIDocument>().rootVisualElement;
            Button button = root.Query<Button>().Where(b => string.Equals(b.text, label, System.StringComparison.Ordinal)).First();
            Assert.IsNotNull(button, $"No button labelled {label}.");
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }
        }
    }
}
