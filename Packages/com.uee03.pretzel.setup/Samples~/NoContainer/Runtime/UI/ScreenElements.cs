using UnityEngine.UIElements;

namespace Pretzel.Samples.NoContainer
{
    /// <summary>Builds the few element shapes both screens share. Styling lives in NoContainer.uss.</summary>
    internal static class ScreenElements
    {
        public static VisualElement Panel(VisualElement parent, params string[] classNames)
        {
            VisualElement element = new();
            AddClasses(element, classNames);
            parent.Add(element);
            return element;
        }

        public static Label Text(VisualElement parent, string text, params string[] classNames)
        {
            Label label = new(text);
            AddClasses(label, classNames);
            parent.Add(label);
            return label;
        }

        public static Button Action(VisualElement parent, string text, System.Action onClick, params string[] classNames)
        {
            Button button = new(onClick) { text = text };
            button.AddToClassList("nc-button");
            AddClasses(button, classNames);
            parent.Add(button);
            return button;
        }

        private static void AddClasses(VisualElement element, string[] classNames)
        {
            foreach (string className in classNames)
            {
                element.AddToClassList(className);
            }
        }
    }
}
