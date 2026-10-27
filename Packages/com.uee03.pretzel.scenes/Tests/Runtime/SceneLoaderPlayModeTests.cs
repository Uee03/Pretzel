using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ThreadPriority = UnityEngine.ThreadPriority;

namespace Pretzel.Scenes.Tests
{
    [PrebuildSetup("Pretzel.Scenes.Tests.SceneLoaderTestScenes")]
    [PostBuildCleanup("Pretzel.Scenes.Tests.SceneLoaderTestScenes")]
    [Timeout(30000)]
    public sealed class SceneLoaderPlayModeTests
    {
        private static int _hostScenes;
        private SceneLoader _loader;

        [SetUp]
        public void SetUp() => _loader = new SceneLoader();

        [UnityTearDown]
        public IEnumerator TearDown() => Run(UnloadTestScenesAsync());

        [UnityTest]
        public IEnumerator AdditiveLoad_RunsTheWholePipeline_InOrder() => Run(AdditiveLoadAsync());

        private async Awaitable AdditiveLoadAsync()
        {
            List<SceneLoadState> states = new();
            List<string> events = new();
            ProgressRecorder progress = new();
            FakeTransition transition = new(events);
            _loader.StateChanged += state => states.Add(state);
            _loader.LoadStarted += _ => events.Add("started");
            _loader.LoadFinished += _ => events.Add("finished");
            ThreadPriority priority = Application.backgroundLoadingPriority;

            Scene scene = await _loader.LoadAsync(TestSceneNames.A, new SceneLoadOptions
            {
                Mode = LoadSceneMode.Additive,
                Transition = transition,
                Progress = progress,
                LoadingStatus = "Loading",
                SetupAsync = async context =>
                {
                    events.Add("setup");
                    Assert.That(context.Scene, Is.EqualTo(SceneManager.GetSceneByName(TestSceneNames.A)));
                    Assert.That(context.Scene.isLoaded, Is.True);
                    Assert.That(_loader.State, Is.EqualTo(SceneLoadState.SettingUp));
                    context.ReportStatus("Spawning");
                    context.Progress.Report(0.5f);
                    await Awaitable.NextFrameAsync();
                },
            });

            Assert.That(scene.IsValid() && scene.isLoaded, Is.True);
            Assert.That(scene.name, Is.EqualTo(TestSceneNames.A));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(scene));
            Assert.That(events, Is.EqualTo(new[] { "started", "status:Loading", "show", "setup", "status:Spawning", "hide", "finished" }));
            Assert.That(states, Is.EqualTo(new[]
            {
                SceneLoadState.ShowingTransition, SceneLoadState.Loading, SceneLoadState.Activating, SceneLoadState.SettingUp,
                SceneLoadState.HidingTransition, SceneLoadState.Idle,
            }));
            progress.AssertMonotonicToOne();
            Assert.That(transition.LastProgress, Is.EqualTo(1f));
            Assert.That(_loader.Progress, Is.EqualTo(1f));
            Assert.That(_loader.IsBusy, Is.False);
            Assert.That(_loader.LastReport.Succeeded, Is.True);
            Assert.That(_loader.LastReport.SceneNames, Is.EqualTo(new[] { TestSceneNames.A }));
            Assert.That(Application.backgroundLoadingPriority, Is.EqualTo(priority));
        }

        [UnityTest]
        public IEnumerator SingleLoad_ReplacesOpenScenes() => Run(SingleLoadAsync());

        private async Awaitable SingleLoadAsync()
        {
            await _loader.LoadAsync(TestSceneNames.A, new SceneLoadOptions { Mode = LoadSceneMode.Additive });
            Scene scene = await _loader.LoadAsync(TestSceneNames.B);

            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(scene));
            Assert.That(SceneManager.GetSceneByName(TestSceneNames.A).isLoaded, Is.False);
            Assert.That(_loader.LastReport.Mode, Is.EqualTo(LoadSceneMode.Single));
        }

        [UnityTest]
        public IEnumerator LoadByBuildIndex_LoadsTheScene() => Run(LoadByBuildIndexAsync());

        private async Awaitable LoadByBuildIndexAsync()
        {
            int index = SceneUtility.GetBuildIndexByScenePath($"Assets/PretzelSceneLoaderTests/{TestSceneNames.C}.unity");
            Assert.That(index, Is.GreaterThanOrEqualTo(0));

            Scene scene = await _loader.LoadAsync(index, new SceneLoadOptions { Mode = LoadSceneMode.Additive });

            Assert.That(scene.name, Is.EqualTo(TestSceneNames.C));
        }

        [UnityTest]
        public IEnumerator SetupFailure_UnloadsTheAdditiveScene_AndRethrows() => Run(SetupFailureAsync());

        private async Awaitable SetupFailureAsync()
        {
            Exception error = await CatchAsync(async () => await _loader.LoadAsync(TestSceneNames.A, new SceneLoadOptions
            {
                Mode = LoadSceneMode.Additive,
                SetupAsync = _ => throw new InvalidOperationException("setup failed"),
            }));

            Assert.That(error, Is.InstanceOf<InvalidOperationException>());
            Assert.That(SceneManager.GetSceneByName(TestSceneNames.A).isLoaded, Is.False);
            Assert.That(_loader.LastReport.Error, Is.SameAs(error));
            Assert.That(_loader.LastReport.Succeeded, Is.False);
            Assert.That(_loader.IsBusy, Is.False);
            Assert.That(_loader.State, Is.EqualTo(SceneLoadState.Idle));
        }

        [UnityTest]
        public IEnumerator CancelDuringSetup_UnloadsTheAdditiveScene() => Run(CancelDuringSetupAsync());

        private async Awaitable CancelDuringSetupAsync()
        {
            using CancellationTokenSource source = new();
            Exception error = await CatchAsync(async () => await _loader.LoadAsync(TestSceneNames.A, new SceneLoadOptions
            {
                Mode = LoadSceneMode.Additive,
                SetupAsync = context =>
                {
                    source.Cancel();
                    return Awaitable.NextFrameAsync();
                },
            }, source.Token));

            Assert.That(error, Is.InstanceOf<OperationCanceledException>());
            Assert.That(SceneManager.GetSceneByName(TestSceneNames.A).isLoaded, Is.False);
            Assert.That(_loader.LastReport.Cancelled, Is.True);
            Assert.That(_loader.IsBusy, Is.False);
        }

        [UnityTest]
        public IEnumerator CancelledToken_LoadsNothing() => Run(CancelledTokenAsync());

        private async Awaitable CancelledTokenAsync()
        {
            Exception error = await CatchAsync(async () =>
                await _loader.LoadAsync(TestSceneNames.A, new SceneLoadOptions { Mode = LoadSceneMode.Additive }, new CancellationToken(true)));

            Assert.That(error, Is.InstanceOf<OperationCanceledException>());
            Assert.That(SceneManager.GetSceneByName(TestSceneNames.A).isLoaded, Is.False);
            Assert.That(_loader.IsBusy, Is.False);
        }

        [UnityTest]
        public IEnumerator QueuePolicy_RunsRequestsOneAfterAnother() => Run(QueuePolicyAsync());

        private async Awaitable QueuePolicyAsync()
        {
            List<string> events = new();
            _loader.LoadStarted += report => events.Add("start " + report.SceneNames[0]);
            _loader.LoadFinished += report => events.Add("finish " + report.SceneNames[0]);
            SceneLoadOptions options = new() { Mode = LoadSceneMode.Additive };

            Awaitable<Scene> first = _loader.LoadAsync(TestSceneNames.A, options);
            Awaitable<Scene> second = _loader.LoadAsync(TestSceneNames.B, options);
            Scene a = await first;
            Scene b = await second;

            Assert.That(a.isLoaded && b.isLoaded, Is.True);
            Assert.That(events, Is.EqualTo(new[]
            {
                "start " + TestSceneNames.A, "finish " + TestSceneNames.A, "start " + TestSceneNames.B, "finish " + TestSceneNames.B,
            }));
            Assert.That(_loader.IsBusy, Is.False);
        }

        [UnityTest]
        public IEnumerator QueuePolicy_CancelledWaiterIsSkipped() => Run(CancelledWaiterAsync());

        private async Awaitable CancelledWaiterAsync()
        {
            SceneLoadOptions options = new() { Mode = LoadSceneMode.Additive };
            using CancellationTokenSource source = new();

            Awaitable<Scene> first = _loader.LoadAsync(TestSceneNames.A, options);
            Awaitable<Scene> second = _loader.LoadAsync(TestSceneNames.B, options, source.Token);
            Awaitable<Scene> third = _loader.LoadAsync(TestSceneNames.C, options);
            source.Cancel();

            await first;
            Exception error = await CatchAsync(async () => await second);
            Scene c = await third;

            Assert.That(error, Is.InstanceOf<OperationCanceledException>());
            Assert.That(SceneManager.GetSceneByName(TestSceneNames.B).isLoaded, Is.False);
            Assert.That(c.isLoaded, Is.True);
            Assert.That(_loader.IsBusy, Is.False);
        }

        [UnityTest]
        public IEnumerator IgnorePolicy_SkipsRequestsDuringALoad() => Run(IgnorePolicyAsync());

        private async Awaitable IgnorePolicyAsync()
        {
            _loader.RequestPolicy = SceneRequestPolicy.Ignore;
            SceneLoadOptions options = new() { Mode = LoadSceneMode.Additive };

            Awaitable<Scene> first = _loader.LoadAsync(TestSceneNames.A, options);
            Scene ignored = await _loader.LoadAsync(TestSceneNames.B, options);
            await first;

            Assert.That(ignored.IsValid(), Is.False);
            Assert.That(SceneManager.GetSceneByName(TestSceneNames.B).isLoaded, Is.False);
        }

        [UnityTest]
        public IEnumerator ThrowPolicy_RejectsRequestsDuringALoad() => Run(ThrowPolicyAsync());

        private async Awaitable ThrowPolicyAsync()
        {
            _loader.RequestPolicy = SceneRequestPolicy.Throw;
            SceneLoadOptions options = new() { Mode = LoadSceneMode.Additive };

            Awaitable<Scene> first = _loader.LoadAsync(TestSceneNames.A, options);
            Exception error = await CatchAsync(async () => await _loader.LoadAsync(TestSceneNames.B, options));
            await first;

            Assert.That(error, Is.InstanceOf<InvalidOperationException>());
        }

        [UnityTest]
        public IEnumerator Group_LoadsEveryScene_AndActivatesTheChosenOne() => Run(GroupAsync());

        private async Awaitable GroupAsync()
        {
            SceneGroup group = SceneGroup.Create(TestSceneNames.B, TestSceneNames.A, TestSceneNames.B);
            Scene setupScene = default;
            int setupScenes = 0;
            try
            {
                IReadOnlyList<Scene> scenes = await _loader.LoadGroupAsync(group, new SceneLoadOptions
                {
                    Mode = LoadSceneMode.Additive,
                    SetupAsync = context =>
                    {
                        setupScene = context.Scene;
                        setupScenes = context.Scenes.Count;
                        return Awaitable.NextFrameAsync();
                    },
                });

                Assert.That(scenes.Count, Is.EqualTo(2));
                Assert.That(scenes[0].name, Is.EqualTo(TestSceneNames.A));
                Assert.That(scenes[1].name, Is.EqualTo(TestSceneNames.B));
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(TestSceneNames.B));
                Assert.That(setupScene.name, Is.EqualTo(TestSceneNames.B));
                Assert.That(setupScenes, Is.EqualTo(2));

                await _loader.UnloadGroupAsync(group);
                Assert.That(SceneManager.GetSceneByName(TestSceneNames.A).isLoaded, Is.False);
                Assert.That(SceneManager.GetSceneByName(TestSceneNames.B).isLoaded, Is.False);
            }
            finally
            {
                UnityEngine.Object.Destroy(group);
            }
        }

        [UnityTest]
        public IEnumerator Preload_HoldsTheScene_ThenActivates() => Run(PreloadActivateAsync());

        private async Awaitable PreloadActivateAsync()
        {
            ProgressRecorder progress = new();
            ThreadPriority priority = Application.backgroundLoadingPriority;
            PreloadedScene preload = await _loader.PreloadAsync(TestSceneNames.A, progress);

            Assert.That(preload.IsReady, Is.True);
            Assert.That(_loader.State, Is.EqualTo(SceneLoadState.Preloaded));
            Assert.That(_loader.IsBusy, Is.True);
            Assert.That(SceneManager.GetSceneByName(TestSceneNames.A).isLoaded, Is.False);
            Assert.That(Application.backgroundLoadingPriority, Is.EqualTo(ThreadPriority.Low));
            progress.AssertMonotonicToOne();

            Scene scene = await preload.ActivateAsync();

            Assert.That(scene.isLoaded, Is.True);
            Assert.That(preload.IsConsumed, Is.True);
            Assert.That(_loader.IsBusy, Is.False);
            Assert.That(_loader.LastReport.FromPreload, Is.True);
            Assert.That(Application.backgroundLoadingPriority, Is.EqualTo(priority));
            Assert.Throws<InvalidOperationException>(() => preload.ActivateAsync());
        }

        [UnityTest]
        public IEnumerator Preload_QueuesOtherLoads_UntilActivated() => Run(PreloadQueuesAsync());

        private async Awaitable PreloadQueuesAsync()
        {
            PreloadedScene preload = await _loader.PreloadAsync(TestSceneNames.A);
            Awaitable<Scene> queued = _loader.LoadAsync(TestSceneNames.B, new SceneLoadOptions { Mode = LoadSceneMode.Additive });
            await Awaitable.NextFrameAsync();
            await Awaitable.NextFrameAsync();

            Assert.That(SceneManager.GetSceneByName(TestSceneNames.B).isLoaded, Is.False);

            await preload.ActivateAsync();
            Scene b = await queued;

            Assert.That(b.isLoaded, Is.True);
        }

        [UnityTest]
        public IEnumerator Preload_Cancel_LeavesNothingLoaded() => Run(PreloadCancelAsync());

        private async Awaitable PreloadCancelAsync()
        {
            ThreadPriority priority = Application.backgroundLoadingPriority;
            PreloadedScene preload = await _loader.PreloadAsync(TestSceneNames.A);

            await preload.CancelAsync();

            Assert.That(SceneManager.GetSceneByName(TestSceneNames.A).isLoaded, Is.False);
            Assert.That(preload.IsConsumed, Is.True);
            Assert.That(_loader.IsBusy, Is.False);
            Assert.That(_loader.State, Is.EqualTo(SceneLoadState.Idle));
            Assert.That(Application.backgroundLoadingPriority, Is.EqualTo(priority));
            Assert.Throws<InvalidOperationException>(() => preload.ActivateAsync());

            Scene scene = await _loader.LoadAsync(TestSceneNames.A, new SceneLoadOptions { Mode = LoadSceneMode.Additive });
            Assert.That(scene.isLoaded, Is.True);
        }

        [UnityTest]
        public IEnumerator MissingScene_Throws_AndReleasesTheLoader() => Run(MissingSceneAsync());

        private async Awaitable MissingSceneAsync()
        {
            LogAssert.Expect(LogType.Error, new Regex(TestSceneNames.Missing));
            Exception error = await CatchAsync(async () =>
                await _loader.LoadAsync(TestSceneNames.Missing, new SceneLoadOptions { Mode = LoadSceneMode.Additive }));

            Assert.That(error, Is.InstanceOf<ArgumentException>());
            Assert.That(_loader.IsBusy, Is.False);
            Assert.That(_loader.LastReport.Error, Is.SameAs(error));
        }

        [UnityTest]
        public IEnumerator Failure_WithFallback_LoadsTheFallbackScene() => Run(FallbackAsync());

        private async Awaitable FallbackAsync()
        {
            LogAssert.Expect(LogType.Error, new Regex(TestSceneNames.Missing));
            Exception error = await CatchAsync(async () => await _loader.LoadAsync(TestSceneNames.Missing, new SceneLoadOptions
            {
                Mode = LoadSceneMode.Additive,
                FallbackScene = TestSceneNames.C,
            }));

            Assert.That(error, Is.InstanceOf<SceneLoadException>());
            Assert.That(((SceneLoadException)error).FallbackScene, Is.EqualTo(TestSceneNames.C));
            Assert.That(error.InnerException, Is.InstanceOf<ArgumentException>());
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(TestSceneNames.C));
            Assert.That(_loader.LastReport.RecoveredToFallback, Is.True);
            Assert.That(_loader.IsBusy, Is.False);
        }

        [UnityTest]
        public IEnumerator ListenerException_IsLogged_AndTheLoadSucceeds() => Run(ListenerExceptionAsync());

        private async Awaitable ListenerExceptionAsync()
        {
            _loader.LoadStarted += _ => throw new InvalidOperationException("listener failed");
            LogAssert.Expect(LogType.Exception, new Regex("listener failed"));

            Scene scene = await _loader.LoadAsync(TestSceneNames.A, new SceneLoadOptions { Mode = LoadSceneMode.Additive });

            Assert.That(scene.isLoaded, Is.True);
        }

        private static IEnumerator Run(Awaitable awaitable)
        {
            Awaitable.Awaiter awaiter = awaitable.GetAwaiter();
            while (!awaiter.IsCompleted)
            {
                yield return null;
            }

            awaiter.GetResult();
        }

        private static async Awaitable<Exception> CatchAsync(Func<Awaitable> action)
        {
            try
            {
                await action();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        private static async Awaitable UnloadTestScenesAsync()
        {
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || !scene.name.StartsWith(TestSceneNames.Prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                if (SceneManager.sceneCount == 1)
                {
                    SceneManager.SetActiveScene(SceneManager.CreateScene($"LoaderTestHost{++_hostScenes}"));
                }

                AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
                while (operation != null && !operation.isDone)
                {
                    await Awaitable.NextFrameAsync();
                }
            }
        }

        private sealed class ProgressRecorder : IProgress<float>
        {
            private readonly List<float> _values = new();

            public void Report(float value) => _values.Add(value);

            public void AssertMonotonicToOne()
            {
                Assert.That(_values, Is.Not.Empty);
                for (int i = 1; i < _values.Count; i++)
                {
                    Assert.That(_values[i], Is.GreaterThanOrEqualTo(_values[i - 1]), $"Progress went back at report {i}.");
                }

                Assert.That(_values[_values.Count - 1], Is.EqualTo(1f));
            }
        }

        private sealed class FakeTransition : ISceneTransition, ISceneStatusDisplay
        {
            private readonly List<string> _events;

            public FakeTransition(List<string> events) => _events = events;

            public float LastProgress { get; private set; }

            public async Awaitable ShowAsync(CancellationToken cancellationToken)
            {
                _events.Add("show");
                await Awaitable.NextFrameAsync(cancellationToken);
            }

            public async Awaitable HideAsync(CancellationToken cancellationToken)
            {
                _events.Add("hide");
                await Awaitable.NextFrameAsync(cancellationToken);
            }

            public void ReportProgress(float progress) => LastProgress = progress;

            public void ReportStatus(string status) => _events.Add("status:" + status);
        }
    }
}
