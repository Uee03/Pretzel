using System;
using NUnit.Framework;
using UnityEngine;

namespace Pretzel.TestPretzel.LiveServicesTests
{
    /// <summary>Shared guards for tests that call the real Unity Gaming Services of the project this host is linked to.</summary>
    public static class LiveServices
    {
        public const string Category = "LiveServices";

        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(45);

        public static void RequireLinkedProject()
        {
            if (string.IsNullOrEmpty(Application.cloudProjectId))
            {
                Assert.Ignore("TestPretzel is not linked to a Unity Cloud project (Edit > Project Settings > Services).");
            }
        }
    }
}
