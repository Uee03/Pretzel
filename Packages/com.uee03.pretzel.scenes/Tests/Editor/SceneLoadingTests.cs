using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pretzel.Core.Timers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pretzel.Scenes.Tests
{
    public sealed class SceneLoadingTests
    {
        private sealed class Recorder : IProgress<float>
        {
            public readonly List<float> Values = new();

            public void Report(float value) => Values.Add(value);
        }

        [Test]
        public void SubProgressMapsAndClampsIntoItsRange()
        {
            Recorder recorder = new();
            SubProgress setup = new(recorder, 0.4f, 1f);
            setup.Report(0f);
            setup.Report(0.5f);
            setup.Report(2f);
            Assert.That(recorder.Values, Is.EqualTo(new[] { 0.4f, 0.7f, 1f }).Within(0.0001f));
        }

        [Test]
        public void NestedSubProgressSplitsARange()
        {
            Recorder recorder = new();
            SubProgress setup = new(recorder, 0.5f, 1f);
            SubProgress spawning = new(setup, 0.2f, 1f);
            spawning.Report(0f);
            spawning.Report(1f);
            Assert.That(recorder.Values, Is.EqualTo(new[] { 0.6f, 1f }).Within(0.0001f));
        }

        [Test]
        public void SubProgressWithoutTargetIsSafe()
        {
            Assert.DoesNotThrow(() => new SubProgress(null, 0f, 1f).Report(0.5f));
        }

        [Test]
        public void DefaultOptionsGiveTheSceneHalfTheBar()
        {
            SceneLoadOptions options = new();
            Assert.That(options.SceneProgressWeight, Is.EqualTo(0.5f));
            Assert.That(options.SettleFrames, Is.EqualTo(1));
            Assert.That(options.SetupAsync, Is.Null);
        }

        [Test]
        public void FindComponentSearchesOneSceneIncludingInactiveObjects()
        {
            GameObject root = new("scene-root");
            GameObject child = new("scene-child");
            child.transform.SetParent(root.transform);
            child.SetActive(false);
            BoxCollider target = child.AddComponent<BoxCollider>();
            try
            {
                Assert.That(root.scene.FindComponent<BoxCollider>(), Is.SameAs(target));
                Assert.That(default(Scene).FindComponent<BoxCollider>(), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FrameBudgetReportsWhenTheFrameIsUsedUp()
        {
            FrameBudget none = new(0f);
            Assert.That(none.IsExceeded, Is.True);
            FrameBudget generous = new(100000f);
            Assert.That(generous.IsExceeded, Is.False);
        }
    }
}
