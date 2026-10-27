using UnityEngine;

namespace Pretzel.Pooling
{
    // Instantiating under an inactive parent keeps Awake and OnEnable from running until an instance is first taken.
    internal static class InactiveHolder
    {
        private static GameObject _holder;

        public static Transform Transform
        {
            get
            {
                if (!_holder)
                {
                    _holder = new GameObject("[Pretzel.Pooling Create]") { hideFlags = HideFlags.HideAndDontSave };
                    _holder.SetActive(false);
                }

                return _holder.transform;
            }
        }
    }
}
