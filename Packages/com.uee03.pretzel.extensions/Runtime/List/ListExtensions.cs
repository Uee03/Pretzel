using System.Collections.Generic;

namespace Pretzel.Extensions
{
    /// <summary>
    /// Extensions for lists and arrays. Arrays are covered because they implement <see cref="IList{T}"/>.
    /// Randomness uses UnityEngine.Random, so it follows Random.InitState.
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Shuffle the elements in place using the Fisher-Yates algorithm.
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                list.Swap(i, UnityEngine.Random.Range(0, i + 1));
            }
        }

        /// <summary>
        /// Swap the elements at two indices.
        /// </summary>
        public static void Swap<T>(this IList<T> list, int i, int j)
        {
            (list[i], list[j]) = (list[j], list[i]);
        }

        /// <summary>
        /// Get a random element. Throws when the list is empty, use <see cref="TryGetRandomItem{T}"/> to avoid that.
        /// </summary>
        public static T GetRandomItem<T>(this IList<T> list)
        {
            if (list.Count == 0)
            {
                throw new System.InvalidOperationException("Cannot pick a random item from an empty list.");
            }

            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// Get a random element, or return false when the list is empty.
        /// </summary>
        public static bool TryGetRandomItem<T>(this IList<T> list, out T item)
        {
            if (list.Count == 0)
            {
                item = default;
                return false;
            }

            item = list[UnityEngine.Random.Range(0, list.Count)];
            return true;
        }
    }
}
