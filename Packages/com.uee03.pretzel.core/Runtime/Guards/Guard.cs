using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Pretzel.Core
{
    /// <summary>
    /// Argument checks that throw when a precondition is not met. Pass <c>nameof(argument)</c> as the parameter name.
    /// </summary>
    public static class Guard
    {
        /// <summary>Throws if the reference is null. Use <see cref="NotDestroyed{T}"/> for Unity objects.</summary>
        public static T NotNull<T>(T value, string paramName) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }

            return value;
        }

        /// <summary>Throws if the Unity object is null or has been destroyed.</summary>
        public static T NotDestroyed<T>(T value, string paramName) where T : Object
        {
            if (!value)
            {
                throw new ArgumentNullException(paramName, "The Unity object is null or has been destroyed.");
            }

            return value;
        }

        public static string NotNullOrEmpty(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Value cannot be null or empty.", paramName);
            }

            return value;
        }

        public static IReadOnlyCollection<T> NotNullOrEmpty<T>(IReadOnlyCollection<T> value, string paramName)
        {
            if (value == null || value.Count == 0)
            {
                throw new ArgumentException("Collection cannot be null or empty.", paramName);
            }

            return value;
        }

        public static int InRange(int value, int min, int max, string paramName)
        {
            if (value < min || value > max)
            {
                throw new ArgumentOutOfRangeException(paramName, value, $"Value must be between {min} and {max}.");
            }

            return value;
        }

        public static float InRange(float value, float min, float max, string paramName)
        {
            if (!(value >= min && value <= max))
            {
                throw new ArgumentOutOfRangeException(paramName, value, $"Value must be between {min} and {max}.");
            }

            return value;
        }
    }
}
