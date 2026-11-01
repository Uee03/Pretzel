using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.UI.Tests
{
    public sealed class ToastStyleTests
    {
        [Test]
        public void TheDefaultStyleSheetShips()
        {
            Assert.That(Resources.Load<StyleSheet>(ToastService.DefaultStyleResource), Is.Not.Null);
        }

        [Test]
        public void ToastsWorkOnAContainerThatIsNotOnAPanelYet()
        {
            VisualElement container = new();
            ToastService toasts = new(container);

            toasts.Show("Saved", 1f, ToastKind.Success);

            Assert.That(container.childCount, Is.EqualTo(1));
            Assert.That(container[0].ClassListContains("pretzel-toast--success"), Is.True);
        }

        [Test]
        public void TheDefaultStyleCanBeSkipped()
        {
            VisualElement container = new();

            Assert.DoesNotThrow(() => new ToastService(container, useDefaultStyle: false));
        }
    }
}
