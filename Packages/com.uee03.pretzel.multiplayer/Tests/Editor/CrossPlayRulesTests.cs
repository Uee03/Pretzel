using NUnit.Framework;

namespace Pretzel.Multiplayer.Tests
{
    public sealed class CrossPlayRulesTests
    {
        [Test]
        public void CrossPlayOn_NeverRestrictsQueries()
        {
            Assert.IsNull(CrossPlayRules.GetQueryPlatformTag(true, "windows"));
        }

        [Test]
        public void CrossPlayOff_RestrictsToTheLocalPlatformTag()
        {
            Assert.AreEqual("android", CrossPlayRules.GetQueryPlatformTag(false, "  Android "));
        }

        [TestCase("windows", "android")]
        [TestCase("ios", "windows")]
        [TestCase("other", "macos")]
        public void CrossPlayOn_ShowsEverySession(string local, string session)
        {
            Assert.IsTrue(CrossPlayRules.IsVisible(true, local, session));
        }

        [Test]
        public void CrossPlayOff_ShowsOnlyTheSamePlatform()
        {
            Assert.IsTrue(CrossPlayRules.IsVisible(false, "windows", "windows"));
            Assert.IsTrue(CrossPlayRules.IsVisible(false, "Windows", " windows "));
            Assert.IsFalse(CrossPlayRules.IsVisible(false, "windows", "android"));
        }

        [Test]
        public void CrossPlayOff_StillShowsSessionsWithoutATag()
        {
            Assert.IsTrue(CrossPlayRules.IsVisible(false, "windows", string.Empty));
            Assert.IsTrue(CrossPlayRules.IsVisible(false, "windows", null));
        }

        [Test]
        public void NormalizeTag_FallsBackToOther()
        {
            Assert.AreEqual(PlatformTags.Unknown, CrossPlayRules.NormalizeTag(null));
            Assert.AreEqual(PlatformTags.Unknown, CrossPlayRules.NormalizeTag("  "));
            Assert.AreEqual("linux", CrossPlayRules.NormalizeTag(" LINUX "));
        }

        [TestCase(UnityEngine.RuntimePlatform.WindowsPlayer, "windows")]
        [TestCase(UnityEngine.RuntimePlatform.WindowsEditor, "windows")]
        [TestCase(UnityEngine.RuntimePlatform.OSXPlayer, "macos")]
        [TestCase(UnityEngine.RuntimePlatform.LinuxPlayer, "linux")]
        [TestCase(UnityEngine.RuntimePlatform.Android, "android")]
        [TestCase(UnityEngine.RuntimePlatform.IPhonePlayer, "ios")]
        [TestCase(UnityEngine.RuntimePlatform.WebGLPlayer, "webgl")]
        [TestCase(UnityEngine.RuntimePlatform.Switch, "switch")]
        [TestCase(UnityEngine.RuntimePlatform.PS5, "playstation")]
        [TestCase(UnityEngine.RuntimePlatform.GameCoreXboxSeries, "xbox")]
        [TestCase(UnityEngine.RuntimePlatform.tvOS, "other")]
        public void PlatformTags_MapRuntimePlatforms(UnityEngine.RuntimePlatform platform, string expected)
        {
            Assert.AreEqual(expected, PlatformTags.FromRuntimePlatform(platform));
        }
    }
}
