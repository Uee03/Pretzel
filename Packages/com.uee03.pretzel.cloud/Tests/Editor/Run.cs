using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace Pretzel.Cloud.Tests
{
    /// <summary>Drives <see cref="Awaitable"/> results from EditMode tests.</summary>
    internal static class Run
    {
        /// <summary>Gets the result of an awaitable that must already be complete, as every in-memory fake is.</summary>
        public static T Result<T>(Awaitable<T> awaitable)
        {
            Awaitable<T>.Awaiter awaiter = awaitable.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True, "The awaitable did not complete synchronously.");
            return awaiter.GetResult();
        }

        public static void Complete(Awaitable awaitable)
        {
            Awaitable.Awaiter awaiter = awaitable.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True, "The awaitable did not complete synchronously.");
            awaiter.GetResult();
        }

        /// <summary>Waits for an awaitable that needs the player loop, such as the local store's background file work.</summary>
        public static IEnumerator Wait(Awaitable awaitable)
        {
            Awaitable.Awaiter awaiter = awaitable.GetAwaiter();
            double deadline = UnityEditor.EditorApplication.timeSinceStartup + 30d;
            while (!awaiter.IsCompleted)
            {
                if (UnityEditor.EditorApplication.timeSinceStartup > deadline)
                {
                    Assert.Fail("The awaitable did not complete within 30 seconds.");
                }

                yield return null;
            }

            awaiter.GetResult();
        }

        /// <summary>Waits for an awaitable that needs the player loop, such as the local store's background file work.</summary>
        public static IEnumerator Wait<T>(Awaitable<T> awaitable, Action<T> onResult)
        {
            Awaitable<T>.Awaiter awaiter = awaitable.GetAwaiter();
            double deadline = UnityEditor.EditorApplication.timeSinceStartup + 20d;
            while (!awaiter.IsCompleted)
            {
                if (UnityEditor.EditorApplication.timeSinceStartup > deadline)
                {
                    Assert.Fail("The awaitable did not complete within 20 seconds.");
                }

                yield return null;
            }

            onResult(awaiter.GetResult());
        }
    }
}
