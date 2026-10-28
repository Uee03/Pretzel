using System.Linq;
using NUnit.Framework;
using Pretzel.Setup.Settings;
using Pretzel.Setup.Tabs;
using UnityEngine.UIElements;

namespace Pretzel.Setup.Tests
{
    public sealed class VContainerTabTests
    {
        [Test]
        public void View_BuildsEverySection()
        {
            VisualElement root = new();
            PretzelTabContext context = new(root, new SettingsSaver(root));
            VContainerTab tab = new();

            VisualElement view = tab.CreateView(context);
            tab.OnSelected();

            string[] buttons = view.Query<Button>().ToList().Select(b => b.text).ToArray();
            Assert.That(buttons, Does.Contain("Generate and set up"));
            Assert.That(buttons, Does.Contain("Add service"));
            Assert.That(view.Query<Toggle>().ToList().Count, Is.GreaterThanOrEqualTo(7));
        }
    }
}
