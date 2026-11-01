using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pretzel.Vfx.PlayTests
{
    public sealed class SpriteEffectsTests
    {
        private GameObject _object;
        private SpriteRenderer _renderer;

        [SetUp]
        public void SetUp()
        {
            _object = new GameObject("Sprite", typeof(SpriteRenderer));
            _renderer = _object.GetComponent<SpriteRenderer>();
            _renderer.color = new Color(0.2f, 0.4f, 0.6f, 1f);
            _object.transform.localScale = new Vector3(2f, 3f, 1f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_object);
        }

        [UnityTest]
        public IEnumerator Flash_TintsThenRestoresTheExactColour()
        {
            Color original = _renderer.color;
            SpriteFlash flash = _object.AddComponent<SpriteFlash>();
            yield return null;

            flash.Flash(Color.red, 0.1f);
            Assert.IsTrue(flash.IsFlashing);
            Assert.AreEqual(Color.red, _renderer.color);

            float waited = 0f;
            while (flash.IsFlashing && waited < 2f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsFalse(flash.IsFlashing);
            Assert.AreEqual(original, _renderer.color);
            Assert.IsFalse(flash.enabled, "An idle flash must not keep an Update running.");
        }

        [UnityTest]
        public IEnumerator Flash_OnTopOfAFlash_DoesNotLoseTheOriginalColour()
        {
            Color original = _renderer.color;
            SpriteFlash flash = _object.AddComponent<SpriteFlash>();
            yield return null;

            flash.Flash(Color.red, 0.2f);
            yield return null;
            flash.Flash(Color.green, 0.1f);
            flash.Stop();

            Assert.AreEqual(original, _renderer.color);
        }

        [UnityTest]
        public IEnumerator Flash_RestoresTheColourWhenDisabledMidFlash()
        {
            Color original = _renderer.color;
            SpriteFlash flash = _object.AddComponent<SpriteFlash>();
            yield return null;
            flash.Flash(Color.red, 5f);

            flash.enabled = false;

            Assert.AreEqual(original, _renderer.color);
            Assert.IsFalse(flash.IsFlashing);
        }

        [UnityTest]
        public IEnumerator Punch_GrowsThenReturnsTheExactScale()
        {
            Vector3 original = _object.transform.localScale;
            PunchScale punch = _object.AddComponent<PunchScale>();
            yield return null;

            punch.Punch(0.5f, 0.1f);
            Assert.Greater(_object.transform.localScale.x, original.x);

            float waited = 0f;
            while (punch.IsPunching && waited < 2f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(original, _object.transform.localScale);
            Assert.IsFalse(punch.enabled, "An idle punch must not keep an Update running.");
        }

        [UnityTest]
        public IEnumerator Punch_OnTopOfAPunch_DoesNotKeepTheGrownScale()
        {
            Vector3 original = _object.transform.localScale;
            PunchScale punch = _object.AddComponent<PunchScale>();
            yield return null;

            punch.Punch(0.5f, 0.2f);
            yield return null;
            punch.Punch(0.5f, 0.2f);
            punch.Stop();

            Assert.AreEqual(original, _object.transform.localScale);
        }

        [UnityTest]
        public IEnumerator Punch_KeepsAnimatingWhileTimeIsStopped()
        {
            PunchScale punch = _object.AddComponent<PunchScale>();
            yield return null;
            Time.timeScale = 0f;
            try
            {
                punch.Punch(0.5f, 0.1f);
                float waited = 0f;
                while (punch.IsPunching && waited < 2f)
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }

                Assert.IsFalse(punch.IsPunching, "The punch froze with the game.");
            }
            finally
            {
                Time.timeScale = 1f;
            }
        }
    }
}
