using Object = UnityEngine.Object;

namespace Pretzel.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Check if a plain C# object is null. For Unity objects use the overload that also detects destroyed objects.
        /// </summary>
        public static bool IsNull(this object value)
        {
            return value == null;
        }

        public static bool IsNotNull(this object value)
        {
            return value != null;
        }

        /// <summary>
        /// Check if a Unity object is null or has been destroyed.
        /// </summary>
        public static bool IsNull(this Object value)
        {
            return value == null;
        }

        public static bool IsNotNull(this Object value)
        {
            return value != null;
        }
    }
}
