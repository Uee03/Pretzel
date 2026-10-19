using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Pretzel.Extensions
{
    public static class TransformExtensions
    {
        /// <summary>
        /// The world position rounded to a Vector3Int.
        /// </summary>
        public static Vector3Int PositionToInt(this Transform transform)
        {
            Vector3 position = transform.position;
            return new Vector3Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), Mathf.RoundToInt(position.z));
        }

        /// <summary>
        /// Set the parent, then reset the local position and rotation so this transform sits on the parent.
        /// </summary>
        public static void ReparentAndAlign(this Transform transform, Transform parent)
        {
            transform.SetParent(parent);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Put every active collider under this transform on the named layer.
        /// </summary>
        /// <exception cref="ArgumentException">The layer name does not exist</exception>
        public static void SetColliderInteractionLayers(this Transform transform, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                throw new ArgumentException($"Layer '{layerName}' does not exist.", nameof(layerName));
            }

            using (ListPool<Collider>.Get(out List<Collider> colliders))
            {
                transform.GetComponentsInChildren(colliders);
                foreach (Collider collider in colliders)
                {
                    collider.gameObject.layer = layer;
                }
            }
        }

        /// <summary>
        /// Destroy every child GameObject.
        /// </summary>
        /// <param name="transform">The parent</param>
        /// <param name="destroyInactive">When false, inactive children are kept</param>
        public static void DestroyAllChildren(this Transform transform, bool destroyInactive = false)
        {
            foreach (Transform child in transform)
            {
                if (destroyInactive || child.gameObject.activeSelf)
                {
                    Object.Destroy(child.gameObject);
                }
            }
        }

        public static void Activate(this Transform transform)
        {
            transform.gameObject.SetActive(true);
        }

        public static void Deactivate(this Transform transform)
        {
            transform.gameObject.SetActive(false);
        }

        public static void Destroy(this Transform transform)
        {
            Object.Destroy(transform.gameObject);
        }

        /// <summary>
        /// Instantiate a copy of this transform's GameObject and return the copy's Transform.
        /// </summary>
        /// <param name="transform">The transform to copy</param>
        /// <param name="position">World position of the copy, defaults to the origin</param>
        /// <param name="rotation">World rotation of the copy, defaults to no rotation</param>
        /// <param name="parent">Optional parent of the copy</param>
        public static Transform Clone(this Transform transform, Vector3 position = default, Quaternion? rotation = null, Transform parent = null)
        {
            return Object.Instantiate(transform.gameObject, position, rotation ?? Quaternion.identity, parent).transform;
        }
    }
}
