using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pretzel.Vfx.PlayTests
{
    public sealed class TimeScaleServiceTests
    {
        [TearDown]
        public void TearDown()
        {
            if (TimeScaleService.Instance != null)
            {
                TimeScaleService.Instance.CancelAll();
            }

            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Freeze_StopsTimeThenRestoresItWithoutHelp()
        {
            Time.timeScale = 1f;
            TimeScaleService.Instance.Freeze(0.15f);

            Assert.AreEqual(TimeScaleService.MinimumScale, Time.timeScale, 0.00001f);

            float waited = 0f;
            while (Time.timeScale < 1f && waited < 2f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(1f, Time.timeScale, "The time scale was not restored.");
            Assert.Greater(waited, 0.1f, "The freeze ended too soon.");
        }

        [UnityTest]
        public IEnumerator SlowMotion_RestoresTheScaleItFound()
        {
            Time.timeScale = 0.5f;
            TimeScaleService.Instance.SlowMotion(0.2f, 0.1f);

            Assert.AreEqual(0.1f, Time.timeScale, 0.0001f);

            float waited = 0f;
            while (TimeScaleService.Instance.Stack.IsActive && waited < 2f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(0.5f, Time.timeScale, 0.0001f);
        }

        [UnityTest]
        public IEnumerator APauseDuringAFreeze_IsNotUndone()
        {
            Time.timeScale = 1f;
            TimeScaleService service = TimeScaleService.Instance;
            service.Freeze(0.1f);

            Time.timeScale = 0f;
            float waited = 0f;
            while (service.Stack.IsActive && waited < 2f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(0f, Time.timeScale, "A pause set during the freeze must survive it.");
        }

        [UnityTest]
        public IEnumerator Cancel_GivesTheTimeBackAtOnce()
        {
            Time.timeScale = 1f;
            int id = TimeScaleService.Instance.Freeze(5f);
            yield return null;

            TimeScaleService.Instance.Cancel(id);

            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator Freeze_KeepsRunningWhileTimeIsStopped()
        {
            Time.timeScale = 1f;
            TimeScaleService.Instance.Freeze(0.1f);
            int frames = 0;

            while (TimeScaleService.Instance.Stack.IsActive && frames < 600)
            {
                frames++;
                yield return null;
            }

            Assert.Less(frames, 600, "The service stalled while the time scale was 0.");
        }
    }
}
