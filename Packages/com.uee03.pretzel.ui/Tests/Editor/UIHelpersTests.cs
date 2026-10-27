using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.UI.Tests
{
    public sealed class UIHelpersTests
    {
        private static bool IsShown(VisualElement element) => element.style.display.value == DisplayStyle.Flex;

        [Test]
        public void ScreenStackShowsTopAndRestoresBelow()
        {
            VisualElement menu = new() { name = "menu" };
            VisualElement options = new() { name = "options" };
            VisualElement dialog = new() { name = "dialog" };
            UIScreenStack stack = new(0f);
            VisualElement changedTo = null;
            stack.Changed += screen => changedTo = screen;

            stack.Push(menu);
            stack.Push(options);
            Assert.That(IsShown(options), Is.True);
            Assert.That(IsShown(menu), Is.False);

            stack.Push(dialog, modal: true);
            Assert.That(IsShown(options), Is.True, "A modal push keeps the screen below visible.");

            Assert.That(stack.Pop(), Is.True);
            Assert.That(IsShown(dialog), Is.False);
            Assert.That(stack.Pop(), Is.True);
            Assert.That(IsShown(menu), Is.True);
            Assert.That(changedTo, Is.SameAs(menu));
            Assert.That(stack.Pop(), Is.False, "The root screen stays by default.");
            Assert.That(stack.Count, Is.EqualTo(1));
        }

        [Test]
        public void ScreenStackRejectsDuplicatesAndPopsToRoot()
        {
            VisualElement a = new();
            VisualElement b = new();
            VisualElement c = new();
            UIScreenStack stack = new(0f);
            stack.Push(a);
            Assert.Throws<InvalidOperationException>(() => stack.Push(a));
            stack.Push(b);
            stack.Replace(c);
            Assert.That(stack.Current, Is.SameAs(c));
            Assert.That(stack.Contains(b), Is.False);
            stack.PopToRoot();
            Assert.That(stack.Current, Is.SameAs(a));
            Assert.That(IsShown(a), Is.True);
            stack.Clear();
            Assert.That(stack.Count, Is.EqualTo(0));
            Assert.That(IsShown(a), Is.False);
        }

        [Test]
        public void SafeAreaInsetsScaleToThePanel()
        {
            // A 2000x1000 screen with a 100 px notch on the left and a 50 px home bar at the bottom, on a half-size panel.
            Rect safe = new(100f, 50f, 1900f, 950f);
            SafeAreaInsets insets = SafeAreaInsets.Compute(safe, new Vector2(2000f, 1000f), new Vector2(1000f, 500f));
            Assert.That(insets.Left, Is.EqualTo(50f).Within(0.001f));
            Assert.That(insets.Bottom, Is.EqualTo(25f).Within(0.001f));
            Assert.That(insets.Top, Is.EqualTo(0f).Within(0.001f));
            Assert.That(insets.Right, Is.EqualTo(0f).Within(0.001f));
            Assert.That(SafeAreaInsets.Compute(safe, Vector2.zero, Vector2.one).Left, Is.EqualTo(0f));
        }

        [Test]
        public void QRequiredExplainsWhatIsMissing()
        {
            VisualElement root = new() { name = "root" };
            Button play = new() { name = "play" };
            root.Add(play);
            Assert.That(root.QRequired<Button>("play"), Is.SameAs(play));
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => root.QRequired<Button>("quit"));
            Assert.That(ex.Message, Does.Contain("quit"));
        }

        [Test]
        public void DisplayVisibilityAndPickingHelpers()
        {
            VisualElement root = new();
            VisualElement child = new();
            root.Add(child);

            root.SetDisplayed(false);
            Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));
            root.SetVisible(false);
            Assert.That(root.style.visibility.value, Is.EqualTo(Visibility.Hidden));
            root.SetPickingModeRecursive(PickingMode.Ignore);
            Assert.That(child.pickingMode, Is.EqualTo(PickingMode.Ignore));
        }
    }
}
