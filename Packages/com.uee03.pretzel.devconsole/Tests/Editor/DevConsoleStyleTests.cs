using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.DevConsole.Tests
{
    public sealed class DevConsoleStyleTests
    {
        [Test]
        public void TheDefaultStyleSheetShips()
        {
            Assert.That(Resources.Load<StyleSheet>(DeveloperConsole.DefaultStyleResource), Is.Not.Null);
        }
    }
}
